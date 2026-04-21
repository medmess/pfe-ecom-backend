using Microsoft.EntityFrameworkCore;
using pfe.ecom.api.Contracts;
using pfe.ecom.api.Data;
using pfe.ecom.api.Models;

namespace pfe.ecom.api.Services;

public class ProductService
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ProductService(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<List<ProductDto>> GetAllAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Brand = p.Brand,
                Category = p.Category,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl
            })
            .ToListAsync();
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Brand = p.Brand,
                Category = p.Category,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ProductDto?> CreateAsync(CreateProductRequest request, string userId, bool isAdmin)
    {
        var finalImageUrl = await ResolveImageAsync(request.ImageUrl, request.ImageFile);

        var product = new Product
        {
            Name = request.Name ?? string.Empty,
            Description = request.Description ?? string.Empty,
            Brand = request.Brand ?? string.Empty,
            Category = request.Category ?? string.Empty,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            ImageUrl = finalImageUrl ?? string.Empty,
            SupplierId = isAdmin ? null : userId
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Brand = product.Brand,
            Category = product.Category,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            ImageUrl = product.ImageUrl
        };
    }

    public async Task<bool> UpdateAsync(int id, UpdateProductRequest request, string userId, bool isAdmin)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return false;

        if (!isAdmin && product.SupplierId != userId)
            return false;

        product.Name = request.Name ?? string.Empty;
        product.Description = request.Description ?? string.Empty;
        product.Brand = request.Brand ?? string.Empty;
        product.Category = request.Category ?? string.Empty;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;

        // update image only if a new file or new URL was provided
        if (request.ImageFile != null || !string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            var finalImageUrl = await ResolveImageAsync(request.ImageUrl, request.ImageFile);
            product.ImageUrl = finalImageUrl ?? string.Empty;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, string userId, bool isAdmin)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return false;

        if (!isAdmin && product.SupplierId != userId)
            return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<string?> ResolveImageAsync(string? imageUrl, IFormFile? imageFile)
    {
        if (imageFile != null && imageFile.Length > 0)
        {
            return await SaveImageAsync(imageFile);
        }

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            return imageUrl.Trim();
        }

        return null;
    }

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        var webRootPath = _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var uploadsFolder = Path.Combine(webRootPath, "uploads", "products");

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(uploadsFolder, fileName);

        using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/products/{fileName}";
    }
}