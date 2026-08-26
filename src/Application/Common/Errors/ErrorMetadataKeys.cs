namespace skestock.Application.Common.Errors;

public static class ErrorMetadataKeys
{
    public const string StatusCode = nameof(StatusCode);
    public const string Title = nameof(Title);

    /// <summary>Stable, machine-readable error code, e.g. "users.not_found".</summary>
    public const string Code = nameof(Code);

    /// <summary>camelCase interpolation params for the frontend, e.g. <c>{ userId: 5 }</c>.</summary>
    public const string Params = nameof(Params);

    public const string UserId = nameof(UserId);
    public const string UserName = nameof(UserName);
    public const string TypeName = nameof(TypeName);
    public const string EntityKind = nameof(EntityKind);
}
