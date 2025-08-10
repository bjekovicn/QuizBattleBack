namespace QuizBattle.Infrastructure.Shared.Data
{
    public sealed class ConnectionString
    {
        public string PgHost { get; set; } = string.Empty;
        public string PgPort { get; set; } = string.Empty;
        public string PgUser { get; set; } = string.Empty;
        public string PgPassword { get; set; } = string.Empty;
        public string PgDatabase { get; set; } = string.Empty;
    }
}
