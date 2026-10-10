using System.ComponentModel.DataAnnotations;

namespace ProductsApi.DTOs;

// POST и PUT принимают одинаковый набор полей без идентификатора.
public class ProductRequestDto
{
    [Required(ErrorMessage = "Укажите название товара.")]
    [StringLength(200, ErrorMessage = "Название должно содержать не более 200 символов.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите цену товара.")]
    [Range(typeof(decimal), "0", "999999999999.99",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "Цена должна быть от 0 до 999999999999.99.")]
    public decimal? Price { get; set; }

    [Required(ErrorMessage = "Укажите категорию товара.")]
    [StringLength(100, ErrorMessage = "Категория должна содержать не более 100 символов.")]
    public string Category { get; set; } = string.Empty;
}
