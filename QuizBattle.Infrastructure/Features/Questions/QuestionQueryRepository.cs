using Dapper;
using QuizBattle.Application.Features.Questions;
using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Domain.Features.Questions;

namespace QuizBattle.Infrastructure.Features.Questions
{
    internal sealed class QuestionQueryRepository : IQuestionQueryRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public QuestionQueryRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private const string SelectColumns = """
        question_id AS Id,
        language_code AS LanguageCode,
        question_text AS Text,
        answer_a AS AnswerA,
        answer_b AS AnswerB,
        answer_c AS AnswerC
        """;

        public async Task<QuestionResponse?> GetByIdAsync(QuestionId id, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = $"SELECT {SelectColumns} FROM questions WHERE question_id = @QuestionId";

            return await connection.QueryFirstOrDefaultAsync<QuestionResponse>(sql, new { QuestionId = id.Value });
        }

        public async Task<IReadOnlyList<QuestionResponse>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await GetAllAsync(null, null, cancellationToken);
        }

        public async Task<IReadOnlyList<QuestionResponse>> GetAllAsync(
            int? skip,
            int? take,
            CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = $"SELECT {SelectColumns} FROM questions ORDER BY question_id";

            if (take.HasValue)
            {
                sql += " LIMIT @Take";
                if (skip.HasValue)
                {
                    sql += " OFFSET @Skip";
                }
            }

            var questions = await connection.QueryAsync<QuestionResponse>(sql, new { Skip = skip, Take = take });
            return questions.ToList();
        }

        public async Task<bool> ExistsAsync(QuestionId id, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT EXISTS(SELECT 1 FROM questions WHERE question_id = @QuestionId)";
            return await connection.ExecuteScalarAsync<bool>(sql, new { QuestionId = id.Value });
        }

        public async Task<IReadOnlyList<QuestionResponse>> GetRandomQuestionsAsync(
            string languageCode,
            int count,
            CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = $"""
            SELECT {SelectColumns}
            FROM questions
            WHERE language_code = @LanguageCode
            ORDER BY RANDOM()
            LIMIT @Count
            """;

            var questions = await connection.QueryAsync<QuestionResponse>(
                sql,
                new { LanguageCode = languageCode, Count = count });

            return questions.ToList();
        }

        public async Task<IReadOnlyList<QuestionResponse>> GetByLanguageAsync(
            string languageCode,
            int? skip = null,
            int? take = null,
            CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = $"SELECT {SelectColumns} FROM questions WHERE language_code = @LanguageCode ORDER BY question_id";

            if (take.HasValue)
            {
                sql += " LIMIT @Take";
                if (skip.HasValue)
                {
                    sql += " OFFSET @Skip";
                }
            }

            var questions = await connection.QueryAsync<QuestionResponse>(
                sql,
                new { LanguageCode = languageCode, Skip = skip, Take = take });

            return questions.ToList();
        }

        public async Task<int> GetCountAsync(string? languageCode = null, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = "SELECT COUNT(*) FROM questions";
            if (!string.IsNullOrWhiteSpace(languageCode))
            {
                sql += " WHERE language_code = @LanguageCode";
            }

            return await connection.ExecuteScalarAsync<int>(sql, new { LanguageCode = languageCode });
        }
    }
}
