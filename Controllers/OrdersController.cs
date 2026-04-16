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
    [Authorize(Roles = "Customer,Admin")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (User.IsInRole("Admin"))
        {
            var allOrders = await _orderService.GetAllAsync();
            return Ok(allOrders);
        }

        var userOrders = await _orderService.GetForUserAsync(userId);
        return Ok(userOrders);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<ActionResult<OrderDto>> GetById(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        OrderDto? order;

        if (User.IsInRole("Admin"))
        {
            order = await _orderService.GetByIdAsync(id);
        }
        else
        {
            order = await _orderService.GetByIdForUserAsync(id, userId);
        }

        if (order == null)
            return NotFound(new { message = "Commande introuvable" });

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
}