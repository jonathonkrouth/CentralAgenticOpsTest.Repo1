namespace FoodProducts.Application.Domain;

public sealed class FoodProduct
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public required string Category { get; set; }

    public decimal Price { get; set; }

    public int CaloriesPerServing { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
