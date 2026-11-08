using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skestock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SetTemporalRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Separate from AddTemporalTables because EF enables versioning at the end of that migration, so
            // this must run after it. Retention cannot be set through EF. TEMPORAL_HISTORY_RETENTION is a database option that cannot be
            // changed inside the migration transaction, so assert it instead (a failure rolls the migration back).
            migrationBuilder.Sql(
                "IF (SELECT is_temporal_history_retention_enabled FROM sys.databases WHERE name = DB_NAME()) = 0 " +
                "THROW 50000, 'TEMPORAL_HISTORY_RETENTION is OFF; run ALTER DATABASE CURRENT SET TEMPORAL_HISTORY_RETENTION ON', 1;");

            migrationBuilder.Sql("ALTER TABLE [dbo].[AspNetRoleClaims] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_AspNetRoleClaimsHistory_Id] ON [dbo].[AspNetRoleClaimsHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[AspNetRoles] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_AspNetRolesHistory_Id] ON [dbo].[AspNetRolesHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[AspNetUserClaims] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_AspNetUserClaimsHistory_Id] ON [dbo].[AspNetUserClaimsHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[AspNetUserLogins] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_AspNetUserLoginsHistory_LoginProvider] ON [dbo].[AspNetUserLoginsHistory] ([LoginProvider], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[AspNetUserRoles] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_AspNetUserRolesHistory_UserId] ON [dbo].[AspNetUserRolesHistory] ([UserId], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[AspNetUsers] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_AspNetUsersHistory_Id] ON [dbo].[AspNetUsersHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[AspNetUserTokens] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_AspNetUserTokensHistory_UserId] ON [dbo].[AspNetUserTokensHistory] ([UserId], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[Categories] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_CategoriesHistory_Id] ON [dbo].[CategoriesHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[CategoryImportBatches] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_CategoryImportBatchesHistory_Id] ON [dbo].[CategoryImportBatchesHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[CategoryImportBatchFiles] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_CategoryImportBatchFilesHistory_Id] ON [dbo].[CategoryImportBatchFilesHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[CategoryImportBatchHistory] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_CategoryImportBatchHistoryHistory_Id] ON [dbo].[CategoryImportBatchHistoryHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[ClassBalances] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_ClassBalancesHistory_Id] ON [dbo].[ClassBalancesHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[ClassDepartment] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_ClassDepartmentHistory_Id] ON [dbo].[ClassDepartmentHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[ClassItemStockVisibilities] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_ClassItemStockVisibilitiesHistory_Id] ON [dbo].[ClassItemStockVisibilitiesHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[Congregations] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_CongregationsHistory_Id] ON [dbo].[CongregationsHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[DepartmentTemplate] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_DepartmentTemplateHistory_Id] ON [dbo].[DepartmentTemplateHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[FileMetadata] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_FileMetadataHistory_Id] ON [dbo].[FileMetadataHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[FruitDessertMessages] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_FruitDessertMessagesHistory_Id] ON [dbo].[FruitDessertMessagesHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[FruitDessertPlanEntries] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_FruitDessertPlanEntriesHistory_Id] ON [dbo].[FruitDessertPlanEntriesHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[GoodsReceiptImportLines] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_GoodsReceiptImportLinesHistory_Id] ON [dbo].[GoodsReceiptImportLinesHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[GoodsReceiptImports] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_GoodsReceiptImportsHistory_Id] ON [dbo].[GoodsReceiptImportsHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[GoodsReceipts] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_GoodsReceiptsHistory_Id] ON [dbo].[GoodsReceiptsHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[ItemImportBatches] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_ItemImportBatchesHistory_Id] ON [dbo].[ItemImportBatchesHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[ItemImportBatchFiles] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_ItemImportBatchFilesHistory_Id] ON [dbo].[ItemImportBatchFilesHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[ItemImportBatchHistory] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_ItemImportBatchHistoryHistory_Id] ON [dbo].[ItemImportBatchHistoryHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[ItemLocationThresholds] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_ItemLocationThresholdsHistory_Id] ON [dbo].[ItemLocationThresholdsHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[Items] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_ItemsHistory_Id] ON [dbo].[ItemsHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[Locations] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_LocationsHistory_Id] ON [dbo].[LocationsHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[MessageTemplates] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_MessageTemplatesHistory_Id] ON [dbo].[MessageTemplatesHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[OrderListLines] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_OrderListLinesHistory_Id] ON [dbo].[OrderListLinesHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[OrderLists] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_OrderListsHistory_Id] ON [dbo].[OrderListsHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[SchoolClasses] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_SchoolClassesHistory_Id] ON [dbo].[SchoolClassesHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[SharedClassConfigurations] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_SharedClassConfigurationsHistory_Id] ON [dbo].[SharedClassConfigurationsHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[StockBatches] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_StockBatchesHistory_Id] ON [dbo].[StockBatchesHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[StockTransactions] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_StockTransactionsHistory_Id] ON [dbo].[StockTransactionsHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[SupplyListLines] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_SupplyListLinesHistory_Id] ON [dbo].[SupplyListLinesHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[SupplyLists] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_SupplyListsHistory_Id] ON [dbo].[SupplyListsHistory] ([Id], [PeriodStart] DESC);");
            migrationBuilder.Sql("ALTER TABLE [dbo].[UserProfiles] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
            migrationBuilder.Sql("CREATE INDEX [IX_UserProfilesHistory_Id] ON [dbo].[UserProfilesHistory] ([Id], [PeriodStart] DESC);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
