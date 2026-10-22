using FoodProducts.Api.Controllers;
using FoodProducts.Application.Contracts;
using FoodProducts.Application.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace FoodProducts.Api.Tests.Controllers;

public sealed class FoodProductsControllerTests
{
    private readonly IFoodProductService _foodProductService = Substitute.For<IFoodProductService>();
    private readonly FoodProductsController _controller;

    public FoodProductsControllerTests()
    {
        _controller = new FoodProductsController(_foodProductService);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithFoodProducts()
    {
        IReadOnlyList<FoodProductDto> foodProducts = [CreateDto(1), CreateDto(2)];
        _foodProductService.GetAllAsync(Arg.Any<CancellationToken>()).Returns(foodProducts);

        var result = await _controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(foodProducts, ok.Value);
    }

    [Fact]
    public async Task GetById_WhenFoodProductExists_ReturnsOk()
    {
        var foodProduct = CreateDto(3);
        _foodProductService.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(foodProduct);

        var result = await _controller.GetById(3, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(foodProduct, ok.Value);
    }

    [Fact]
    public async Task GetById_WhenFoodProductDoesNotExist_ReturnsNotFound()
    {
        _foodProductService.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns((FoodProductDto?)null);

        var result = await _controller.GetById(3, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtGetById()
    {
        var request = CreateRequest();
        var created = CreateDto(12);
        _foodProductService.CreateAsync(request, Arg.Any<CancellationToken>()).Returns(created);

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(FoodProductsController.GetById), createdResult.ActionName);
        Assert.Equal(12, createdResult.RouteValues?["id"]);
        Assert.Same(created, createdResult.Value);
    }

    [Fact]
    public async Task Update_WhenFoodProductExists_ReturnsOk()
    {
        var request = CreateRequest();
        var updated = CreateDto(4);
        _foodProductService.UpdateAsync(4, request, Arg.Any<CancellationToken>()).Returns(updated);

        var result = await _controller.Update(4, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(updated, ok.Value);
    }

    [Fact]
    public async Task Update_WhenFoodProductDoesNotExist_ReturnsNotFound()
    {
        var request = CreateRequest();
        _foodProductService.UpdateAsync(4, request, Arg.Any<CancellationToken>()).Returns((FoodProductDto?)null);

        var result = await _controller.Update(4, request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Theory]
    [InlineData(true, typeof(NoContentResult))]
    [InlineData(false, typeof(NotFoundResult))]
    public async Task Delete_ReturnsExpectedResult(bool deleted, Type expectedResultType)
    {
        _foodProductService.DeleteAsync(6, Arg.Any<CancellationToken>()).Returns(deleted);

        var result = await _controller.Delete(6, CancellationToken.None);

        Assert.IsType(expectedResultType, result);
    }

    private static FoodProductRequest CreateRequest() => new("Granola Bar", "Oats and honey", "Snacks", 2.49m, 190);

    private static FoodProductDto CreateDto(int id) => new(
        id,
        "Granola Bar",
        "Oats and honey",
        "Snacks",
        2.49m,
        190,
        true,
        DateTimeOffset.UnixEpoch,
        DateTimeOffset.UnixEpoch);
}
