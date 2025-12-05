using System.Security.Claims;

namespace QuizBattle.Api.Shared.Extensions
{
    public static class CurrentUserExtensions
    {
        public static int? GetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value;

            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        public static int GetRequiredUserId(this ClaimsPrincipal user)
        {
            return user.GetUserId()
                ?? throw new UnauthorizedAccessException("User ID not found in token.");
        }

        public static string? GetEmail(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Email)?.Value
                ?? user.FindFirst("email")?.Value;
        }
    }
}
