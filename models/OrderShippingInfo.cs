using System.ComponentModel.DataAnnotations;

namespace pfe.ecom.api.Models;

public class OrderShippingInfo
{
  public int Id { get; set; }

  public int OrderId { get; set; }

  [MaxLength(120)]
  public string? FullName { get; set; }

  [MaxLength(40)]
  public string? Phone { get; set; }

  [MaxLength(80)]
  public string? Wilaya { get; set; }

  [MaxLength(500)]
  public string? Address { get; set; }

  [MaxLength(500)]
  public string? Notes { get; set; }

  [MaxLength(30)]
  public string? AddressChoice { get; set; }

  [MaxLength(80)]
  public string? DeliveryService { get; set; }

  [MaxLength(30)]
  public string? DeliveryMode { get; set; }

  [MaxLength(120)]
  public string? AgencySite { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public Order Order { get; set; } = null!;
}
