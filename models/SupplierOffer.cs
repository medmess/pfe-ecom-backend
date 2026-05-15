using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pfe.ecom.api.Models;

public class SupplierOffer
{
    public int Id { get; set; }

    [Required, MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(800)]
    public string? Description { get; set; }

    public decimal UnitPrice { get; set; }

    public int AvailableQuantity { get; set; }

    public int MinimumQuantity { get; set; }

    public DateTime DeliveryDate { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string ProviderId { get; set; } = string.Empty;

    [ForeignKey(nameof(ProviderId))]
    public ApplicationUser Provider { get; set; } = null!;
}
