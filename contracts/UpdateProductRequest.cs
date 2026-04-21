using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace pfe.ecom.api.Contracts;

public class UpdateProductRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Brand { get; set; }

    public string? Category { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    // pasted image URL
    public string? ImageUrl { get; set; }

    // uploaded file from PC
    public IFormFile? ImageFile { get; set; }
}