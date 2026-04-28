using System.ComponentModel.DataAnnotations;

namespace pfe.ecom.api.Contracts;

public class CreatePaymentRequest
{
  [Required]
  public string Method { get; set; } = string.Empty;
}

public class PaymentDto
{
  public int Id { get; set; }

  public int OrderId { get; set; }

  public decimal Amount { get; set; }

  public string Method { get; set; } = string.Empty;

  public string Status { get; set; } = string.Empty;

  public string? TransactionRef { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime? PaidAt { get; set; }
}
