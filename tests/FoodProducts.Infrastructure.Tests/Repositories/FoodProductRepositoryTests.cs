using System.Data;
using FoodProducts.Application.Domain;
using FoodProducts.Infrastructure.Data;
using FoodProducts.Infrastructure.Repositories;
using FoodProducts.Infrastructure.Tests.Fakes;
using NSubstitute;

namespace FoodProducts.Infrastructure.Tests.Repositories;

public sealed class FoodProductRepositoryTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 10, 8, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = new(2026, 1, 12, 16, 45, 0, TimeSpan.Zero);

    private readonly FakeDbCommand _command = new();
    private readonly FoodProductRepository _repository;

    public FoodProductRepositoryTests()
    {
        var connectionFactory = Substitute.For<IDbConnectionFactory>();
        connectionFactory.CreateConnection().Returns(_ => new FakeDbConnection(_command));
        _repository = new FoodProductRepository(connectionFactory);
    }

    [Fact]
    public async Task GetAllAsync_MapsEveryRow()
    {
        _command.ReaderResult = CreateResultTable(
            CreateFoodProduct(1, "Granola Bar", description: null),
            CreateFoodProduct(2, "Greek Yogurt", description: "Plain, whole milk"));

        var foodProducts = await _repository.GetAllAsync();

        Assert.Contains("FROM dbo.FoodProducts", _command.CommandText);
        Assert.Contains("ORDER BY Name", _command.CommandText);
        Assert.Collection(
            foodProducts,
            first =>
            {
                Assert.Equal(1, first.Id);
                Assert.Equal("Granola Bar", first.Name);
                Assert.Null(first.Description);
            },
            second =>
            {
                Assert.Equal(2, second.Id);
                Assert.Equal("Greek Yogurt", second.Name);
                Assert.Equal("Plain, whole milk", second.Description);
            });
    }

    [Fact]
    public async Task GetByIdAsync_WhenRowExists_ReturnsMappedFoodProduct()
    {
        _command.ReaderResult = CreateResultTable(CreateFoodProduct(7, "Oat Milk", description: "Barista edition"));

        var foodProduct = await _repository.GetByIdAsync(7);

        Assert.NotNull(foodProduct);
        Assert.Equal(7, foodProduct.Id);
        Assert.Equal("Oat Milk", foodProduct.Name);
        Assert.Equal("Barista edition", foodProduct.Description);
        Assert.Equal("Snacks", foodProduct.Category);
        Assert.Equal(3.49m, foodProduct.Price);
        Assert.Equal(150, foodProduct.CaloriesPerServing);
        Assert.True(foodProduct.IsActive);
        Assert.Equal(CreatedAt, foodProduct.CreatedAt);
        Assert.Equal(UpdatedAt, foodProduct.UpdatedAt);
        Assert.Contains("WHERE Id = @Id", _command.CommandText);
        Assert.Equal(7, _command.Parameters["@Id"].Value);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNoRowExists_ReturnsNull()
    {
        _command.ReaderResult = CreateResultTable();

        var foodProduct = await _repository.GetByIdAsync(99);

        Assert.Null(foodProduct);
    }

    [Fact]
    public async Task CreateAsync_SendsParametersAndReturnsNewId()
    {
        _command.ScalarResult = 42;

        var id = await _repository.CreateAsync(CreateFoodProduct(0, "Trail Mix", description: null));

        Assert.Equal(42, id);
        Assert.Contains("INSERT INTO dbo.FoodProducts", _command.CommandText);
        Assert.Equal("Trail Mix", _command.Parameters["@Name"].Value);
        Assert.Equal(DBNull.Value, _command.Parameters["@Description"].Value);
        Assert.Equal("Snacks", _command.Parameters["@Category"].Value);
        Assert.Equal(3.49m, _command.Parameters["@Price"].Value);
        Assert.Equal(150, _command.Parameters["@CaloriesPerServing"].Value);
        Assert.True(Assert.IsType<bool>(_command.Parameters["@IsActive"].Value));
        Assert.Equal(CreatedAt, _command.Parameters["@CreatedAt"].Value);
        Assert.Equal(UpdatedAt, _command.Parameters["@UpdatedAt"].Value);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public async Task UpdateAsync_ReturnsWhetherARowWasUpdated(int rowsAffected, bool expected)
    {
        _command.NonQueryResult = rowsAffected;

        var updated = await _repository.UpdateAsync(CreateFoodProduct(5, "Rice Cakes", description: "Lightly salted"));

        Assert.Equal(expected, updated);
        Assert.Contains("UPDATE dbo.FoodProducts", _command.CommandText);
        Assert.Equal(5, _command.Parameters["@Id"].Value);
        Assert.Equal("Rice Cakes", _command.Parameters["@Name"].Value);
        Assert.Equal("Lightly salted", _command.Parameters["@Description"].Value);
        Assert.False(_command.Parameters.Contains("@CreatedAt"));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public async Task DeleteAsync_ReturnsWhetherARowWasDeleted(int rowsAffected, bool expected)
    {
        _command.NonQueryResult = rowsAffected;

        var deleted = await _repository.DeleteAsync(3);

        Assert.Equal(expected, deleted);
        Assert.Contains("DELETE FROM dbo.FoodProducts", _command.CommandText);
        Assert.Equal(3, _command.Parameters["@Id"].Value);
    }

    private static FoodProduct CreateFoodProduct(int id, string name, string? description) => new()
    {
        Id = id,
        Name = name,
        Description = description,
        Category = "Snacks",
        Price = 3.49m,
        CaloriesPerServing = 150,
        IsActive = true,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt,
    };

    private static DataTable CreateResultTable(params FoodProduct[] foodProducts)
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Description", typeof(string));
        table.Columns.Add("Category", typeof(string));
        table.Columns.Add("Price", typeof(decimal));
        table.Columns.Add("CaloriesPerServing", typeof(int));
        table.Columns.Add("IsActive", typeof(bool));
        table.Columns.Add("CreatedAt", typeof(DateTimeOffset));
        table.Columns.Add("UpdatedAt", typeof(DateTimeOffset));

        foreach (var foodProduct in foodProducts)
        {
            table.Rows.Add(
                foodProduct.Id,
                foodProduct.Name,
                (object?)foodProduct.Description ?? DBNull.Value,
                foodProduct.Category,
                foodProduct.Price,
                foodProduct.CaloriesPerServing,
                foodProduct.IsActive,
                foodProduct.CreatedAt,
                foodProduct.UpdatedAt);
        }

        return table;
    }
}
