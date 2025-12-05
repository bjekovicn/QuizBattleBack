using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Api.Shared.Abstractions;
using QuizBattle.Api.Shared.Extensions;
using QuizBattle.Application.Features.Auth;
using QuizBattle.Application.Features.Auth.Commands;
using QuizBattle.Application.Features.Auth.Queries;

namespace QuizBattle.Api.Features.Auth
{

    public sealed class AuthEndpoints : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth")
                .WithTags("Authentication")
                .RequireRateLimiting("auth");

            // Public endpoints
            group.MapPost("/google", LoginWithGoogle)
                .WithName("LoginWithGoogle")
                .AllowAnonymous();

            group.MapPost("/apple", LoginWithApple)
                .WithName("LoginWithApple")
                .AllowAnonymous();

            group.MapPost("/refresh", RefreshToken)
                .WithName("RefreshToken")
                .AllowAnonymous();

            // Protected endpoints
            group.MapPost("/logout", Logout)
                .WithName("Logout")
                .RequireAuthorization();

            group.MapPost("/logout-all", LogoutAllSessions)
                .WithName("LogoutAllSessions")
                .RequireAuthorization();

            group.MapGet("/sessions", GetActiveSessions)
                .WithName("GetActiveSessions")
                .RequireAuthorization();

            group.MapDelete("/sessions/{sessionId:guid}", RevokeSession)
                .WithName("RevokeSession")
                .RequireAuthorization();
        }

        private static async Task<IResult> LoginWithGoogle(
            [FromBody] GoogleLoginRequest request,
            HttpContext httpContext,
            ISender sender,
            CancellationToken ct)
        {
            var command = new LoginWithGoogleCommand(
                request.IdToken,
                request.DeviceToken,
                request.DevicePlatform.HasValue
                    ? (Domain.Features.Users.DevicePlatform)request.DevicePlatform.Value
                    : null,
                request.DeviceInfo,
                httpContext.Connection.RemoteIpAddress?.ToString());

            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> LoginWithApple(
            [FromBody] AppleLoginRequest request,
            HttpContext httpContext,
            ISender sender,
            CancellationToken ct)
        {
            var command = new LoginWithAppleCommand(
                request.IdToken,
                request.FirstName,
                request.LastName,
                request.DeviceToken,
                request.DevicePlatform.HasValue
                    ? (Domain.Features.Users.DevicePlatform)request.DevicePlatform.Value
                    : null,
                request.DeviceInfo,
                httpContext.Connection.RemoteIpAddress?.ToString());

            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> RefreshToken(
            [FromBody] RefreshTokenRequest request,
            HttpContext httpContext,
            ISender sender,
            CancellationToken ct)
        {
            var command = new RefreshTokenCommand(
                request.AccessToken,
                request.RefreshToken,
                request.DeviceInfo,
                httpContext.Connection.RemoteIpAddress?.ToString());

            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> Logout(
            [FromBody] LogoutRequest request,
            ISender sender,
            CancellationToken ct)
        {
            var command = new LogoutCommand(request.RefreshToken);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> LogoutAllSessions(
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct)
        {
            var userId = user.GetRequiredUserId();
            var command = new LogoutAllSessionsCommand(userId);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> GetActiveSessions(
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct)
        {
            var userId = user.GetRequiredUserId();
            var query = new GetActiveSessionsQuery(userId);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> RevokeSession(
            Guid sessionId,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct)
        {
            var userId = user.GetRequiredUserId();
            var command = new RevokeSessionCommand(userId, sessionId);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }
    }

    // Request DTOs
    public sealed record GoogleLoginRequest(
        string IdToken,
        string? DeviceToken,
        int? DevicePlatform,
        string? DeviceInfo);

    public sealed record AppleLoginRequest(
        string IdToken,
        string? FirstName,
        string? LastName,
        string? DeviceToken,
        int? DevicePlatform,
        string? DeviceInfo);

    public sealed record RefreshTokenRequest(
        string AccessToken,
        string RefreshToken,
        string? DeviceInfo);

    public sealed record LogoutRequest(string RefreshToken);
}
