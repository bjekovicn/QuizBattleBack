using MediatR;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Api.Shared.Abstractions;
using QuizBattle.Api.Shared.Extensions;
using QuizBattle.Application.Features.Questions;
using QuizBattle.Application.Features.Questions.Commands;
using QuizBattle.Application.Features.Questions.Queries;

namespace QuizBattle.Api.Features.Questions
{

    public sealed class QuestionEndpoints : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/questions")
                .WithTags("Questions")
                //.RequireAuthorization()
                .RequireRateLimiting("api");

            group.MapGet("/", GetAllQuestions)
                .WithName("GetAllQuestions");

            group.MapGet("/{id:int}", GetQuestionById)
                .WithName("GetQuestionById");

            group.MapGet("/random", GetRandomQuestions)
                .WithName("GetRandomQuestions");

            group.MapGet("/count", GetQuestionCount)
                .WithName("GetQuestionCount");

            group.MapPost("/", CreateQuestion)
                .WithName("CreateQuestion");

            group.MapPost("/bulk", CreateBulkQuestions)
                .WithName("CreateBulkQuestions");

            group.MapPut("/{id:int}", UpdateQuestion)
                .WithName("UpdateQuestion");

            group.MapDelete("/{id:int}", DeleteQuestion)
                .WithName("DeleteQuestion");
        }

        private static async Task<IResult> GetAllQuestions(
            [FromQuery] string? languageCode,
            [FromQuery] int? skip,
            [FromQuery] int? take,
            ISender sender,
            CancellationToken ct)
        {
            var query = new GetAllQuestionsQuery(languageCode, skip, take);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> GetQuestionById(
            int id,
            ISender sender,
            CancellationToken ct)
        {
            var query = new GetQuestionByIdQuery(id);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> GetRandomQuestions(
            [FromQuery] string languageCode,
            [FromQuery] int count,
            ISender sender,
            CancellationToken ct)
        {
            var query = new GetRandomQuestionsQuery(languageCode, count > 0 ? count : 10);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> GetQuestionCount(
            [FromQuery] string? languageCode,
            ISender sender,
            CancellationToken ct)
        {
            var query = new GetQuestionCountQuery(languageCode);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> CreateQuestion(
            [FromBody] CreateQuestionRequest request,
            ISender sender,
            CancellationToken ct)
        {
            var command = new CreateQuestionCommand(
                request.LanguageCode,
                request.Text,
                request.AnswerA,
                request.AnswerB,
                request.AnswerC);

            var result = await sender.Send(command, ct);

            return result.IsSuccess
                ? Results.Created($"/api/questions/{result.Value}", new { Id = result.Value })
                : result.ToHttpResult();
        }

        private static async Task<IResult> CreateBulkQuestions(
            [FromBody] List<CreateQuestionRequest> requests,
            ISender sender,
            CancellationToken ct)
        {
            var questions = requests.Select(r => new CreateQuestionDto(
                r.LanguageCode,
                r.Text,
                r.AnswerA,
                r.AnswerB,
                r.AnswerC)).ToList();

            var command = new CreateBulkQuestionsCommand(questions);
            var result = await sender.Send(command, ct);

            return result.IsSuccess
                ? Results.Created("/api/questions", new { Count = result.Value })
                : result.ToHttpResult();
        }

        private static async Task<IResult> UpdateQuestion(
            int id,
            [FromBody] UpdateQuestionRequest request,
            ISender sender,
            CancellationToken ct)
        {
            var command = new UpdateQuestionCommand(
                id,
                request.LanguageCode,
                request.Text,
                request.AnswerA,
                request.AnswerB,
                request.AnswerC);

            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> DeleteQuestion(
            int id,
            ISender sender,
            CancellationToken ct)
        {
            var command = new DeleteQuestionCommand(id);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }
    }

    // Request DTOs
    public sealed record CreateQuestionRequest(
        string LanguageCode,
        string Text,
        string AnswerA,
        string AnswerB,
        string AnswerC);

    public sealed record UpdateQuestionRequest(
        string LanguageCode,
        string Text,
        string AnswerA,
        string AnswerB,
        string AnswerC);
}
