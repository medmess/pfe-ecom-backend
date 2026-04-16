using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pfe.ecom.api.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Brand { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    [MaxLength(1000)]
    public string ImageUrl { get; set; } = string.Empty;

    // Nullable so old existing products stay valid
    public string? SupplierId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public ApplicationUser? Supplier { get; set; }
}