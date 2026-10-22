using FoodProducts.Application.Contracts;
using FoodProducts.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodProducts.Api.Controllers;

[ApiController]
[Route("api/food-products")]
public sealed class FoodProductsController(IFoodProductService foodProductService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FoodProductDto>>> GetAll(CancellationToken cancellationToken)
    {
        var foodProducts = await foodProductService.GetAllAsync(cancellationToken);
        return Ok(foodProducts);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FoodProductDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var foodProduct = await foodProductService.GetByIdAsync(id, cancellationToken);
        return foodProduct is null ? NotFound() : Ok(foodProduct);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FoodProductDto>> Create(FoodProductRequest request, CancellationToken cancellationToken)
    {
        var foodProduct = await foodProductService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = foodProduct.Id }, foodProduct);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FoodProductDto>> Update(int id, FoodProductRequest request, CancellationToken cancellationToken)
    {
        var foodProduct = await foodProductService.UpdateAsync(id, request, cancellationToken);
        return foodProduct is null ? NotFound() : Ok(foodProduct);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await foodProductService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
