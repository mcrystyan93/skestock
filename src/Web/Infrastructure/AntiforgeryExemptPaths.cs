namespace skestock.Web.Infrastructure;

/// <summary>
/// Single source of truth for request paths that must remain callable without a valid
/// antiforgery token — pre-authentication / self-service Identity flows where no meaningful
/// token can exist yet, plus the token-issuing endpoint itself.
/// </summary>
/// <remarks>
/// <see cref="MapIdentityApi{TUser}"/> maps all of its sub-routes (login, register, refresh,
/// confirmEmail, resendConfirmationEmail, forgotPassword, resetPassword, manage/*) as a single
/// route group, so individual sub-routes can't be given their own endpoint metadata (e.g.
/// <c>.DisableAntiforgery()</c>) — hence this path-based exemption list instead. If Identity
/// endpoints are ever mapped individually, prefer switching to endpoint metadata.
/// </remarks>
public static class AntiforgeryExemptPaths
{
    public static readonly string[] Prefixes =
    [
        "/api/Users/login",
        "/api/Users/register",
        "/api/Users/refresh",
        "/api/Users/confirmEmail",
        "/api/Users/resendConfirmationEmail",
        "/api/Users/forgotPassword",
        "/api/Users/resetPassword",
        "/api/Antiforgery/token",
    ];

    public static bool IsExempt(PathString path) =>
        Prefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
}
