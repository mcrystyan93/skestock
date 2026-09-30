namespace skestock.Shared;

public static class Services
{
    /// <summary>
    /// The name of the Web Frontend service.
    /// This service is responsible for hosting the frontend application.
    /// </summary>
    public const string WebFrontend = "webfrontend";

    /// <summary>
    /// The name of the Web API service.
    /// This service is responsible for hosting the Web API application.
    /// </summary>
    public const string WebApi = "webapi";

    /// <summary>
    /// The name of the Worker service.
    /// This service is responsible for running background/queued processing jobs.
    /// </summary>
    public const string Worker = "worker";

    /// <summary>
    /// The name of the Database Server service.
    /// This service is responsible for hosting the database server (e.g., PostgreSQL, SQL Server, or SQLite).
    /// </summary>
    public const string DatabaseServer = "dbserver";

    /// <summary>
    /// The name of the Database.
    /// This is the name of the database that will be created and used by the application.
    /// </summary>
    public const string Database = "skestockDb";

    /// <summary>Least-privilege login used by Web and Worker (data read/write only).</summary>
    public const string DatabaseAppLogin = "skestock_app";

    /// <summary>Login that owns the schema; used only by the one-shot migration service.</summary>
    public const string DatabaseMigratorLogin = "skestock_migrator";

    /// <summary>The one-shot service that provisions the logins and applies EF migrations.</summary>
    public const string DatabaseMigrator = "db-migrate";
    public const string MigrateArgument = "--migrate";
    public const string DatabaseMigrationSettings = "DatabaseMigration";
    public const string DatabaseAppPassword = "AppPassword";
    public const string DatabaseMigratorPassword = "MigratorPassword";
    
    public const string DatabaseVolumes = "skestock-db-data";
    
    public const string Cache = "skestock-cache";
    public const string CacheCommander = "redis-commander";
    public const string CacheVolumes = "skestock-cache-data";    
    public const string DataProtection = "DataProtection";
    public const string DataProtectionKeysDirectory = "KeysDirectory";
    public const string DataProtectionKeysVolume = "skestock-data-protection-keys";
    public const string DataProtectionKeysPath = "/var/lib/skestock/data-protection-keys";
    
    /// <summary>
    /// The name of the Storage service.
    /// This service is responsible for providing storage capabilities.
    /// </summary>
    public const string Storage = "storage";
    public const string StorageVolumes = "storage-data";
    public const string Blobs = "blobs";
    public const string BlobService = "blobservice";
    public const string Queues = "queues";
    public const string GoodsReceiptImportQueue = "goods-receipt-import";
    public const string CategoryImportQueue = "category-import";
    public const string ItemImportQueue = "item-import";
    
    public const string OpenApiSettings = "OpenApiSettings";
    public const string OpenApiKey = "ApiKey";
    public const string OpenApiModel = "Model";

    public const string ImportBatchSettings = "ImportBatch";

    public const string WorkerSettings = "Worker";

    public const string DailyStatisticsSettings = "DailyStatistics";
    public const string DailyStatisticsJobName = "daily-statistics";
    public const string DailyStatisticsLockKey = "skestock:jobs:daily-statistics";

    // Time zone that defines business calendar days (statistics day boundaries).
    public const string BusinessTimeZoneId = "Europe/Bucharest";
}
