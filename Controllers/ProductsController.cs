using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pfe.ecom.api.Contracts;
using pfe.ecom.api.Services;

namespace pfe.ecom.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _productService;

        public ProductsController(ProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
        {
            var products = await _productService.GetAllAsync();
            return Ok(products);
        }
    [HttpGet("mine")]
    [Authorize(Roles = "Supplier,Dealer,Admin")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetMine()
    {
      var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

      if (string.IsNullOrEmpty(userId))
        return Unauthorized();

      var products = await _productService.GetMineAsync(userId);
      return Ok(products);
    }
    [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductDto>> GetById(int id)
        {
            var product = await _productService.GetByIdAsync(id);

            if (product == null)
                return NotFound(new { message = "Produit introuvable" });

            return Ok(product);
        }

        [HttpPost]
        [Authorize(Roles = "Supplier,Dealer,Admin")]
        public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var isAdmin = User.IsInRole("Admin");

            var createdProduct = await _productService.CreateAsync(request, userId, isAdmin);

            if (createdProduct == null)
                return BadRequest(new { message = "Impossible de créer le produit" });

            return CreatedAtAction(nameof(GetById), new { id = createdProduct.Id }, createdProduct);
        }

        [HttpPost("upload")]
        [Authorize(Roles = "Supplier,Dealer,Admin")]
        public async Task<ActionResult<ProductDto>> CreateWithImage([FromForm] ProductFormRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var imageUrl = SaveProductImageAsync(request.Image);
            if (imageUrl == InvalidImageMessage)
                return BadRequest(new { message = InvalidImageMessage });

            var createRequest = new CreateProductRequest
            {
                Name = request.Name,
                Description = request.Description,
                Brand = request.Brand,
                Category = request.Category,
                Price = request.Price,
                DiscountPercent = request.DiscountPercent,
                StockQuantity = request.StockQuantity,
                ImageUrl = imageUrl ?? request.ImageUrl
            };

            var createdProduct = await _productService.CreateAsync(createRequest, userId, User.IsInRole("Admin"));

            if (createdProduct == null)
                return BadRequest(new { message = "Impossible de crÃ©er le produit" });

            return CreatedAtAction(nameof(GetById), new { id = createdProduct.Id }, createdProduct);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Supplier,Dealer,Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var isAdmin = User.IsInRole("Admin");

            var updated = await _productService.UpdateAsync(id, request, userId, isAdmin);

            if (!updated)
                return NotFound(new { message = "Produit introuvable ou accès refusé" });

            return NoContent();
        }

        [HttpPut("{id}/upload")]
        [Authorize(Roles = "Supplier,Dealer,Admin")]
        public async Task<ActionResult<ProductDto>> UpdateWithImage(int id, [FromForm] ProductFormRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var imageUrl = SaveProductImageAsync(request.Image);
            if (imageUrl == InvalidImageMessage)
                return BadRequest(new { message = InvalidImageMessage });

            var updateRequest = new UpdateProductRequest
            {
                Name = request.Name,
                Description = request.Description,
                Brand = request.Brand,
                Category = request.Category,
                Price = request.Price,
                DiscountPercent = request.DiscountPercent,
                StockQuantity = request.StockQuantity,
                ImageUrl = imageUrl ?? request.ImageUrl
            };

            var updated = await _productService.UpdateAsync(id, updateRequest, userId, User.IsInRole("Admin"));

            if (!updated)
                return NotFound(new { message = "Produit introuvable ou accÃ¨s refusÃ©" });

            var product = await _productService.GetByIdAsync(id);
            return Ok(product);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Supplier,Dealer,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var isAdmin = User.IsInRole("Admin");

            var deleted = await _productService.DeleteAsync(id, userId, isAdmin);

            if (!deleted)
                return NotFound(new { message = "Produit introuvable ou accès refusé" });

            return NoContent();
        }

        private const string InvalidImageMessage = "Only image files are allowed.";

        private string? SaveProductImageAsync(IFormFile? image)
        {
            if (image == null || image.Length == 0)
                return null;

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

            if (!allowedExtensions.Contains(extension))
                return InvalidImageMessage;

            var uploadRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
            Directory.CreateDirectory(uploadRoot);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadRoot, fileName);

            using var stream = System.IO.File.Create(filePath);
            image.CopyTo(stream);

            return $"{Request.Scheme}://{Request.Host}/uploads/products/{fileName}";
        }
    }
}
