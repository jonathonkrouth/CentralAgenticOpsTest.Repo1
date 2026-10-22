namespace FoodProducts.Application.Contracts;

public sealed record FoodProductRequest(
    string Name,
    string? Description,
    string Category,
    decimal Price,
    int CaloriesPerServing,
    bool IsActive = true);
