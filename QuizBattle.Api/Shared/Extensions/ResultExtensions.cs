using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Api.Shared.Extensions
{

    public static class ResultExtensions
    {
        public static IResult ToHttpResult(this Result result)
        {
            return result.IsSuccess
                ? Results.Ok()
                : Results.Problem(
                    statusCode: GetStatusCode(result.Error),
                    title: result.Error.Code,
                    detail: result.Error.Message);
        }

        public static IResult ToHttpResult<T>(this Result<T> result)
        {
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    statusCode: GetStatusCode(result.Error),
                    title: result.Error.Code,
                    detail: result.Error.Message);
        }

        private static int GetStatusCode(Error error) => error.Code switch
        {
            "User.NotFound" => StatusCodes.Status404NotFound,
            "User.AlreadyExists" => StatusCodes.Status409Conflict,
            "Question.NotFound" => StatusCodes.Status404NotFound,
            "Game.NotFound" => StatusCodes.Status404NotFound,
            "Game.Full" => StatusCodes.Status400BadRequest,
            "Game.PlayerAlreadyInGame" => StatusCodes.Status409Conflict,
            "Validation" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
