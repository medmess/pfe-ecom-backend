using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pfe.ecom.api.Models;

public class Payment
{
  public int Id { get; set; }

  [Required]
  public int OrderId { get; set; }

  [ForeignKey("OrderId")]
  public Order Order { get; set; } = null!;

  public decimal Amount { get; set; }

  [Required]
  [MaxLength(50)]
  public string Method { get; set; } = string.Empty;
  // Edahabia, CCP, CashOnDelivery

  [Required]
  [MaxLength(50)]
  public string Status { get; set; } = "Pending";
  // Pending, Paid, Failed

  [MaxLength(100)]
  public string? TransactionRef { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public DateTime? PaidAt { get; set; }
}
