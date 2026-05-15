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

  // =========================================================
  // GET ALL ORDERS
  // =========================================================

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

  // Admin / Supplier / Dealer only see orders
  // containing THEIR OWN products
  public async Task<List<OrderDto>> GetForSupplierAsync(string supplierId)
  {
    return await BuildOrdersQuery(
        _context.Orders.Where(o =>
            o.OrderItems.Any(i =>
                i.Product.SupplierId == supplierId))
    ).ToListAsync();
  }

  public async Task<List<OrderDto>> GetForDeliveryServiceAsync(
      string deliveryServiceName)
  {
    return await BuildOrdersQuery(
        _context.Orders.Where(o =>
            o.ShippingInfo != null &&
            o.ShippingInfo.DeliveryService != null &&
            o.ShippingInfo.DeliveryService == deliveryServiceName)
    ).ToListAsync();
  }

  // =========================================================
  // GET BY ID
  // =========================================================

  public async Task<OrderDto?> GetByIdAsync(int id)
  {
    return await BuildOrdersQuery(
        _context.Orders.Where(o => o.Id == id)
    ).FirstOrDefaultAsync();
  }

  public async Task<OrderDto?> GetByIdForUserAsync(
      int id,
      string userId)
  {
    return await BuildOrdersQuery(
        _context.Orders.Where(o =>
            o.Id == id &&
            o.UserId == userId)
    ).FirstOrDefaultAsync();
  }

  public async Task<OrderDto?> GetByIdForSupplierAsync(
      int id,
      string supplierId)
  {
    return await BuildOrdersQuery(
        _context.Orders.Where(o =>
            o.Id == id &&
            o.OrderItems.Any(i =>
                i.Product.SupplierId == supplierId))
    ).FirstOrDefaultAsync();
  }

  // =========================================================
  // QUERY BUILDER
  // =========================================================

  private IQueryable<OrderDto> BuildOrdersQuery(
      IQueryable<Order> query)
  {
    return query
        .AsNoTracking()
        .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
        .Include(o => o.ShippingInfo)
        .OrderByDescending(o => o.OrderDate)
        .Select(o => new OrderDto
        {
          Id = o.Id,
          UserId = o.UserId,
          OrderDate = o.OrderDate,
          TotalAmount = o.TotalAmount,

          // Commercial status
          Status = o.Status == "ReturnRequested"
              ? "Returned"
              : o.Status,

          // Delivery tracking
          DeliveryStatus = string.IsNullOrWhiteSpace(o.DeliveryStatus)
              ? "Pending"
              : o.DeliveryStatus,

          CancelledAt = o.CancelledAt,
          CancelReason = o.CancelReason,

          ReturnRequestedAt = o.ReturnRequestedAt,
          ReturnReason = o.ReturnReason,

          ShippingInfo = o.ShippingInfo == null
              ? null
              : new ShippingInfoDto
              {
                FullName = o.ShippingInfo.FullName,
                Phone = o.ShippingInfo.Phone,
                Wilaya = o.ShippingInfo.Wilaya,
                Address = o.ShippingInfo.Address,
                Notes = o.ShippingInfo.Notes,
                AddressChoice = o.ShippingInfo.AddressChoice,
                DeliveryService = o.ShippingInfo.DeliveryService,
                DeliveryMode = o.ShippingInfo.DeliveryMode,
                AgencySite = o.ShippingInfo.AgencySite
              },

          Items = o.OrderItems.Select(i => new OrderItemDto
          {
            ProductId = i.ProductId,
            ProductName = i.Product.Name,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
          }).ToList()
        });
  }

  // =========================================================
  // CREATE ORDER
  // =========================================================

  public async Task<OrderDto?> CreateAsync(
      string userId,
      CreateOrderRequest request)
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

    var ids = normalizedItems
        .Select(i => i.ProductId)
        .ToList();

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

      // Delivery tracking
      DeliveryStatus = "Pending",

      TotalAmount = 0,

      OrderItems = new List<OrderItem>(),

      ShippingInfo = BuildShippingInfo(request.ShippingInfo)
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

  // =========================================================
  // SHIPPING INFO
  // =========================================================

  private static OrderShippingInfo? BuildShippingInfo(
      ShippingInfoDto? shipping)
  {
    if (shipping == null)
      return null;

    return new OrderShippingInfo
    {
      FullName = Normalize(shipping.FullName),
      Phone = Normalize(shipping.Phone),
      Wilaya = Normalize(shipping.Wilaya),
      Address = Normalize(shipping.Address),
      Notes = Normalize(shipping.Notes),
      AddressChoice = Normalize(shipping.AddressChoice),
      DeliveryService = Normalize(shipping.DeliveryService),
      DeliveryMode = Normalize(shipping.DeliveryMode),
      AgencySite = Normalize(shipping.AgencySite),
      CreatedAt = DateTime.UtcNow
    };
  }

  private static string? Normalize(string? value)
  {
    return string.IsNullOrWhiteSpace(value)
        ? null
        : value.Trim();
  }

  // =========================================================
  // UPDATE COMMERCIAL STATUS
  // ONLY SUPPLIER / DEALER
  // =========================================================

  public async Task<OrderDto?> UpdateStatusAsync(
      int id,
      string status,
      string supplierId)
  {
    if (string.IsNullOrWhiteSpace(status))
      return null;

    var allowed = new[]
    {
      "Pending",
      "Processing",
      "Cancelled",
      "Returned",
      "ReturnRejected"
    };

    var normalized = allowed.FirstOrDefault(s =>
        s.Equals(status.Trim(),
        StringComparison.OrdinalIgnoreCase));

    if (normalized == null)
      return null;

    var order = await _context.Orders
        .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
        .FirstOrDefaultAsync(o =>
            o.Id == id &&
            o.OrderItems.Any(i =>
                i.Product.SupplierId == supplierId));

    if (order == null)
      return null;

    order.Status = normalized;

    await _context.SaveChangesAsync();

    return await GetByIdForSupplierAsync(id, supplierId);
  }

  // =========================================================
  // CUSTOMER CANCEL
  // =========================================================

  public async Task<OrderDto?> CancelOrderAsync(
      int id,
      string userId,
      string? reason)
  {
    var order = await _context.Orders
        .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
        .FirstOrDefaultAsync(o =>
            o.Id == id &&
            o.UserId == userId);

    if (order == null)
      return null;

    if (order.Status != "Pending" &&
        order.Status != "Processing")
      return null;

    order.Status = "Cancelled";
    order.CancelledAt = DateTime.UtcNow;
    order.CancelReason = reason;

    foreach (var item in order.OrderItems)
    {
      item.Product.StockQuantity += item.Quantity;
    }

    await _context.SaveChangesAsync();

    return await GetByIdForUserAsync(id, userId);
  }

  // =========================================================
  // CUSTOMER RETURN
  // =========================================================

  public async Task<OrderDto?> RequestReturnAsync(
      int id,
      string userId,
      string? reason)
  {
    var order = await _context.Orders
        .FirstOrDefaultAsync(o =>
            o.Id == id &&
            o.UserId == userId);

    if (order == null)
      return null;

    if (order.DeliveryStatus != "Delivered")
      return null;

    order.Status = "Returned";
    order.ReturnRequestedAt = DateTime.UtcNow;
    order.ReturnReason = reason;

    await _context.SaveChangesAsync();

    return await GetByIdForUserAsync(id, userId);
  }
}
