// using Microsoft.AspNetCore.Antiforgery;
// using Microsoft.AspNetCore.Http.HttpResults;
//
// namespace skestock.Web.Endpoints;
//
// public class Antiforgery : IEndpointGroup
// {
//     public static void Map(RouteGroupBuilder groupBuilder)
//     {
//         groupBuilder.MapGet(GetToken, "token");
//     }
//
//     [EndpointSummary("Get an antiforgery token")]
//     [EndpointDescription(
//         "Issues an antiforgery token pair and writes the request token as a JS-readable " +
//         "'XSRF-TOKEN' cookie. Callable anonymously so the SPA can seed the cookie before " +
//         "login/register, and must be called again immediately after login/logout since the " +
//         "token is bound to the current principal.")]
//     public static Ok GetToken(IAntiforgery antiforgery, HttpContext context)
//     {
//         var tokens = antiforgery.GetAndStoreTokens(context);
//         context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
//         {
//             HttpOnly = false,
//             SameSite = SameSiteMode.Strict,
//             Secure = !context.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase),
//         });
//
//         return TypedResults.Ok();
//     }
// }
