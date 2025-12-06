using Microsoft.AspNetCore.Mvc;
using QuizBattle.Api.Shared.Abstractions;
using QuizBattle.Application.Features.Auth;
using QuizBattle.Application.Features.Auth.Commands.DTOs;
using QuizBattle.Application.Features.Users;
using QuizBattle.Application.Shared.Abstractions.Auth;
using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Domain.Features.Auth;
using QuizBattle.Domain.Features.Users;

namespace QuizBattle.Api.Features.Auth;

/// <summary>
/// Development-only authentication endpoints for testing without real OAuth providers.
/// These endpoints are ONLY available in Development environment.
/// </summary>
public sealed class DevAuthEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Only register in Development
        var env = app.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (!env.IsDevelopment())
            return;

        var group = app.MapGroup("/api/auth/dev")
            .WithTags("Development Authentication")
            .AllowAnonymous();

        group.MapPost("/login", DevLogin)
            .WithName("DevLogin")
            .WithDescription("Development-only login. Creates or retrieves a test user and returns JWT tokens.");

        group.MapPost("/create-test-user", CreateTestUser)
            .WithName("CreateTestUser")
            .WithDescription("Creates a test user with specified parameters.");

        group.MapGet("/test-token/{userId:int}", GetTestToken)
            .WithName("GetTestToken")
            .WithDescription("Generates a test JWT token for an existing user.");
    }

    private static async Task<IResult> DevLogin(
        [FromBody] DevLoginRequest request,
        IUserCommandRepository userCommandRepository,
        IUserQueryRepository userQueryRepository,
        IJwtTokenService jwtTokenService,
        ITokenHashService tokenHashService,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        HttpContext httpContext,
        CancellationToken ct)
    {
        // Try to find existing user by email or create new one
        var existingUser = !string.IsNullOrWhiteSpace(request.Email)
            ? await FindUserByEmailAsync(userQueryRepository, request.Email, ct)
            : null;

        User user;
        if (existingUser is not null)
        {
            // Get the domain entity for updating
            var domainUser = await userCommandRepository.GetByIdAsync(new UserId(existingUser.Id), ct);
            if (domainUser is null)
                return Results.NotFound("User not found");

            domainUser.UpdateLastLogin();
            user = domainUser;
        }
        else
        {
            // Create new test user with fake Google ID
            var testGoogleId = $"dev_test_{Guid.NewGuid():N}";
            user = User.CreateWithGoogle(
                testGoogleId,
                request.FirstName ?? "Test",
                request.LastName ?? "User",
                request.PhotoUrl,
                request.Email ?? $"test_{Guid.NewGuid():N}@dev.local");

            await userCommandRepository.AddAsync(user, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);

        // Generate tokens
        var userResponse = await userQueryRepository.GetByIdAsync(user.Id, ct);
        var accessToken = jwtTokenService.GenerateAccessToken(userResponse!);
        var refreshTokenPlain = jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = tokenHashService.HashToken(refreshTokenPlain);

        var refreshToken = RefreshToken.Create(
            user.Id,
            refreshTokenHash,
            jwtTokenService.GetRefreshTokenExpirationDays(),
            "DevAuth",
            httpContext.Connection.RemoteIpAddress?.ToString());

        await refreshTokenRepository.AddAsync(refreshToken, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var expiresAt = DateTimeOffset.UtcNow
            .AddDays(jwtTokenService.GetRefreshTokenExpirationDays())
            .ToUnixTimeSeconds();

        return Results.Ok(new AuthResponse(
            userResponse!,
            accessToken,
            refreshTokenPlain,
            expiresAt));
    }

    private static async Task<IResult> CreateTestUser(
        [FromBody] CreateTestUserRequest request,
        IUserCommandRepository userCommandRepository,
        IUserQueryRepository userQueryRepository,
        IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var testGoogleId = $"dev_test_{Guid.NewGuid():N}";
        var user = User.CreateWithGoogle(
            testGoogleId,
            request.FirstName,
            request.LastName,
            request.PhotoUrl,
            request.Email);

        if (request.InitialCoins > 0)
            user.AddCoins(request.InitialCoins);

        if (request.InitialTokens > 0)
            user.AddTokens(request.InitialTokens);

        await userCommandRepository.AddAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var response = await userQueryRepository.GetByIdAsync(user.Id, ct);
        return Results.Created($"/api/users/{user.Id.Value}", response);
    }

    private static async Task<IResult> GetTestToken(
        int userId,
        IUserQueryRepository userQueryRepository,
        IJwtTokenService jwtTokenService,
        ITokenHashService tokenHashService,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var user = await userQueryRepository.GetByIdAsync(new UserId(userId), ct);
        if (user is null)
            return Results.NotFound($"User with ID {userId} not found");

        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var refreshTokenPlain = jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = tokenHashService.HashToken(refreshTokenPlain);

        var refreshToken = RefreshToken.Create(
            new UserId(userId),
            refreshTokenHash,
            jwtTokenService.GetRefreshTokenExpirationDays(),
            "DevAuth-GetToken",
            httpContext.Connection.RemoteIpAddress?.ToString());

        await refreshTokenRepository.AddAsync(refreshToken, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var expiresAt = DateTimeOffset.UtcNow
            .AddDays(jwtTokenService.GetRefreshTokenExpirationDays())
            .ToUnixTimeSeconds();

        return Results.Ok(new AuthResponse(
            user,
            accessToken,
            refreshTokenPlain,
            expiresAt));
    }

    private static async Task<UserResponse?> FindUserByEmailAsync(
        IUserQueryRepository repository,
        string email,
        CancellationToken ct)
    {
        // This is a simple implementation - in production you'd have a proper method
        var users = await repository.GetAllAsync(null, 100, ct);
        return users.FirstOrDefault(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
    }
}

// Request DTOs for Dev Auth
public sealed record DevLoginRequest(
    string? Email = null,
    string? FirstName = null,
    string? LastName = null,
    string? PhotoUrl = null);

public sealed record CreateTestUserRequest(
    string FirstName,
    string LastName,
    string? Email = null,
    string? PhotoUrl = null,
    int InitialCoins = 0,
    int InitialTokens = 50);