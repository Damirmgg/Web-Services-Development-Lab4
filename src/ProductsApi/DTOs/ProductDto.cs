namespace ProductsApi.DTOs;

// Category хранится в сущности, но не раскрывается в ответе клиенту.
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
