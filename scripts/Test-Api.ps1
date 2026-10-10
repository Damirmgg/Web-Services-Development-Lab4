param([string]$BaseUrl = 'http://localhost:5084')

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$client = [System.Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromSeconds(15)
$createdIds = [System.Collections.Generic.List[int]]::new()
$script:checks = 0

function Send-ApiRequest {
    param(
        [string]$Method,
        [string]$Path,
        [object]$Body = $null,
        [string]$RawBody = $null,
        [string]$ContentType = 'application/json'
    )
    $request = [System.Net.Http.HttpRequestMessage]::new(
        [System.Net.Http.HttpMethod]::new($Method), ($BaseUrl.TrimEnd('/') + $Path))
    try {
        if ($null -ne $Body -or $PSBoundParameters.ContainsKey('RawBody')) {
            $json = if ($PSBoundParameters.ContainsKey('RawBody')) { $RawBody } else { $Body | ConvertTo-Json -Depth 10 }
            $request.Content = [System.Net.Http.StringContent]::new(
                $json, [System.Text.Encoding]::UTF8, $ContentType)
        }
        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        try {
            $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            return [pscustomobject]@{
                Status = [int]$response.StatusCode
                Json = ($content | ConvertFrom-Json)
                Location = [string]$response.Headers.Location
            }
        }
        finally { $response.Dispose() }
    }
    finally { $request.Dispose() }
}

function Assert-Check {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
    $script:checks++
    Write-Host ('PASS: ' + $Message) -ForegroundColor Green
}

function Assert-Envelope {
    param($Response, [int]$Status, [bool]$Success)
    Assert-Check ($Response.Status -eq $Status) ('Expected HTTP ' + $Status + ', received ' + $Response.Status)
    Assert-Check ($Response.Json.isSuccess -eq $Success) 'isSuccess matches HTTP status'
    $fields = @($Response.Json.PSObject.Properties.Name | Sort-Object)
    Assert-Check (($fields -join ',') -eq 'errorMessage,isSuccess,result') 'ReturnResult fields'
    Assert-Check ($Response.Json.errorMessage -is [array]) 'errorMessage is an array'
    if ($Success) {
        Assert-Check ($Response.Json.errorMessage.Count -eq 0) 'No errors in a successful response'
    }
    else {
        Assert-Check ($Response.Json.errorMessage.Count -gt 0) 'Error response contains messages'
        Assert-Check ($null -eq $Response.Json.result) 'Error response has null result'
    }
}

try {
    $list = Send-ApiRequest GET '/api/products'
    Assert-Envelope $list 200 $true

    $created = Send-ApiRequest POST '/api/products' @{
        name = '  Smoke test product  '
        price = 350000.50
        category = 'Test category'
    }
    Assert-Envelope $created 201 $true
    $productId = [int]$created.Json.result.id
    $createdIds.Add($productId)
    Assert-Check ($productId -gt 0) 'Database generated an ID'
    Assert-Check ($created.Location.EndsWith('/api/products/' + $productId)) 'Location header points to the product'
    Assert-Check ($created.Json.result.name -eq 'Smoke test product') 'Name was trimmed'
    Assert-Check ($created.Json.result.price -eq 350000.50) 'Decimal price is preserved'
    Assert-Check (@($created.Json.result.PSObject.Properties.Name).Count -eq 3) 'Response DTO has three fields'
    Assert-Check ($created.Json.result.PSObject.Properties.Name -notcontains 'category') 'Category is hidden'

    $loaded = Send-ApiRequest GET ('/api/products/' + $productId)
    Assert-Envelope $loaded 200 $true
    Assert-Check ($loaded.Json.result.name -eq 'Smoke test product') 'Created product can be loaded'

    $list = Send-ApiRequest GET '/api/products'
    Assert-Check ($list.Json.result.id -contains $productId) 'Created product appears in the list'

    $updated = Send-ApiRequest PUT ('/api/products/' + $productId) @{
        name = 'Updated product'
        price = 420000.75
        category = 'Updated category'
    }
    Assert-Envelope $updated 200 $true
    Assert-Check ($updated.Json.result.id -eq $productId) 'PUT preserves the ID'
    Assert-Check ($updated.Json.result.name -eq 'Updated product') 'PUT updates the name'
    Assert-Check ($updated.Json.result.price -eq 420000.75) 'PUT updates the price'
    Assert-Check ($updated.Json.result.PSObject.Properties.Name -notcontains 'category') 'PUT returns DTO without Category'

    $loaded = Send-ApiRequest GET ('/api/products/' + $productId)
    Assert-Check ($loaded.Json.result.name -eq 'Updated product') 'Changes were saved to the database'

    $invalid = Send-ApiRequest POST '/api/products' @{ name = ' '; price = -1; category = '' }
    Assert-Envelope $invalid 400 $false
    Assert-Check ($invalid.Json.errorMessage.Count -ge 3) 'Validation reports name, price and category errors'

    $missingPrice = Send-ApiRequest POST '/api/products' @{ name = 'Product'; category = 'Test' }
    Assert-Envelope $missingPrice 400 $false

    $largePrice = Send-ApiRequest POST '/api/products' @{ name = 'Product'; price = 1000000000000; category = 'Test' }
    Assert-Envelope $largePrice 400 $false

    $malformed = Send-ApiRequest POST '/api/products' -RawBody '{'
    Assert-Envelope $malformed 400 $false

    $unsupported = Send-ApiRequest POST '/api/products' -RawBody '{}' -ContentType 'text/plain'
    Assert-Envelope $unsupported 415 $false

    $badRoute = Send-ApiRequest GET '/api/products/not-an-id'
    Assert-Envelope $badRoute 404 $false

    $badMethod = Send-ApiRequest PATCH ('/api/products/' + $productId) @{ name = 'Product' }
    Assert-Envelope $badMethod 405 $false

    $deleted = Send-ApiRequest DELETE ('/api/products/' + $productId)
    Assert-Envelope $deleted 200 $true
    Assert-Check ($deleted.Json.result -eq $true) 'DELETE confirms deletion'
    $createdIds.Remove($productId) | Out-Null

    $missing = Send-ApiRequest GET ('/api/products/' + $productId)
    Assert-Envelope $missing 404 $false
    $missingUpdate = Send-ApiRequest PUT ('/api/products/' + $productId) @{
        name = 'Product'; price = 100; category = 'Test'
    }
    Assert-Envelope $missingUpdate 404 $false
    $missingDelete = Send-ApiRequest DELETE ('/api/products/' + $productId)
    Assert-Envelope $missingDelete 404 $false

    Write-Host ('All ' + $script:checks + ' checks passed.') -ForegroundColor Green
}
finally {
    foreach ($remainingId in $createdIds) {
        try { Send-ApiRequest DELETE ('/api/products/' + $remainingId) | Out-Null }
        catch { Write-Warning ('Could not remove test product ' + $remainingId) }
    }
    $client.Dispose()
}
