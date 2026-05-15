using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pfe.ecom.api.Models;

public class SupplyRequest
{
    public int Id { get; set; }

    public int SupplierOfferId { get; set; }

    [ForeignKey(nameof(SupplierOfferId))]
    public SupplierOffer SupplierOffer { get; set; } = null!;

    public string DealerId { get; set; } = string.Empty;

    [ForeignKey(nameof(DealerId))]
    public ApplicationUser Dealer { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal TotalAmount { get; set; }

    [MaxLength(40)]
    public string Status { get; set; } = "Pending";

    [MaxLength(40)]
    public string PaymentMethod { get; set; } = "Cash";

    [MaxLength(40)]
    public string PaymentStatus { get; set; } = "Pending";

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }

    public DateTime? CancelledAt { get; set; }
}
