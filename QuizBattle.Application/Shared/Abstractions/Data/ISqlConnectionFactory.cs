using System.Data;

namespace QuizBattle.Application.Shared.Abstractions.Data
{
    public interface ISqlConnectionFactory
    {
        IDbConnection CreateConnection();
    }

}
