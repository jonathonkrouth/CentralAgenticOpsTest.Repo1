using System.Data;
using System.Data.Common;
using System.Globalization;
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
        await using var command = connection.CreateCommand();
        command.CommandText = SelectSql + " ORDER BY Name;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var foodProducts = new List<FoodProduct>();
        while (await reader.ReadAsync(cancellationToken))
        {
            foodProducts.Add(Map(reader));
        }

        return foodProducts;
    }

    public async Task<FoodProduct?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = SelectSql + " WHERE Id = @Id;";
        AddParameter(command, "@Id", DbType.Int32, id);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<int> CreateAsync(FoodProduct foodProduct, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(foodProduct);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = InsertSql;
        AddWritableParameters(command, foodProduct);
        AddParameter(command, "@CreatedAt", DbType.DateTimeOffset, foodProduct.CreatedAt);

        var newId = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(newId, CultureInfo.InvariantCulture);
    }

    public async Task<bool> UpdateAsync(FoodProduct foodProduct, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(foodProduct);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = UpdateSql;
        AddParameter(command, "@Id", DbType.Int32, foodProduct.Id);
        AddWritableParameters(command, foodProduct);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = DeleteSql;
        AddParameter(command, "@Id", DbType.Int32, id);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void AddWritableParameters(DbCommand command, FoodProduct foodProduct)
    {
        AddParameter(command, "@Name", DbType.String, foodProduct.Name);
        AddParameter(command, "@Description", DbType.String, foodProduct.Description);
        AddParameter(command, "@Category", DbType.String, foodProduct.Category);
        AddParameter(command, "@Price", DbType.Decimal, foodProduct.Price);
        AddParameter(command, "@CaloriesPerServing", DbType.Int32, foodProduct.CaloriesPerServing);
        AddParameter(command, "@IsActive", DbType.Boolean, foodProduct.IsActive);
        AddParameter(command, "@UpdatedAt", DbType.DateTimeOffset, foodProduct.UpdatedAt);
    }

    private static void AddParameter(DbCommand command, string name, DbType dbType, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = dbType;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static FoodProduct Map(DbDataReader reader)
    {
        var descriptionOrdinal = reader.GetOrdinal("Description");

        return new FoodProduct
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Description = reader.IsDBNull(descriptionOrdinal) ? null : reader.GetString(descriptionOrdinal),
            Category = reader.GetString(reader.GetOrdinal("Category")),
            Price = reader.GetDecimal(reader.GetOrdinal("Price")),
            CaloriesPerServing = reader.GetInt32(reader.GetOrdinal("CaloriesPerServing")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("UpdatedAt")),
        };
    }
}
