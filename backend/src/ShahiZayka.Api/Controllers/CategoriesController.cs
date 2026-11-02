using Microsoft.AspNetCore.Mvc;
using ShahiZayka.Application.DTOs;
using ShahiZayka.Application.Interfaces;

namespace ShahiZayka.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IProductService _productService;

    public CategoriesController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Gets all active menu categories.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _productService.GetAllCategoriesAsync();
        return Ok(categories);
    }
}
