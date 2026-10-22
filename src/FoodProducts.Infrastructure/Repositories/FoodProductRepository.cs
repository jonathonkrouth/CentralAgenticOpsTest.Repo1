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

        var foodProducts = await connection.QueryAsync<FoodProduct>(new CommandDefinition(
            SelectSql + " ORDER BY Name;",
            cancellationToken: cancellationToken));
        return foodProducts.AsList();
    }

    public async Task<FoodProduct?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", id, DbType.Int32);

        return await connection.QueryFirstOrDefaultAsync<FoodProduct>(new CommandDefinition(
            SelectSql + " WHERE Id = @Id;",
            parameters,
            cancellationToken: cancellationToken));
    }

    public async Task<int> CreateAsync(FoodProduct foodProduct, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(foodProduct);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = CreateWritableParameters(foodProduct);
        parameters.Add("@CreatedAt", foodProduct.CreatedAt, DbType.DateTimeOffset);

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
        parameters.Add("@Id", foodProduct.Id, DbType.Int32);
        AddWritableParameters(parameters, foodProduct);

        return await connection.ExecuteAsync(new CommandDefinition(
            UpdateSql,
            parameters,
            cancellationToken: cancellationToken)) > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", id, DbType.Int32);

        return await connection.ExecuteAsync(new CommandDefinition(
            DeleteSql,
            parameters,
            cancellationToken: cancellationToken)) > 0;
    }

    private static DynamicParameters CreateWritableParameters(FoodProduct foodProduct)
    {
        var parameters = new DynamicParameters();
        AddWritableParameters(parameters, foodProduct);
        return parameters;
    }

    private static void AddWritableParameters(DynamicParameters parameters, FoodProduct foodProduct)
    {
        parameters.Add("@Name", foodProduct.Name, DbType.String);
        parameters.Add("@Description", foodProduct.Description, DbType.String);
        parameters.Add("@Category", foodProduct.Category, DbType.String);
        parameters.Add("@Price", foodProduct.Price, DbType.Decimal);
        parameters.Add("@CaloriesPerServing", foodProduct.CaloriesPerServing, DbType.Int32);
        parameters.Add("@IsActive", foodProduct.IsActive, DbType.Boolean);
        parameters.Add("@UpdatedAt", foodProduct.UpdatedAt, DbType.DateTimeOffset);
    }
}
