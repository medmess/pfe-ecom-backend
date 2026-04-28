using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pfe.ecom.api.Contracts;
using pfe.ecom.api.Services;

namespace pfe.ecom.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
  private readonly OrderService _orderService;

  public OrdersController(OrderService orderService)
  {
    _orderService = orderService;
  }

  [HttpGet]
  [Authorize(Roles = "Customer,Supplier,Admin")]
  public async Task<ActionResult<IEnumerable<OrderDto>>> GetAll()
  {
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(userId))
      return Unauthorized();

    if (User.IsInRole("Supplier") || User.IsInRole("Admin"))
    {
      return Ok(await _orderService.GetForSupplierAsync(userId));
    }

    return Ok(await _orderService.GetForUserAsync(userId));
  }

  [HttpGet("{id}")]
  [Authorize(Roles = "Customer,Supplier,Admin")]
  public async Task<ActionResult<OrderDto>> GetById(int id)
  {
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(userId))
      return Unauthorized();

    OrderDto? order;

    if (User.IsInRole("Supplier") || User.IsInRole("Admin"))
    {
      order = await _orderService.GetByIdForSupplierAsync(id, userId);
    }
    else
    {
      order = await _orderService.GetByIdForUserAsync(id, userId);
    }

    if (order == null)
      return NotFound(new { message = "Commande introuvable ou accès refusé" });

    return Ok(order);
  }

  [HttpPost]
  [Authorize(Roles = "Customer,Admin")]
  public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderRequest request)
  {
    if (!ModelState.IsValid)
      return ValidationProblem(ModelState);

    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(userId))
      return Unauthorized();

    var createdOrder = await _orderService.CreateAsync(userId, request);

    if (createdOrder == null)
    {
      return BadRequest(new
      {
        message = "Commande invalide, produit introuvable ou stock insuffisant"
      });
    }

    return CreatedAtAction(nameof(GetById), new { id = createdOrder.Id }, createdOrder);
  }

  [HttpPut("{id}/status")]
  [Authorize(Roles = "Supplier,Admin")]
  public async Task<ActionResult<OrderDto>> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request)
  {
    if (!ModelState.IsValid)
      return ValidationProblem(ModelState);

    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(userId))
      return Unauthorized();

    var updatedOrder = await _orderService.UpdateStatusAsync(id, request.Status, userId);

    if (updatedOrder == null)
    {
      return BadRequest(new
      {
        message = "Commande introuvable, accès refusé ou statut invalide"
      });
    }

    return Ok(updatedOrder);
  }

  [HttpPut("{id}/cancel")]
  [Authorize(Roles = "Customer")]
  public async Task<ActionResult<OrderDto>> CancelOrder(int id, [FromBody] OrderActionRequest request)
  {
    if (!ModelState.IsValid)
      return ValidationProblem(ModelState);

    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(userId))
      return Unauthorized();

    var cancelledOrder = await _orderService.CancelOrderAsync(id, userId, request.Reason);

    if (cancelledOrder == null)
    {
      return BadRequest(new
      {
        message = "Impossible d'annuler cette commande. Elle est peut-être déjà expédiée, livrée ou introuvable."
      });
    }

    return Ok(cancelledOrder);
  }

  [HttpPut("{id}/return-request")]
  [Authorize(Roles = "Customer")]
  public async Task<ActionResult<OrderDto>> RequestReturn(int id, [FromBody] OrderActionRequest request)
  {
    if (!ModelState.IsValid)
      return ValidationProblem(ModelState);

    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(userId))
      return Unauthorized();

    var returnedOrder = await _orderService.RequestReturnAsync(id, userId, request.Reason);

    if (returnedOrder == null)
    {
      return BadRequest(new
      {
        message = "Impossible de demander un retour. Le retour est possible seulement après la livraison."
      });
    }

    return Ok(returnedOrder);
  }
}
