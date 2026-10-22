using System.Data;
using System.Globalization;
using Dapper;
using FoodProducts.Application.Abstractions;
using FoodProducts.Application.Domain;
using FoodProducts.Infrastructure.Data;

namespace FoodProducts.Infrastructure.Repositories;

public sealed class FoodProductRepository(IDbConnectionFactory connectionFactory) : IFoodProductRepository
{
    private const string SelectSql = """
        SELECT Id, Name, Description, Category, Price, CaloriesPerServing, IsActive, CreatedAt, UpdatedAt
        FROM dbo.FoodProducts
        """;

    private const string InsertSql = """
        INSERT INTO dbo.FoodProducts (Name, Description, Category, Price, CaloriesPerServing, IsActive, CreatedAt, UpdatedAt)
        OUTPUT INSERTED.Id
        VALUES (@Name, @Description, @Category, @Price, @CaloriesPerServing, @IsActive, @CreatedAt, @UpdatedAt);
        """;

    private const string UpdateSql = """
        UPDATE dbo.FoodProducts
        SET Name = @Name,
            Description = @Description,
            Category = @Category,
            Price = @Price,
            CaloriesPerServing = @CaloriesPerServing,
            IsActive = @IsActive,
            UpdatedAt = @UpdatedAt
        WHERE Id = @Id;
        """;

    private const string DeleteSql = "DELETE FROM dbo.FoodProducts WHERE Id = @Id;";

    public async Task<IReadOnlyList<FoodProduct>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var rows = await connection.QueryAsync<FoodProductRow>(new CommandDefinition(
            SelectSql + " ORDER BY Name;",
            cancellationToken: cancellationToken));

        return rows.Select(row => row.ToFoodProduct()).ToList();
    }

    public async Task<FoodProduct?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        AddParameter(parameters, "@Id", DbType.Int32, id);

        var row = await connection.QueryFirstOrDefaultAsync<FoodProductRow>(new CommandDefinition(
            SelectSql + " WHERE Id = @Id;",
            parameters,
            cancellationToken: cancellationToken));

        return row?.ToFoodProduct();
    }

    public async Task<int> CreateAsync(FoodProduct foodProduct, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(foodProduct);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        AddWritableParameters(parameters, foodProduct);
        AddParameter(parameters, "@CreatedAt", DbType.DateTimeOffset, foodProduct.CreatedAt);

        var newId = await connection.ExecuteScalarAsync<object?>(new CommandDefinition(
            InsertSql,
            parameters,
            cancellationToken: cancellationToken));

        return Convert.ToInt32(newId, CultureInfo.InvariantCulture);
    }

    public async Task<bool> UpdateAsync(FoodProduct foodProduct, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(foodProduct);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        AddParameter(parameters, "@Id", DbType.Int32, foodProduct.Id);
        AddWritableParameters(parameters, foodProduct);

        var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
            UpdateSql,
            parameters,
            cancellationToken: cancellationToken));

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        AddParameter(parameters, "@Id", DbType.Int32, id);

        var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
            DeleteSql,
            parameters,
            cancellationToken: cancellationToken));

        return rowsAffected > 0;
    }

    private static void AddWritableParameters(DynamicParameters parameters, FoodProduct foodProduct)
    {
        AddParameter(parameters, "@Name", DbType.String, foodProduct.Name);
        AddParameter(parameters, "@Description", DbType.String, foodProduct.Description);
        AddParameter(parameters, "@Category", DbType.String, foodProduct.Category);
        AddParameter(parameters, "@Price", DbType.Decimal, foodProduct.Price);
        AddParameter(parameters, "@CaloriesPerServing", DbType.Int32, foodProduct.CaloriesPerServing);
        AddParameter(parameters, "@IsActive", DbType.Boolean, foodProduct.IsActive);
        AddParameter(parameters, "@UpdatedAt", DbType.DateTimeOffset, foodProduct.UpdatedAt);
    }

    private static void AddParameter(DynamicParameters parameters, string name, DbType dbType, object? value) =>
        parameters.Add(name, value ?? DBNull.Value, dbType);

    private sealed class FoodProductRow
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Category { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int CaloriesPerServing { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public FoodProduct ToFoodProduct() => new()
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Category = Category,
            Price = Price,
            CaloriesPerServing = CaloriesPerServing,
            IsActive = IsActive,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
        };
    }
}
