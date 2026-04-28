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
    [Authorize(Roles = "Supplier,Admin")]
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
        [Authorize(Roles = "Supplier,Admin")]
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

        [HttpPut("{id}")]
        [Authorize(Roles = "Supplier,Admin")]
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

        [HttpDelete("{id}")]
        [Authorize(Roles = "Supplier,Admin")]
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
    }
}
