using FoodProducts.Application.Domain;

namespace FoodProducts.Application.Contracts;

public sealed record FoodProductDto(
    int Id,
    string Name,
    string? Description,
    string Category,
    decimal Price,
    int CaloriesPerServing,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static FoodProductDto FromEntity(FoodProduct foodProduct) => new(
        foodProduct.Id,
        foodProduct.Name,
        foodProduct.Description,
        foodProduct.Category,
        foodProduct.Price,
        foodProduct.CaloriesPerServing,
        foodProduct.IsActive,
        foodProduct.CreatedAt,
        foodProduct.UpdatedAt);
}
