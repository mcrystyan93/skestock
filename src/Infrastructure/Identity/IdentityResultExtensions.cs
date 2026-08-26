using FluentResults;
using Microsoft.AspNetCore.Identity;

namespace skestock.Infrastructure.Identity;

public static class IdentityResultExtensions
{
    public static Result ToApplicationResult(this IdentityResult result)
    {
        return result.Succeeded
            ? Result.Ok()
            : Result.Fail(result.Errors.Select(e => e.Description));
    }
}
