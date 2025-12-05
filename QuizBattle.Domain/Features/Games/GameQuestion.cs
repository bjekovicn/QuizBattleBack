using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Domain.Features.Games
{
    public sealed class GameQuestion : ValueObject<GameQuestion>
    {
        public int QuestionId { get; }
        public int RoundNumber { get; }
        public string Text { get; }
        public string OptionA { get; }
        public string OptionB { get; }
        public string OptionC { get; }
        public string CorrectOption { get; } 

        private GameQuestion()
        {
            Text = string.Empty;
            OptionA = string.Empty;
            OptionB = string.Empty;
            OptionC = string.Empty;
            CorrectOption = "A";
        }

        private GameQuestion(
            int questionId,
            int roundNumber,
            string text,
            string optionA,
            string optionB,
            string optionC,
            string correctOption)
        {
            QuestionId = questionId;
            RoundNumber = roundNumber;
            Text = text;
            OptionA = optionA;
            OptionB = optionB;
            OptionC = optionC;
            CorrectOption = correctOption;
        }

        public static GameQuestion CreateShuffled(int questionId, int roundNumber, string text,
            string correctAnswer, string wrongAnswer1, string wrongAnswer2)
        {
            var answers = new List<(string Answer, bool IsCorrect)>
        {
            (correctAnswer, true),
            (wrongAnswer1, false),
            (wrongAnswer2, false)
        };

            // Fisher-Yates shuffle
            var random = Random.Shared;
            for (int i = answers.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (answers[i], answers[j]) = (answers[j], answers[i]);
            }

            string correctOption = answers[0].IsCorrect ? "A" :
                                   answers[1].IsCorrect ? "B" : "C";

            return new GameQuestion(
                questionId,
                roundNumber,
                text,
                answers[0].Answer,
                answers[1].Answer,
                answers[2].Answer,
                correctOption);
        }

        public bool IsCorrectAnswer(string answer) =>
            string.Equals(CorrectOption, answer, StringComparison.OrdinalIgnoreCase);

        public string GetCorrectAnswerText() => CorrectOption switch
        {
            "A" => OptionA,
            "B" => OptionB,
            "C" => OptionC,
            _ => throw new InvalidOperationException("Invalid correct option.")
        };

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return QuestionId;
            yield return RoundNumber;
        }
    }
}
