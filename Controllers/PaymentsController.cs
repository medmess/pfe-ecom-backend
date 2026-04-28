using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pfe.ecom.api.Data;
using pfe.ecom.api.Models;

namespace pfe.ecom.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
  private readonly AppDbContext _context;

  public PaymentsController(AppDbContext context)
  {
    _context = context;
  }

  [HttpPost("create/{orderId}")]
  [Authorize(Roles = "Customer,Admin")]
  public async Task<IActionResult> CreatePayment(int orderId, [FromBody] CreatePaymentRequest request)
  {
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(userId))
      return Unauthorized();

    var allowedMethods = new[] { "Edahabia", "CCP", "CashOnDelivery" };

    if (!allowedMethods.Contains(request.Method))
    {
      return BadRequest(new
      {
        message = "Invalid payment method. Use: Edahabia, CCP, or CashOnDelivery."
      });
    }

    var order = await _context.Orders
        .FirstOrDefaultAsync(o => o.Id == orderId);

    if (order == null)
      return NotFound(new { message = "Order not found" });

    if (!User.IsInRole("Admin") && order.UserId != userId)
      return Forbid();

    var existingPayment = await _context.Payments
        .FirstOrDefaultAsync(p => p.OrderId == orderId);

    if (existingPayment != null)
    {
      return BadRequest(new
      {
        message = "A payment already exists for this order."
      });
    }

    var payment = new Payment
    {
      OrderId = order.Id,
      Amount = order.TotalAmount,
      Method = request.Method,
      Status = "Pending",
      TransactionRef = GenerateDemoTransactionRef(request.Method),
      CreatedAt = DateTime.UtcNow
    };

    _context.Payments.Add(payment);
    order.Status = "Pending";

    await _context.SaveChangesAsync();

    return Ok(ToPaymentResponse("Payment created successfully.", payment));
  }

  [HttpPost("confirm/{paymentId}")]
  [Authorize(Roles = "Customer,Supplier,Admin")]
  public async Task<IActionResult> ConfirmPayment(int paymentId)
  {
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(userId))
      return Unauthorized();

    var payment = await _context.Payments
        .Include(p => p.Order)
            .ThenInclude(o => o.OrderItems)
                .ThenInclude(i => i.Product)
        .FirstOrDefaultAsync(p => p.Id == paymentId);

    if (payment == null)
      return NotFound(new { message = "Payment not found" });

    var isAdmin = User.IsInRole("Admin");
    var isCustomerOwner = payment.Order.UserId == userId;
    var isSupplierForThisOrder = payment.Order.OrderItems
        .Any(i => i.Product.SupplierId == userId);

    if (!isAdmin && !isCustomerOwner && !isSupplierForThisOrder)
      return Forbid();

    if (payment.Status == "Paid")
    {
      return BadRequest(new
      {
        message = "Payment is already paid."
      });
    }

    if (payment.Method == "CashOnDelivery")
    {
      if (!isAdmin && !isSupplierForThisOrder)
        return Forbid();

      payment.Status = "Paid";
      payment.PaidAt = DateTime.UtcNow;
      payment.Order.Status = "Delivered";

      await _context.SaveChangesAsync();

      return Ok(ToPaymentResponse("Cash on delivery payment confirmed by supplier.", payment));
    }

    if (!isAdmin && !isCustomerOwner)
      return Forbid();

    payment.Status = "Paid";
    payment.PaidAt = DateTime.UtcNow;
    payment.Order.Status = "Processing";

    await _context.SaveChangesAsync();

    return Ok(ToPaymentResponse("Demo payment confirmed successfully.", payment));
  }

  [HttpPost("fail/{paymentId}")]
  [Authorize(Roles = "Customer,Admin")]
  public async Task<IActionResult> FailPayment(int paymentId)
  {
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(userId))
      return Unauthorized();

    var payment = await _context.Payments
        .Include(p => p.Order)
        .FirstOrDefaultAsync(p => p.Id == paymentId);

    if (payment == null)
      return NotFound(new { message = "Payment not found" });

    if (!User.IsInRole("Admin") && payment.Order.UserId != userId)
      return Forbid();

    payment.Status = "Failed";
    payment.Order.Status = "Pending";

    await _context.SaveChangesAsync();

    return Ok(ToPaymentResponse("Payment marked as failed.", payment));
  }

  [HttpGet("order/{orderId}")]
  [Authorize(Roles = "Customer,Supplier,Admin")]
  public async Task<IActionResult> GetPaymentByOrder(int orderId)
  {
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(userId))
      return Unauthorized();

    var payment = await _context.Payments
        .Include(p => p.Order)
            .ThenInclude(o => o.OrderItems)
                .ThenInclude(i => i.Product)
        .FirstOrDefaultAsync(p => p.OrderId == orderId);

    if (payment == null)
      return NotFound(new { message = "Payment not found for this order" });

    var isAdmin = User.IsInRole("Admin");
    var isCustomerOwner = payment.Order.UserId == userId;
    var isSupplierForThisOrder = payment.Order.OrderItems
        .Any(i => i.Product.SupplierId == userId);

    if (!isAdmin && !isCustomerOwner && !isSupplierForThisOrder)
      return Forbid();

    return Ok(new
    {
      payment.Id,
      payment.OrderId,
      payment.Amount,
      payment.Method,
      payment.Status,
      payment.TransactionRef,
      payment.CreatedAt,
      payment.PaidAt
    });
  }

  private static string GenerateDemoTransactionRef(string method)
  {
    var prefix = method switch
    {
      "Edahabia" => "EDB",
      "CCP" => "CCP",
      "CashOnDelivery" => "COD",
      _ => "PAY"
    };

    return $"{prefix}-DEMO-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
  }

  private static object ToPaymentResponse(string message, Payment payment)
  {
    return new
    {
      message,
      payment = new
      {
        payment.Id,
        payment.OrderId,
        payment.Amount,
        payment.Method,
        payment.Status,
        payment.TransactionRef,
        payment.CreatedAt,
        payment.PaidAt
      }
    };
  }
}

public class CreatePaymentRequest
{
  public string Method { get; set; } = string.Empty;
}
