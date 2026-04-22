using System.Data;

namespace FamilyTreeApiV2.Infrastructure.Database;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
