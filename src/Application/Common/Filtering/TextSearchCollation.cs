using Microsoft.EntityFrameworkCore.Infrastructure;

namespace skestock.Application.Common.Filtering;

public static class TextSearchCollation
{
    public const string Romanian = "Romanian_100_CI_AI";
    public const string AccentInsensitive = "Latin1_General_100_CI_AI";
    private const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";

    public static bool IsSqlServer(DatabaseFacade database) =>
        string.Equals(database.ProviderName, SqlServerProvider, StringComparison.Ordinal);
}
