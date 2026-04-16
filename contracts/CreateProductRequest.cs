using System.ComponentModel.DataAnnotations;

namespace pfe.ecom.api.Contracts;

public class CreateProductRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Brand { get; set; }

    public string? Category { get; set; }

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public string? ImageUrl { get; set; }
}