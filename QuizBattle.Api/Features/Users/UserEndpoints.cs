using MediatR;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Api.Shared.Abstractions;
using QuizBattle.Api.Shared.Extensions;
using QuizBattle.Application.Features.Users;
using QuizBattle.Application.Features.Users.Commands;
using QuizBattle.Application.Features.Users.Queries;
using QuizBattle.Domain.Features.Users;

namespace QuizBattle.Api.Features.Users
{

    public sealed class UserEndpoints : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/users")
                .WithTags("Users")
                //.RequireAuthorization()
                .RequireRateLimiting("api");

            group.MapGet("/", GetAllUsers)
                .WithName("GetAllUsers");

            group.MapGet("/{id:int}", GetUserById)
                .WithName("GetUserById");

            group.MapPost("/", CreateUser)
                .WithName("CreateUser");

            group.MapPost("/login", LoginUser)
                .WithName("LoginUser");

            group.MapPut("/{id:int}", UpdateUser)
                .WithName("UpdateUser");

            group.MapDelete("/{id:int}", DeleteUser)
                .WithName("DeleteUser");

            group.MapGet("/leaderboard", GetLeaderboard)
                .WithName("GetLeaderboard");

            group.MapPost("/{id:int}/device-token", RegisterDeviceToken)
                .WithName("RegisterDeviceToken");

            group.MapDelete("/{id:int}/device-token", RemoveDeviceToken)
                .WithName("RemoveDeviceToken");
        }

        private static async Task<IResult> GetAllUsers(
            [FromQuery] int? skip,
            [FromQuery] int? take,
            ISender sender,
            CancellationToken ct)
        {
            var query = new GetAllUsersQuery(skip, take);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> GetUserById(
            int id,
            ISender sender,
            CancellationToken ct)
        {
            var query = new GetUserByIdQuery(id);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> CreateUser(
            [FromBody] CreateUserRequest request,
            ISender sender,
            CancellationToken ct)
        {
            var command = new CreateUserCommand(
                request.GoogleId,
                request.AppleId,
                request.FirstName,
                request.LastName,
                request.Photo,
                request.Email);

            var result = await sender.Send(command, ct);

            return result.IsSuccess
                ? Results.Created($"/api/users/{result.Value}", new { Id = result.Value })
                : result.ToHttpResult();
        }

        private static async Task<IResult> LoginUser(
            [FromBody] LoginUserRequest request,
            ISender sender,
            CancellationToken ct)
        {
            var command = new LoginUserCommand(
                request.GoogleId,
                request.AppleId,
                request.FirstName,
                request.LastName,
                request.Photo,
                request.Email,
                request.DeviceToken,
                request.DevicePlatform.HasValue ? (DevicePlatform)request.DevicePlatform.Value : null);

            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> UpdateUser(
            int id,
            [FromBody] UpdateUserRequest request,
            ISender sender,
            CancellationToken ct)
        {
            var command = new UpdateUserCommand(id, request.FirstName, request.LastName, request.Photo);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> DeleteUser(
            int id,
            ISender sender,
            CancellationToken ct)
        {
            var command = new DeleteUserCommand(id);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> GetLeaderboard(
            [FromQuery] int take,
            ISender sender,
            CancellationToken ct)
        {
            var query = new GetLeaderboardQuery(take > 0 ? take : 10);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> RegisterDeviceToken(
            int id,
            [FromBody] RegisterDeviceTokenRequest request,
            ISender sender,
            CancellationToken ct)
        {
            var command = new RegisterDeviceTokenCommand(id, request.Token, (DevicePlatform)request.Platform);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> RemoveDeviceToken(
            int id,
            [FromBody] RemoveDeviceTokenRequest request,
            ISender sender,
            CancellationToken ct)
        {
            var command = new RemoveDeviceTokenCommand(id, request.Token);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }
    }

    // Request DTOs
    public sealed record CreateUserRequest(
        string? GoogleId,
        string? AppleId,
        string? FirstName,
        string? LastName,
        string? Photo,
        string? Email);

    public sealed record LoginUserRequest(
        string? GoogleId,
        string? AppleId,
        string? FirstName,
        string? LastName,
        string? Photo,
        string? Email,
        string? DeviceToken,
        int? DevicePlatform);

    public sealed record UpdateUserRequest(
        string? FirstName,
        string? LastName,
        string? Photo);

    public sealed record RegisterDeviceTokenRequest(
        string Token,
        int Platform);

    public sealed record RemoveDeviceTokenRequest(string Token);
}