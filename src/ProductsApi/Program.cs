using System.Reflection;
using AutoMapper;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using ProductsApi.Data;
using ProductsApi.Mappings;
using ProductsApi.Models;
using ProductsApi.Repositories;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Не задана строка подключения DefaultConnection.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.SuppressMapClientErrors = true;
    // Ошибки валидации используют тот же контракт, что и ответы контроллера.
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                ? "Некорректный формат запроса."
                : error.ErrorMessage)
            .Distinct()
            .ToArray();
        return new BadRequestObjectResult(ReturnResult<object>.Failure(errors));
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Лабораторная работа 04 — API товаров",
        Version = "v1",
        Description = "EF Core, Repository, DTO, AutoMapper и единый ReturnResult."
    });
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory,
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});

var app = builder.Build();

app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    app.Logger.LogError(exception, "Ошибка обработки запроса {Path}", context.Request.Path);
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(
        ReturnResult<object>.Failure("Внутренняя ошибка сервера."));
}));
app.UseStatusCodePages(async statusContext =>
{
    var context = statusContext.HttpContext;
    var message = context.Response.StatusCode switch
    {
        404 => "Указанный ресурс не найден.",
        405 => "HTTP-метод не поддерживается для этого адреса.",
        415 => "Используйте Content-Type: application/json.",
        _ => $"Запрос завершился с кодом {context.Response.StatusCode}."
    };
    await context.Response.WriteAsJsonAsync(ReturnResult<object>.Failure(message));
});

if (app.Environment.IsDevelopment())
{
    // Применяем сохранённую Code First миграцию при локальном запуске.
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    scope.ServiceProvider.GetRequiredService<IMapper>().ConfigurationProvider.AssertConfigurationIsValid();

    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.MapControllers();
app.Run();
