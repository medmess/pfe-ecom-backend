using System.ComponentModel.DataAnnotations;

namespace pfe.ecom.api.Contracts;

public class OrderActionRequest
{
  [MaxLength(500)]
  public string? Reason { get; set; }
}
