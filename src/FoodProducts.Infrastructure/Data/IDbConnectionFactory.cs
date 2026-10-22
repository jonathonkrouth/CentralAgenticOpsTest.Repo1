using System.Data.Common;

namespace FoodProducts.Infrastructure.Data;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
