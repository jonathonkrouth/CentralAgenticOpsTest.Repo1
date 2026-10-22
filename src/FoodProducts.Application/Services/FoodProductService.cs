using FoodProducts.Application.Abstractions;
using FoodProducts.Application.Contracts;
using FoodProducts.Application.Domain;
using FoodProducts.Application.Validation;

namespace FoodProducts.Application.Services;

public sealed class FoodProductService(IFoodProductRepository repository, TimeProvider timeProvider) : IFoodProductService
{
    public async Task<IReadOnlyList<FoodProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var foodProducts = await repository.GetAllAsync(cancellationToken);
        return foodProducts.Select(FoodProductDto.FromEntity).ToList();
    }

    public async Task<FoodProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var foodProduct = await repository.GetByIdAsync(id, cancellationToken);
        return foodProduct is null ? null : FoodProductDto.FromEntity(foodProduct);
    }

    public async Task<FoodProductDto> CreateAsync(FoodProductRequest request, CancellationToken cancellationToken = default)
    {
        FoodProductRequestValidator.ValidateAndThrow(request);

        var now = timeProvider.GetUtcNow();
        var foodProduct = new FoodProduct
        {
            Name = request.Name.Trim(),
            Description = NormalizeOptional(request.Description),
            Category = request.Category.Trim(),
            Price = request.Price,
            CaloriesPerServing = request.CaloriesPerServing,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now,
        };

        foodProduct.Id = await repository.CreateAsync(foodProduct, cancellationToken);
        return FoodProductDto.FromEntity(foodProduct);
    }

    public async Task<FoodProductDto?> UpdateAsync(int id, FoodProductRequest request, CancellationToken cancellationToken = default)
    {
        FoodProductRequestValidator.ValidateAndThrow(request);

        var foodProduct = await repository.GetByIdAsync(id, cancellationToken);
        if (foodProduct is null)
        {
            return null;
        }

        foodProduct.Name = request.Name.Trim();
        foodProduct.Description = NormalizeOptional(request.Description);
        foodProduct.Category = request.Category.Trim();
        foodProduct.Price = request.Price;
        foodProduct.CaloriesPerServing = request.CaloriesPerServing;
        foodProduct.IsActive = request.IsActive;
        foodProduct.UpdatedAt = timeProvider.GetUtcNow();

        var updated = await repository.UpdateAsync(foodProduct, cancellationToken);
        return updated ? FoodProductDto.FromEntity(foodProduct) : null;
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(id, cancellationToken);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
