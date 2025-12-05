using System.Text.Json.Serialization;

namespace QuizBattle.Application.Features.Games.RedisModels
{
    public sealed class GameRoomDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("gameType")]
        public int GameType { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("languageCode")]
        public string LanguageCode { get; set; } = "sr";

        [JsonPropertyName("totalRounds")]
        public int TotalRounds { get; set; }

        [JsonPropertyName("currentRound")]
        public int CurrentRound { get; set; }

        [JsonPropertyName("createdAt")]
        public long CreatedAt { get; set; }

        [JsonPropertyName("startedAt")]
        public long? StartedAt { get; set; }

        [JsonPropertyName("roundStartedAt")]
        public long? RoundStartedAt { get; set; }

        [JsonPropertyName("roundEndsAt")]
        public long? RoundEndsAt { get; set; }

        [JsonPropertyName("hostPlayerId")]
        public int? HostPlayerId { get; set; }

        [JsonPropertyName("players")]
        public List<GamePlayerDto> Players { get; set; } = new();

        [JsonPropertyName("questions")]
        public List<GameQuestionDto> Questions { get; set; } = new();
    }

    public sealed class GamePlayerDto
    {
        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("photoUrl")]
        public string? PhotoUrl { get; set; }

        [JsonPropertyName("colorHex")]
        public string ColorHex { get; set; } = "#FF4038";

        [JsonPropertyName("colorName")]
        public string ColorName { get; set; } = "Red";

        [JsonPropertyName("totalScore")]
        public int TotalScore { get; set; }

        [JsonPropertyName("currentRoundScore")]
        public int CurrentRoundScore { get; set; }

        [JsonPropertyName("isReady")]
        public bool IsReady { get; set; }

        [JsonPropertyName("isConnected")]
        public bool IsConnected { get; set; }

        [JsonPropertyName("currentAnswer")]
        public PlayerAnswerDto? CurrentAnswer { get; set; }

        [JsonPropertyName("joinedAt")]
        public long JoinedAt { get; set; }
    }

    public sealed class PlayerAnswerDto
    {
        [JsonPropertyName("answer")]
        public string Answer { get; set; } = string.Empty;

        [JsonPropertyName("responseTimeMs")]
        public long ResponseTimeMs { get; set; }

        [JsonPropertyName("answeredAt")]
        public long AnsweredAt { get; set; }
    }

    public sealed class GameQuestionDto
    {
        [JsonPropertyName("questionId")]
        public int QuestionId { get; set; }

        [JsonPropertyName("roundNumber")]
        public int RoundNumber { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("optionA")]
        public string OptionA { get; set; } = string.Empty;

        [JsonPropertyName("optionB")]
        public string OptionB { get; set; } = string.Empty;

        [JsonPropertyName("optionC")]
        public string OptionC { get; set; } = string.Empty;

        [JsonPropertyName("correctOption")]
        public string CorrectOption { get; set; } = "A";
    }
}
