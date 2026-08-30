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
    /// The name of the Database Server service.
    /// This service is responsible for hosting the database server (e.g., PostgreSQL, SQL Server, or SQLite).
    /// </summary>
    public const string DatabaseServer = "dbserver";

    /// <summary>
    /// The name of the Database.
    /// This is the name of the database that will be created and used by the application.
    /// </summary>
    public const string Database = "skestockDb";
    
    public const string DatabaseVolumes = "skestock-db-data";
    
    public const string Cache = "skestock-cache";
    
    public const string CacheVolumes = "skestock-cache-data";    
    
    /// <summary>
    /// The name of the Storage service.
    /// This service is responsible for providing storage capabilities.
    /// </summary>
    public const string Storage = "storage";
    public const string StorageVolumes = "storage-data";
}
