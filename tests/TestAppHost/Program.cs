using skestock.Shared;

namespace skestock.TestAppHost;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        builder.AddSqlServer(Services.DatabaseServer)
            .AddDatabase(Services.Database);

        builder.AddRedis(Services.Cache);

        builder.Build().Run();
    }
}