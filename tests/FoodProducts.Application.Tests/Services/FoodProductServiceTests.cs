using FoodProducts.Application.Abstractions;
using FoodProducts.Application.Contracts;
using FoodProducts.Application.Domain;
using FoodProducts.Application.Exceptions;
using FoodProducts.Application.Services;
using NSubstitute;

namespace FoodProducts.Application.Tests.Services;

public sealed class FoodProductServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IFoodProductRepository _repository = Substitute.For<IFoodProductRepository>();
    private readonly FoodProductService _service;

    public FoodProductServiceTests()
    {
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(Now);
        _service = new FoodProductService(_repository, timeProvider);
    }

    public static TheoryData<FoodProductRequest, string> InvalidRequests => new()
    {
        { ValidRequest() with { Name = " " }, nameof(FoodProductRequest.Name) },
        { ValidRequest() with { Name = new string('a', 201) }, nameof(FoodProductRequest.Name) },
        { ValidRequest() with { Description = new string('a', 1001) }, nameof(FoodProductRequest.Description) },
        { ValidRequest() with { Category = "" }, nameof(FoodProductRequest.Category) },
        { ValidRequest() with { Category = new string('a', 101) }, nameof(FoodProductRequest.Category) },
        { ValidRequest() with { Price = -0.01m }, nameof(FoodProductRequest.Price) },
        { ValidRequest() with { Price = 1.999m }, nameof(FoodProductRequest.Price) },
        { ValidRequest() with { CaloriesPerServing = -1 }, nameof(FoodProductRequest.CaloriesPerServing) },
    };

    [Fact]
    public async Task GetAllAsync_ReturnsMappedFoodProducts()
    {
        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([CreateFoodProduct(1, "Granola Bar"), CreateFoodProduct(2, "Greek Yogurt")]);

        var foodProducts = await _service.GetAllAsync();

        Assert.Collection(
            foodProducts,
            first => Assert.Equal("Granola Bar", first.Name),
            second => Assert.Equal("Greek Yogurt", second.Name));
    }

    [Fact]
    public async Task GetByIdAsync_WhenFoodProductExists_ReturnsDto()
    {
        _repository.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(CreateFoodProduct(3, "Oat Milk"));

        var foodProduct = await _service.GetByIdAsync(3);

        Assert.NotNull(foodProduct);
        Assert.Equal(3, foodProduct.Id);
        Assert.Equal("Oat Milk", foodProduct.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFoodProductDoesNotExist_ReturnsNull()
    {
        _repository.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns((FoodProduct?)null);

        var foodProduct = await _service.GetByIdAsync(3);

        Assert.Null(foodProduct);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_PersistsNormalizedFoodProduct()
    {
        _repository.CreateAsync(Arg.Any<FoodProduct>(), Arg.Any<CancellationToken>()).Returns(10);

        var foodProduct = await _service.CreateAsync(ValidRequest() with { Name = "  Trail Mix  ", Description = "   " });

        Assert.Equal(10, foodProduct.Id);
        Assert.Equal("Trail Mix", foodProduct.Name);
        Assert.Null(foodProduct.Description);
        Assert.Equal(Now, foodProduct.CreatedAt);
        Assert.Equal(Now, foodProduct.UpdatedAt);
        await _repository.Received(1).CreateAsync(
            Arg.Is<FoodProduct>(p => p.Name == "Trail Mix" && p.Description == null && p.CreatedAt == Now),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task CreateAsync_WithInvalidRequest_ThrowsValidationException(FoodProductRequest request, string expectedErrorKey)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));

        Assert.Contains(expectedErrorKey, exception.Errors.Keys);
        await _repository.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Fact]
    public async Task UpdateAsync_WhenFoodProductExists_UpdatesFieldsAndPreservesCreatedAt()
    {
        var createdAt = Now.AddDays(-30);
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(CreateFoodProduct(5, "Old Name", createdAt));
        _repository.UpdateAsync(Arg.Any<FoodProduct>(), Arg.Any<CancellationToken>()).Returns(true);

        var foodProduct = await _service.UpdateAsync(5, ValidRequest() with { Name = "New Name", Price = 4.25m });

        Assert.NotNull(foodProduct);
        Assert.Equal("New Name", foodProduct.Name);
        Assert.Equal(4.25m, foodProduct.Price);
        Assert.Equal(createdAt, foodProduct.CreatedAt);
        Assert.Equal(Now, foodProduct.UpdatedAt);
        await _repository.Received(1).UpdateAsync(
            Arg.Is<FoodProduct>(p => p.Id == 5 && p.Name == "New Name" && p.UpdatedAt == Now),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenFoodProductDoesNotExist_ReturnsNullWithoutUpdating()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((FoodProduct?)null);

        var foodProduct = await _service.UpdateAsync(5, ValidRequest());

        Assert.Null(foodProduct);
        await _repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidRequest_ThrowsBeforeLoadingFoodProduct()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(5, ValidRequest() with { Price = -1m }));

        await _repository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteAsync_ReturnsRepositoryResult(bool deleted)
    {
        _repository.DeleteAsync(8, Arg.Any<CancellationToken>()).Returns(deleted);

        var result = await _service.DeleteAsync(8);

        Assert.Equal(deleted, result);
    }

    private static FoodProductRequest ValidRequest() => new("Granola Bar", "Oats and honey", "Snacks", 2.49m, 190);

    private static FoodProduct CreateFoodProduct(int id, string name, DateTimeOffset? createdAt = null) => new()
    {
        Id = id,
        Name = name,
        Category = "Snacks",
        Price = 2.49m,
        CaloriesPerServing = 190,
        IsActive = true,
        CreatedAt = createdAt ?? Now,
        UpdatedAt = createdAt ?? Now,
    };
}
