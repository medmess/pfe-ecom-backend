namespace pfe.ecom.api.Contracts;

public class OrderDto
{
  public int Id { get; set; }

  public string UserId { get; set; } = string.Empty;

  public DateTime OrderDate { get; set; }

  public decimal TotalAmount { get; set; }

  public string Status { get; set; } = string.Empty;

  public DateTime? CancelledAt { get; set; }

  public string? CancelReason { get; set; }

  public DateTime? ReturnRequestedAt { get; set; }

  public string? ReturnReason { get; set; }

  public List<OrderItemDto> Items { get; set; } = new();
}
