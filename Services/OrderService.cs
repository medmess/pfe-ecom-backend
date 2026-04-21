using Microsoft.EntityFrameworkCore;
using pfe.ecom.api.Contracts;
using pfe.ecom.api.Data;
using pfe.ecom.api.Models;

namespace pfe.ecom.api.Services;

public class OrderService
{
    private readonly AppDbContext _context;

    public OrderService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<OrderDto>> GetAllAsync()
    {
        return await BuildOrdersQuery(_context.Orders)
            .ToListAsync();
    }

    public async Task<List<OrderDto>> GetForUserAsync(string userId)
    {
        return await BuildOrdersQuery(
            _context.Orders.Where(o => o.UserId == userId)
        ).ToListAsync();
    }

    public async Task<List<OrderDto>> GetForSupplierAsync(string supplierId)
    {
        return await BuildOrdersQuery(
            _context.Orders.Where(o =>
                o.OrderItems.Any(i =>
                    i.Product.SupplierId == supplierId))
        ).ToListAsync();
    }

    public async Task<OrderDto?> GetByIdAsync(int id)
    {
        return await BuildOrdersQuery(
            _context.Orders.Where(o => o.Id == id)
        ).FirstOrDefaultAsync();
    }

    public async Task<OrderDto?> GetByIdForUserAsync(int id, string userId)
    {
        return await BuildOrdersQuery(
            _context.Orders.Where(o =>
                o.Id == id && o.UserId == userId)
        ).FirstOrDefaultAsync();
    }

    private IQueryable<OrderDto> BuildOrdersQuery(IQueryable<Order> query)
    {
        return query
            .AsNoTracking()
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                Items = o.OrderItems.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            });
    }

    public async Task<OrderDto?> CreateAsync(string userId, CreateOrderRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
            return null;

        var normalizedItems = request.Items
            .Where(i => i.ProductId > 0 && i.Quantity > 0)
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Quantity = g.Sum(x => x.Quantity)
            })
            .ToList();

        if (normalizedItems.Count == 0)
            return null;

        var ids = normalizedItems.Select(i => i.ProductId).ToList();

        var products = await _context.Products
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        if (products.Count != normalizedItems.Count)
            return null;

        foreach (var item in normalizedItems)
        {
            var product = products[item.ProductId];

            if (product.StockQuantity < item.Quantity)
                return null;
        }

        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            Status = "Pending",
            TotalAmount = 0,
            OrderItems = new List<OrderItem>()
        };

        decimal total = 0;

        foreach (var item in normalizedItems)
        {
            var product = products[item.ProductId];

            order.OrderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });

            total += product.Price * item.Quantity;
            product.StockQuantity -= item.Quantity;
        }

        order.TotalAmount = total;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return await GetByIdForUserAsync(order.Id, userId);
    }

    public async Task<OrderDto?> UpdateStatusAsync(int id, string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        var allowed = new[] { "Pending", "Processing", "Delivered" };

        var normalized = allowed.FirstOrDefault(s =>
            s.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));

        if (normalized == null)
            return null;

        var order = await _context.Orders.FindAsync(id);

        if (order == null)
            return null;

        order.Status = normalized;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }
}