# Graph Report - Client  (2026-09-20)

## Corpus Check
- 358 files · ~564,817 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 2401 nodes · 4512 edges · 166 communities (154 shown, 10 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS · INFERRED: 4 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `db8bbc84`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- school-class-overview.page.ts
- review-modal.ts
- ske
- problem-detail.feature.ts
- class-analytics.page.ts
- scan.py
- DynamicSizeVirtualScrollStrategy
- item-import.store.ts
- Signal Forms
- Core Patterns
- @angular/core
- Architecture — Deep Dive
- .NET quick checklist
- BaseTableWithFilter
- ErrorAlert
- school-class.ts
- withProblemDetailsFeature
- goods-receipts.feature.ts
- items/list/tables/import/large/table.ts
- StockItemDto
- angular-developer/SKILL.md
- category-import.ts
- item-import-review.store.ts
- NgRx SignalStore - Core Examples
- package.json
- stock-category-cards-container.ts
- stock.ts
- @angular/router
- errors/index.ts
- item-import.ts
- goods-receipt.ts
- item-list-tab.ts
- add-stock-batch-modal.ts
- order-list.ts
- icon-catalog.ts
- Command Reference
- MCP Server — Complete Reference
- NgRx SignalStore - Migration Examples
- @ngrx/signals
- Copilot Instructions — skestock
- Angular Aria
- NgRx SignalStore - Effects Examples
- auth.http.ts
- category-list-tab.ts
- school-class-detail.store.ts
- Testing — Complete Reference
- dependencies
- category-detail.store.ts
- PAGINATION_PAGE_SIZE
- item-detail-modal.ts
- stock-batch-collection.feature.ts
- Deployment — Complete Reference
- Aspire — Polyglot Distributed-App Orchestration
- FilterForm
- Community (CommunityToolkit/Aspire)
- NgRx SignalStore - Testing Examples
- categories.page.ts
- storage.http.ts
- NgRx SignalStore - Entity Examples
- theme-switcher.ts
- CategoryImportListSmall
- order-lists-tab.ts
- GetAllSchoolClassesRequest
- 2. Advanced CSS Animations
- Dashboard — Complete Reference
- NgRx SignalStore - Custom Features Examples
- NgRx SignalStore - Reference
- devDependencies
- scripts
- app.config.ts
- stock/ui/list/filter/filter-form.ts
- CategoryDto
- ItemDto
- school-classes/ui/modals/form.ts
- adjust/form.ts
- Acquire Codebase Knowledge
- aspire/SKILL.md
- Debugging strategies
- Common Issues & Solutions
- GetAllCategoriesRequest
- review.store.ts
- header-container.ts
- Theme
- category-dropdown.ts
- categories/ui/modals/detail/form.ts
- Core Tasks
- Component Styling
- Angular Developer Guidelines
- login.page.ts
- categories/list/tables/import/large/table.ts
- ColumnFilter
- LocationsHttp
- school-classes.page.ts
- ng-zorro-antd Component Documentation (English)
- Core Sections (Required)
- Core Sections (Required)
- Core Sections (Required)
- Core Sections (Required)
- Core Sections (Required)
- Components
- Angular CLI MCP Server
- Template-Driven Forms
- class-analytics.store.ts
- category-import-review-line-row.ts
- ItemDropdown
- Core Sections (Required)
- Angular CLI Guide for Agents
- Creating and Using Services
- Data Resolvers
- Define Routes
- Inputs
- Reactive Forms
- Manual Setup (Tailwind v4)
- rxjs
- simple.routes.ts
- SchoolClassDto
- ItemsHttp
- base-table-with-filter.ts
- angular-guidelines.instructions.md
- Core Sections (Required)
- Inquiry Checkpoints
- Stack Detection Reference
- Dependency Injection (DI) Fundamentals
- Route Loading Strategies
- Outputs (Custom Events)
- Async Reactivity with `resource`
- Setting Up for Router Testing
- Angular Signals Overview
- Ske
- pagination.ts
- directives/index.ts
- Defining Dependency Providers
- Environment configuration
- Navigate to Routes
- Rendering Strategies
- Route Transition Animations
- Show Routes with Outlets
- Features
- GetAllCategoryImportBatchesRequest
- item-dropdown.ts
- LoaderDirective
- acquire-codebase-knowledge/SKILL.md
- Side Effects with `effect` and `afterRenderEffect`
- Hierarchical Injectors
- Component Host Elements
- Router Lifecycle and Events
- Anti-Patterns
- Decision Framework
- StopClick
- home.routes.ts
- StockAdjustmentModal
- aspnetcore-https.js
- Dependent State with `linkedSignal`
- Automatic Migrations & Code Modernization
- Testing Fundamentals
- generate-icon-catalog.mjs
- FilterForm
- CategoryImportModal
- CategoryImportReviewModal
- ItemImportModal
- ItemImportReviewModal
- AddStockBatchModal
- StockBooleanField
- proxy.conf.js

## God Nodes (most connected - your core abstractions)
1. `@angular/core` - 177 edges
2. `rxjs` - 61 edges
3. `@ngrx/signals` - 45 edges
4. `withProblemDetailsFeature()` - 39 edges
5. `lodash-es` - 38 edges
6. `withLoadingFeature()` - 38 edges
7. `@ngrx/operators` - 30 edges
8. `ColumnFilter` - 29 edges
9. `ItemDto` - 26 edges
10. `BaseTableWithFilter` - 26 edges

## Surprising Connections (you probably didn't know these)
- `buildConfirmRequest()` --calls--> `toDateOnlyString()`  [EXTRACTED]
  src/app/shared/goods-receipt-imports/services/review.store.ts → src/app/core/models/base.ts
- `buildEditableLine()` --calls--> `getDateForShelfLife()`  [EXTRACTED]
  src/app/shared/goods-receipt-imports/services/review.store.ts → src/app/core/models/base.ts
- `buildCategoryImportBatchListFilter()` --calls--> `prioritizeSort()`  [EXTRACTED]
  src/app/core/models/category-import.ts → src/app/core/models/pagination.ts
- `CategoryImportState` --calls--> `buildCategoryImportBatchListFilter()`  [EXTRACTED]
  src/app/shared/categories/services/category-import.store.ts → src/app/core/models/category-import.ts
- `withCategoryCollection()` --calls--> `buildCategoryListFilter()`  [EXTRACTED]
  src/app/shared/categories/services/category-collection.feature.ts → src/app/core/models/category.ts

## Import Cycles
- 3-file cycle: `src/app/shared/errors/index.ts -> src/app/shared/errors/ui/error-alert.ts -> src/app/shared/errors/ui/problem-detail/problem-detail-text.ts -> src/app/shared/errors/index.ts`

## Communities (166 total, 10 thin omitted)

### Community 0 - "school-class-overview.page.ts"
Cohesion: 0.06
Nodes (21): @microsoft/signalr, RealtimeGroup, realtimeGroups, signalrEvents, GroupManagerState, initialState, SignalRGroupManagerStore, SignalRBridge (+13 more)

### Community 1 - "review-modal.ts"
Cohesion: 0.06
Nodes (30): GetAllGoodsReceiptImportsRequest, GoodsReceiptImportListItemDto, GoodsReceiptImportReviewDto, GoodsReceiptImportListStore, GoodsReceiptImportState, initialState, buildConfirmRequest(), GoodsReceiptImportReviewTarget (+22 more)

### Community 2 - "ske"
Cohesion: 0.04
Nodes (48): build, serve, test, builder, configurations, defaultConfiguration, options, cli (+40 more)

### Community 3 - "problem-detail.feature.ts"
Cohesion: 0.07
Nodes (35): AuthErrorCode, CommonErrorCode, ErrorCode, ErrorCodes, ResourceErrorCode, StockErrorCode, ValidationErrorCode, ValueOf (+27 more)

### Community 4 - "class-analytics.page.ts"
Cohesion: 0.06
Nodes (30): apexcharts, ng-apexcharts, CategoryStockSummaryDto, ClassAnalysisFilter, ClassAnalysisFilterFormData, ClassGoodsReceiptCostsDto, ClassStockByCategoryDto, GoodsReceiptCostPointDto (+22 more)

### Community 5 - "scan.py"
Cohesion: 0.08
Nodes (41): collect_code_metrics(), detect_ci_cd_pipelines(), detect_containers(), detect_monorepo(), detect_performance_markers(), detect_security_configs(), find_entry_points(), find_env_templates() (+33 more)

### Community 6 - "DynamicSizeVirtualScrollStrategy"
Cohesion: 0.07
Nodes (8): Input, CdkDynamicSizeVirtualScroll, DynamicSizeVirtualScrollStrategy, attach(), FakeViewport, Directive, DynamicVirtualScrollItem, Directive

### Community 7 - "item-import.store.ts"
Cohesion: 0.10
Nodes (19): buildItemImportBatchListFilter(), ConfirmItemImportBatchRequest, CreateItemImportBatchRequest, GetAllItemImportBatchesRequest, ItemImportBatchDto, ItemImportBatchListItemDto, ItemImportFilterContainer, Component (+11 more)

### Community 8 - "Signal Forms"
Cohesion: 0.06
Nodes (34): Accessing State, Async Validation, Big Form Example, Binding, Common Pitfalls (DO NOT DO THESE), Conditional Validation, Context, Creating a Form (+26 more)

### Community 9 - "Core Patterns"
Cohesion: 0.06
Nodes (35): Available Hooks, Available Updaters, Benefits, Common Use Cases, Core Patterns, CRITICAL: Before Managing State with NgRx SignalStore, CRITICAL REMINDERS, Feature Ordering (+27 more)

### Community 10 - "@angular/core"
Cohesion: 0.12
Nodes (15): @angular/core, @angular/forms, lodash-es, getDateForShelfLife(), PaginatedResponse, CategoryImportFilterModel, CategoryListFilterModel, ITEM_FILTER_FIELDS (+7 more)

### Community 11 - "Architecture — Deep Dive"
Cohesion: 0.06
Nodes (30): Annotations, Architecture — Deep Dive, Available events, Built-in health checks, Configuration, Configuration sources, Configuring non-.NET services, Connection strings (+22 more)

### Community 12 - ".NET quick checklist"
Cohesion: 0.07
Nodes (29): Assertions, Async Programming Best Practices, Build, C# version, Cloud-native / cloud-ready, Code coverage (dotnet-coverage), Code Design Rules, Do first (+21 more)

### Community 13 - "BaseTableWithFilter"
Cohesion: 0.09
Nodes (14): @angular/common, PaginationSort, STOCK_BATCH_TABLE_COLUMNS, Table, Component, initialState, StockBatchListStore, StockBatchState (+6 more)

### Community 14 - "ErrorAlert"
Cohesion: 0.10
Nodes (18): MoveStockRequest, ErrorAlert, Component, initialState, stockMoveApiEvents, StockMoveState, StockMoveForm, StockMoveFormModel (+10 more)

### Community 15 - "school-class.ts"
Cohesion: 0.10
Nodes (17): CLASS_STATUS_COLORS, CLASS_STATUS_LABELS, ClassStatus, Active, Closed, Paused, Upcoming, SCHOOL_CLASS_TABLE_COLUMNS (+9 more)

### Community 16 - "withProblemDetailsFeature"
Cohesion: 0.14
Nodes (19): buildGoodsReceiptImportListFilter(), realtimeEvents, initialState, initialState, SchoolClassOverviewState, initialState, SchoolClassSummaryState, withSchoolClassSummaryFeature() (+11 more)

### Community 17 - "goods-receipts.feature.ts"
Cohesion: 0.12
Nodes (16): buildGoodsReceiptListFilter(), CreateGoodsReceiptImportRequest, CreateGoodsReceiptRequest, GetAllGoodsReceiptsRequest, GoodsReceiptImportDto, GoodsReceiptImportFiles, GoodsReceiptImportModalData, GoodsReceiptImportState (+8 more)

### Community 18 - "items/list/tables/import/large/table.ts"
Cohesion: 0.13
Nodes (10): ITEM_IMPORT_BATCH_STATUS_COLORS, ITEM_IMPORT_BATCH_STATUS_LABELS, ItemImportBatchFileDto, ItemImportBatchStatus, ItemImportTable, Component, ItemImportListSmall, Component (+2 more)

### Community 19 - "StockItemDto"
Cohesion: 0.13
Nodes (14): STOCK_TABLE_COLUMNS, StockItemCategoryGroup, StockItemDto, StockCategoryCard, Component, StockCategoryCardsContainer, Component, StockCategoryCards (+6 more)

### Community 20 - "angular-developer/SKILL.md"
Cohesion: 0.09
Nodes (18): Example: Testing with a `MatButtonHarness`, Key Concepts, Testing with Component Harnesses, Using a Harness in a Unit Test, Why Use Harnesses?, Custom & Enterprise Testing Tools, End-to-End (E2E) Testing, Setting Up and Running E2E Tests (+10 more)

### Community 21 - "category-import.ts"
Cohesion: 0.14
Nodes (15): buildCategoryImportBatchListFilter(), CategoryImportBatchMutationDto, CategoryImportBatchReviewDto, CategoryImportBatchTableColumn, CategoryImportReviewLineDto, ConfirmCategoryImportBatchRequest, ConfirmCategoryImportBatchResponse, CreateCategoryImportBatchRequest (+7 more)

### Community 22 - "item-import-review.store.ts"
Cohesion: 0.12
Nodes (15): ConfirmItemImportBatchResponse, ItemImportBatchReviewDto, ItemImportReviewLineDto, initialState, ItemImportReviewEditableLine, ItemImportReviewState, nextRowId(), ItemImportReviewLineRow (+7 more)

### Community 23 - "NgRx SignalStore - Core Examples"
Cohesion: 0.10
Nodes (21): Bad Example - Direct Mutation and Magic Numbers, Bad Example - FormGroup in withState (v19+ Breaking Change), Bad Example - Subscribing and Manual Change Detection, Component-Level Stores, Good Example - FormGroup with withProps (v19+ Breaking Change), Good Example - Injecting Services in withMethods, Good Example - Lifecycle Hooks for Initialization, Good Example - Multiple patchState Patterns (+13 more)

### Community 24 - "package.json"
Cohesion: 0.10
Nodes (20): name, packageManager, private, version, @angular/build, @angular/cli, @angular/compiler, @angular/compiler-cli (+12 more)

### Community 25 - "stock-category-cards-container.ts"
Cohesion: 0.14
Nodes (10): getDropdownFilterValue(), StockPreferencesService, Service, initialState, StockState, StockStore, FilterContainer, Component (+2 more)

### Community 26 - "stock.ts"
Cohesion: 0.17
Nodes (11): AdjustStockRequest, GetClassLocationStockRequest, RemoveExpiredStockRequest, SetClassItemStockVisibilityRequest, StockReportDto, StockTableColumn, initialState, StockAdjustmentState (+3 more)

### Community 27 - "@angular/router"
Cohesion: 0.13
Nodes (9): @angular/router, authGuard(), Full, fullRoutes, Component, categoriesRoutes, classAnalyticsRoutes, itemsRoutes (+1 more)

### Community 28 - "errors/index.ts"
Cohesion: 0.15
Nodes (13): @ngrx/operators, AuthState, initialState, initialState, ItemCollectionState, withItemCollection(), initialState, LoadItemRequest (+5 more)

### Community 29 - "item-import.ts"
Cohesion: 0.19
Nodes (15): buildCategoryListFilter(), CategoryTableColumn, buildItemListFilter(), ConfirmItemImportRequestItem, ITEM_IMPORT_BATCH_TABLE_COLUMNS, ItemImportBatchTableColumn, ItemDropdownValue, ItemTableColumn (+7 more)

### Community 30 - "goods-receipt.ts"
Cohesion: 0.13
Nodes (15): ConfirmGoodsReceiptImportLineRequest, CreateGoodsReceiptLineRequest, GOODS_RECEIPT_IMPORT_STATUS_COLORS, GOODS_RECEIPT_IMPORT_STATUS_LABELS, GOODS_RECEIPT_IMPORT_TABLE_COLUMNS, GOODS_RECEIPT_TABLE_COLUMNS, GoodsReceiptDto, GoodsReceiptImportReviewLineDto (+7 more)

### Community 31 - "item-list-tab.ts"
Cohesion: 0.13
Nodes (10): GetAllItemsRequest, ITEM_TABLE_COLUMNS, FilterContainer, Component, Table, Component, ItemListSmall, Component (+2 more)

### Community 32 - "add-stock-batch-modal.ts"
Cohesion: 0.14
Nodes (10): LocationDto, LocationDropdown, Component, AddStockBatchModalData, Form, StockBatchFormModel, StockBatchFormSubmit, Component (+2 more)

### Community 33 - "order-list.ts"
Cohesion: 0.11
Nodes (12): buildOrderListFilter(), CreateOrderListRequest, ORDER_LIST_TABLE_COLUMNS, OrderListDto, OrderListLineDto, OrderListLineRequest, OrderListListItemDto, OrderListStatus (+4 more)

### Community 34 - "icon-catalog.ts"
Cohesion: 0.18
Nodes (10): buildIconAssetPath(), ICON_ASSET_NAMES, ICON_CATALOG, IconPickerValue, toHumanReadableIconName(), toIconFileName(), IconPicker, IconPickerFormModel (+2 more)

### Community 35 - "Command Reference"
Cohesion: 0.11
Nodes (19): `aspire add`, `aspire cache`, `aspire config`, `aspire deploy` (Preview), `aspire do` (Preview), `aspire init`, `aspire mcp`, `aspire mcp init` (+11 more)

### Community 36 - "MCP Server — Complete Reference"
Cohesion: 0.11
Nodes (19): AppHost management tools, Debugging with AI assistance, Excluding Resources from MCP, Fallback for documentation (13.1 users), Integration tools, Limitations, MCP Server — Complete Reference, MCP Tools (+11 more)

### Community 37 - "NgRx SignalStore - Migration Examples"
Cohesion: 0.11
Nodes (19): Good Example - Hybrid Approach During Migration, Migration Checklist, NgRx SignalStore - Migration Examples, Pattern 1: Migrating Actions to Methods, Pattern 2: Migrating Reducers to patchState, Pattern 3: Migrating Selectors to withComputed, Pattern 4: Migrating Effects to rxMethod, Pattern 5: Migrating Entity State (+11 more)

### Community 38 - "@ngrx/signals"
Cohesion: 0.13
Nodes (14): @ngrx/signals, LoadingKey, SetLoadedKey, SetLoadingKey, initialState, OrderListCollectionState, withOrderListCollection(), initialState (+6 more)

### Community 39 - "Copilot Instructions — skestock"
Cohesion: 0.11
Nodes (17): AppHost resource graph (`src/AppHost/Program.cs`), Architecture & dependency direction, Auth, Caching (`src/Application/Common/Caching/`), Copilot Instructions — skestock, Custom agents available (`.github/agents/`), Endpoints (`src/Web/Endpoints/`), Errors & the `Result<T>` pattern (`src/Application/Common/Errors/`) (+9 more)

### Community 40 - "Angular Aria"
Cohesion: 0.11
Nodes (17): 10. Integration with Signal Forms, 1. Accordion, 2. Listbox, 3. Combobox, Select, and Multiselect, 4. Menu and Menubar, 5. Tabs, 6. Toolbar, 7. Tree (+9 more)

### Community 41 - "NgRx SignalStore - Effects Examples"
Cohesion: 0.11
Nodes (18): Bad Example - async/await Without Cancellation, Good Example - Connecting Signal to rxMethod, Good Example - Coordinated Effects, Good Example - Different Input Types, Good Example - Polling with Stop Capability, Good Example - Retry Logic with exponentialBackoff, Good Example - rxMethod with switchMap, Good Example - Using signalMethod Without RxJS (+10 more)

### Community 42 - "auth.http.ts"
Cohesion: 0.14
Nodes (9): appInitializer(), AntiforgeryHttp, Service, AuthHttp, Service, Credentials, UserInfo, LoginPage (+1 more)

### Community 43 - "category-list-tab.ts"
Cohesion: 0.15
Nodes (9): CATEGORY_TABLE_COLUMNS, Table, Component, CategoryListSmall, Component, CategoryListTab, Component, BaseList (+1 more)

### Community 44 - "school-class-detail.store.ts"
Cohesion: 0.19
Nodes (9): CreateSchoolClassRequest, UpdateSchoolClassRequest, initialState, NEW_SCHOOL_CLASS_ROUTE_ID, schoolClassApiEvents, SchoolClassDetailState, SchoolClassesHttp, Service (+1 more)

### Community 45 - "Testing — Complete Reference"
Cohesion: 0.12
Nodes (16): API integration test, Basic health check test, Best practices, Connection string access, Core pattern: DistributedApplicationTestingBuilder, Customizing the test AppHost, Exclude resources, MSTest examples (+8 more)

### Community 46 - "dependencies"
Cohesion: 0.12
Nodes (17): dependencies, @angular/common, @angular/compiler, @angular/core, @angular/forms, @angular/platform-browser, @angular/router, apexcharts (+9 more)

### Community 47 - "category-detail.store.ts"
Cohesion: 0.19
Nodes (11): CategoryMutationDto, CreateCategoryRequest, UpdateCategoryRequest, CategoriesHttp, Service, categoryApiEvents, CategoryDetailState, initialState (+3 more)

### Community 48 - "PAGINATION_PAGE_SIZE"
Cohesion: 0.15
Nodes (13): PAGINATION_PAGE_SIZE, PaginatedResponseData, buildSchoolClassListFilter(), initialState, LocationCollectionState, withLocationCollection(), initialState, LocationDropdownState (+5 more)

### Community 49 - "item-detail-modal.ts"
Cohesion: 0.15
Nodes (11): CreateItemRequest, EditItemRequest, itemApiEvents, ItemDetailState, NEW_ITEM_ROUTE_ID, Form, ItemFormModel, Component (+3 more)

### Community 50 - "stock-batch-collection.feature.ts"
Cohesion: 0.17
Nodes (12): CreateStockBatchRequest, GetAllStockBatchesRequest, StockBatchListItemDto, initialState, StockBatchCollectionState, withStockBatchCollection(), initialState, stockBatchApiEvents (+4 more)

### Community 51 - "Deployment — Complete Reference"
Cohesion: 0.12
Nodes (15): Azure App Service, Azure Container Apps, Azure DevOps example, CI/CD integration, Conditional resources, Deployment — Complete Reference, Dev Containers & GitHub Codespaces, Docker (+7 more)

### Community 52 - "Aspire — Polyglot Distributed-App Orchestration"
Cohesion: 0.12
Nodes (16): 1. Researching Aspire Documentation, 2. Prerequisites & Install, 3. Project Templates, 4. AppHost Quick Start (Polyglot), 5. Core Concepts (Summary), 6. CLI Quick Reference, 7. Common Patterns, 8. Key URLs (+8 more)

### Community 53 - "FilterForm"
Cohesion: 0.16
Nodes (5): buildEqualsFilter(), FilterForm, Component, FilterForm, Component

### Community 54 - "Community (CommunityToolkit/Aspire)"
Cohesion: 0.13
Nodes (15): Bun, Community (CommunityToolkit/Aspire), Complete mixed-language example, Dapr, Deno, Go, Hosting model differences, Java (Spring Boot) (+7 more)

### Community 55 - "NgRx SignalStore - Testing Examples"
Cohesion: 0.13
Nodes (15): Good Example - Avoiding onInit Side Effects in Tests, Good Example - Mocking HttpClient in Store Tests, Good Example - Testing Async Effects, Good Example - Testing Component with Store, Good Example - Testing Store Methods, Good Example - Testing withEntities Store, Good Example - Using unprotected for State Access (v19.1+), NgRx SignalStore - Testing Examples (+7 more)

### Community 56 - "categories.page.ts"
Cohesion: 0.24
Nodes (9): CategoryImportBatchListItemDto, CategoryImportFilterContainer, Component, Header, Component, CategoryImportListTab, Component, CategoryListState (+1 more)

### Community 57 - "storage.http.ts"
Cohesion: 0.20
Nodes (8): ConfirmUploadRequest, FileDownloadResult, FileMetadataDto, FileStatus, RequestUploadRequest, UploadRequestResult, StorageHttp, Service

### Community 58 - "NgRx SignalStore - Entity Examples"
Cohesion: 0.14
Nodes (14): Bad Example - Array Instead of withEntities, Good Example - addEntities, updateEntities, removeEntities, Good Example - entityConfig for Non-Standard IDs (v18+), Good Example - Multiple Collections with Named Entities, Good Example - New Entity Operations, Good Example - Sorted Entity Collection, Good Example - withEntities for Todo Collection, NgRx SignalStore - Entity Examples (+6 more)

### Community 59 - "theme-switcher.ts"
Cohesion: 0.16
Nodes (7): Header, Component, ThemeSwitcherFormComponent, ThemeToggleFormModel, Component, ThemeSwitcher, Component

### Community 60 - "CategoryImportListSmall"
Cohesion: 0.16
Nodes (5): CategoryImportBatchStatus, Table, Component, CategoryImportListSmall, Component

### Community 61 - "order-lists-tab.ts"
Cohesion: 0.23
Nodes (7): GetAllOrderListsRequest, FilterContainer, Component, FilterForm, OrderListFilterModel, Component, OrderListListStore

### Community 62 - "GetAllSchoolClassesRequest"
Cohesion: 0.19
Nodes (7): GetAllSchoolClassesRequest, initialState, SchoolClassDropdownState, SchoolClassDropdownStore, SchoolClassDropdown, SchoolClassDropdownFormModel, Component

### Community 63 - "2. Advanced CSS Animations"
Cohesion: 0.15
Nodes (12): 1. Native CSS Animations (v20.2+ Recommended), 2. Advanced CSS Animations, 3. Legacy Animations DSL (Deprecated), Angular Animations, `animate.enter` and `animate.leave`, Animating Auto Height, Animating State and Styles, Defining Transitions (+4 more)

### Community 64 - "Dashboard — Complete Reference"
Cohesion: 0.15
Nodes (13): Authentication, Configure your services, Copilot integration, Dashboard — Complete Reference, Dashboard configuration, Dashboard URL, Docker Compose example, JavaScript (OpenTelemetry SDK) (+5 more)

### Community 65 - "NgRx SignalStore - Custom Features Examples"
Cohesion: 0.15
Nodes (13): Good Example - Combining Multiple Custom Features, Good Example - Feature Requiring Specific State, Good Example - Multiple Call States, Good Example - Redux DevTools Support, Good Example - Reusable Loading State Feature, Good Example - Using ngrx-toolkit withCallState, NgRx SignalStore - Custom Features Examples, Pattern 1: Basic signalStoreFeature (+5 more)

### Community 66 - "NgRx SignalStore - Reference"
Cohesion: 0.15
Nodes (13): DevTools Usage, Entity Lookups, From Traditional NgRx to SignalStore, Migration Guide, NgRx SignalStore - Reference, Performance Considerations, RED FLAGS, rxMethod Cleanup (+5 more)

### Community 67 - "devDependencies"
Cohesion: 0.15
Nodes (13): devDependencies, @angular/build, @angular/cli, @angular/compiler-cli, jsdom, less, postcss, prettier (+5 more)

### Community 68 - "scripts"
Cohesion: 0.15
Nodes (13): scripts, build, build:prod, dev, generate:icons, ng, prebuild, prestart (+5 more)

### Community 69 - "app.config.ts"
Cohesion: 0.18
Nodes (7): @angular/platform-browser, appConfig, routes, authInterceptor(), provideSignalR(), RealtimeEventName, realtimeEventNames

### Community 70 - "stock/ui/list/filter/filter-form.ts"
Cohesion: 0.21
Nodes (6): CategoryDropdownValue, CategoryDropdown, Component, ItemFormSubmit, STOCK_FILTER_FIELDS, StockListFilterModel

### Community 71 - "CategoryDto"
Cohesion: 0.22
Nodes (3): CategoryDto, CategoriesPage, Component

### Community 72 - "ItemDto"
Cohesion: 0.22
Nodes (3): ItemDto, ItemsPage, Component

### Community 73 - "school-classes/ui/modals/form.ts"
Cohesion: 0.18
Nodes (7): CLASS_STATUS_OPTIONS, Form, SchoolClassFormModel, SchoolClassFormSubmit, Component, SchoolClassDetailModal, Component

### Community 74 - "adjust/form.ts"
Cohesion: 0.15
Nodes (11): ADJUSTMENT_REASON_OPTIONS, AdjustmentReason, Adjustment, Damaged, Expired, Found, Miscount, Other (+3 more)

### Community 75 - "Acquire Codebase Knowledge"
Cohesion: 0.17
Nodes (12): Acquire Codebase Knowledge, Anti-Patterns, Bundled Assets, Enhanced Scan Output Sections, Focus Area Mode, Gotchas, Output Contract (Required), Phase 1: Scan and Read Intent (+4 more)

### Community 76 - "aspire/SKILL.md"
Cohesion: 0.24
Nodes (5): Categories at a glance, Discovering integrations (MCP tools), Integration pattern, Integrations Catalog, Workflow

### Community 77 - "Debugging strategies"
Cohesion: 0.17
Nodes (12): 1. Check the dashboard first, 2. Check environment variables, 3. Read console logs, 4. Check the DAG, 5. Use MCP for AI-assisted debugging, 6. Isolate the problem, Debugging strategies, Diagnostic Codes (+4 more)

### Community 78 - "Common Issues & Solutions"
Cohesion: 0.17
Nodes (12): Build & configuration, Common Issues & Solutions, Container runtime, Dashboard, Deployment, Go workloads, Health checks & startup, Java workloads (+4 more)

### Community 79 - "GetAllCategoriesRequest"
Cohesion: 0.20
Nodes (5): GetAllCategoriesRequest, FilterContainer, Component, FilterForm, Component

### Community 80 - "review.store.ts"
Cohesion: 0.23
Nodes (8): ConfirmGoodsReceiptImportRequest, GoodsReceiptImportsHttp, Service, buildEditableLine(), initialState, nextRowId(), ReviewState, ReviewStore

### Community 81 - "header-container.ts"
Cohesion: 0.18
Nodes (6): SchoolClassSummary, Header, Component, SchoolClassOverviewStore, AddGoodsReceiptModal, Component

### Community 82 - "Theme"
Cohesion: 0.26
Nodes (6): Theme, compact, dark, default, ThemeService, Service

### Community 83 - "category-dropdown.ts"
Cohesion: 0.20
Nodes (7): CategoryDropdownState, CategoryDropdownStore, initialState, CategoryDropdownFormModel, CategoryDetailModal, Component, CategoryFormModel

### Community 84 - "categories/ui/modals/detail/form.ts"
Cohesion: 0.18
Nodes (7): CategoryFormSubmit, Form, Component, SkeletonInputLoader, SkeletonInputLoaderDirective, Component, Directive

### Community 85 - "Core Tasks"
Cohesion: 0.18
Nodes (10): Analysis Order, C#/.NET Janitor, Code Modernization, Code Quality, Core Tasks, Documentation, Documentation Resources, Execution Rules (+2 more)

### Community 86 - "Component Styling"
Cohesion: 0.18
Nodes (10): Component Styling, Defining Styles, External Styles, `:host`, `:host-context()`, `::ng-deep`, Special Selectors, Styles in Templates (+2 more)

### Community 87 - "Angular Developer Guidelines"
Cohesion: 0.18
Nodes (11): Angular Aria, Angular Developer Guidelines, Components, Creating New Projects, Dependency Injection, Forms, Reactivity and Data Management, Routing (+3 more)

### Community 88 - "login.page.ts"
Cohesion: 0.27
Nodes (5): App, Component, AuthStore, LoginForm, Component

### Community 89 - "categories/list/tables/import/large/table.ts"
Cohesion: 0.24
Nodes (7): CATEGORY_IMPORT_BATCH_STATUS_COLORS, CATEGORY_IMPORT_BATCH_STATUS_LABELS, CATEGORY_IMPORT_BATCH_TABLE_COLUMNS, CategoryImportBatchFileDto, TableContainer, Component, CategoryImportReviewState

### Community 90 - "ColumnFilter"
Cohesion: 0.22
Nodes (6): GoodsReceiptListItemDto, ColumnFilter, GoodsReceiptsTab, Component, StockList, Component

### Community 91 - "LocationsHttp"
Cohesion: 0.18
Nodes (5): CreateLocationRequest, GetAllLocationsRequest, UpdateLocationRequest, LocationsHttp, Service

### Community 92 - "school-classes.page.ts"
Cohesion: 0.27
Nodes (6): FilterContainer, Component, Header, Component, initialState, SchoolClassListState

### Community 93 - "ng-zorro-antd Component Documentation (English)"
Cohesion: 0.20
Nodes (9): Animations, Component Usage Standards, Core ng-zorro Rules, Date Components (Critical in v22), Internationalization and Direction, ng-zorro-antd Component Documentation (English), Recommended Documentation Source, Theming (+1 more)

### Community 94 - "Core Sections (Required)"
Cohesion: 0.20
Nodes (9): 1) Architectural Style, 2) System Flow, 3) Layer/Module Responsibilities, 4) Reused Patterns, 5) Known Architectural Risks, 6) Evidence, Architecture, Core Sections (Required) (+1 more)

### Community 95 - "Core Sections (Required)"
Cohesion: 0.20
Nodes (9): 1) Naming Rules, 2) Formatting and Linting, 3) Import and Module Conventions, 4) Error and Logging Conventions, 5) Testing Conventions, 6) Evidence, Coding Conventions, Core Sections (Required) (+1 more)

### Community 96 - "Core Sections (Required)"
Cohesion: 0.20
Nodes (9): 1) Integration Inventory, 2) Data Stores, 3) Secrets and Credentials Handling, 4) Reliability and Failure Behavior, 5) Observability for Integrations, 6) Evidence, Core Sections (Required), Extended Sections (Optional) (+1 more)

### Community 97 - "Core Sections (Required)"
Cohesion: 0.20
Nodes (9): 1) Runtime Summary, 2) Production Frameworks and Dependencies, 3) Development Toolchain, 4) Key Commands, 5) Environment and Config, 6) Evidence, Core Sections (Required), Extended Sections (Optional) (+1 more)

### Community 98 - "Core Sections (Required)"
Cohesion: 0.20
Nodes (9): 1) Test Stack and Commands, 2) Test Layout, 3) Test Scope Matrix, 4) Mocking and Isolation Strategy, 5) Coverage and Quality Signals, 6) Evidence, Core Sections (Required), Extended Sections (Optional) (+1 more)

### Community 99 - "Components"
Cohesion: 0.20
Nodes (9): Component Definition, Components, Conditional Rendering (`@if`), Core Concepts, Loops (`@for`), Metadata Options, Switching Content (`@switch`), Template Control Flow (+1 more)

### Community 100 - "Angular CLI MCP Server"
Cohesion: 0.20
Nodes (9): Angular CLI MCP Server, Antigravity IDE, Available Tools (Default), Command Options, Configuration, Cursor, Experimental Tools, Gemini CLI (+1 more)

### Community 101 - "Template-Driven Forms"
Cohesion: 0.20
Nodes (9): Building the Form Template, Core Directives, Form and Control State, Resetting the Form, Setup, Submitting the Form, Template-Driven Forms, Two-Way Binding with `[(ngModel)]` (+1 more)

### Community 102 - "class-analytics.store.ts"
Cohesion: 0.22
Nodes (7): toDateOnlyString(), withQueryParamsSync(), ClassAnalyticsHttp, Service, AnalyticsState, ClassAnalyticsStore, initialState

### Community 103 - "category-import-review-line-row.ts"
Cohesion: 0.24
Nodes (7): CategoryImportReviewEditableLine, CategoryImportReviewLineRow, Component, CategoryImportReviewTable, CategoryReviewLinesFormModel, CategoryReviewLinesSubmitResult, Component

### Community 105 - "Core Sections (Required)"
Cohesion: 0.22
Nodes (8): 1) Top-Level Map, 2) Entry Points, 3) Module Boundaries, 4) Naming and Organization Rules, 5) Evidence, Codebase Structure, Core Sections (Required), Extended Sections (Optional)

### Community 106 - "Angular CLI Guide for Agents"
Cohesion: 0.22
Nodes (8): 1. Managing Dependencies, 2. Generating Code (`ng generate` or `ng g`), 3. Development Server & Proxying, 4. Building the Application, 5. Testing, 6. Deployment, Angular CLI Guide for Agents, Backend API Proxying

### Community 107 - "Creating and Using Services"
Cohesion: 0.22
Nodes (8): Advanced Service Patterns, Creating a Service, Creating and Using Services, Injecting a Service, Injecting into a Component, Injecting into Another Service, The `autoProvided` option, The `@Service` decorator

### Community 108 - "Data Resolvers"
Cohesion: 0.22
Nodes (8): 1. Via `ActivatedRoute` (Traditional), 2. Via Component Inputs (Modern), Accessing Resolved Data, Best Practices, Configuring the Route, Creating a Resolver, Data Resolvers, Error Handling

### Community 109 - "Define Routes"
Cohesion: 0.22
Nodes (8): Basic Configuration, Define Routes, Matching Strategy, Nested (Child) Routes, Page Titles, Redirects, Route Data and Providers, URL Paths

### Community 110 - "Inputs"
Cohesion: 0.22
Nodes (8): Best Practices, Configuration Options, Decorator-based Inputs (@Input), Inputs, Model Inputs (Two-Way Binding), Signal-based Inputs, Usage, Usage in Template

### Community 111 - "Reactive Forms"
Cohesion: 0.22
Nodes (8): Accessing Controls, Core Classes, Manual State Management, Reactive Forms, Setup, Template Binding, Unified Change Events, Updating Values

### Community 112 - "Manual Setup (Tailwind v4)"
Cohesion: 0.22
Nodes (8): 1. Install Dependencies, 2. Configure PostCSS, 3. Import Tailwind CSS, 4. Use Utility Classes, Automated Setup (Recommended), Manual Setup (Tailwind v4), Summary for AI Agents, Using Tailwind CSS with Angular

### Community 113 - "rxjs"
Cohesion: 0.25
Nodes (5): rxjs, CategoryImportStateModel, initialState, FileUpload, Component

### Community 114 - "simple.routes.ts"
Cohesion: 0.22
Nodes (5): guestGuard(), simpleRoutes, Simple, Component, loginRoutes

### Community 115 - "SchoolClassDto"
Cohesion: 0.33
Nodes (3): SchoolClassDto, SchoolClassesPage, Component

### Community 117 - "base-table-with-filter.ts"
Cohesion: 0.28
Nodes (5): SCROLL_THRESHOLD_PX, initialTableDimensions, initialTableDimensions, VirtualData, TableDimensions

### Community 118 - "angular-guidelines.instructions.md"
Cohesion: 0.25
Nodes (7): Accessibility Requirements, Angular Best Practices, Components, Services, State Management, Templates, TypeScript Best Practices

### Community 119 - "Core Sections (Required)"
Cohesion: 0.25
Nodes (8): 1) Top Risks (Prioritized), 2) Technical Debt, 3) Security Concerns, 4) Performance and Scaling Concerns, 5) Fragile/High-Churn Areas, 6) `[ASK USER]` Questions, 7) Evidence, Core Sections (Required)

### Community 120 - "Inquiry Checkpoints"
Cohesion: 0.25
Nodes (8): 1. STACK.md — Tech Stack, 2. STRUCTURE.md — Directory Layout, 3. ARCHITECTURE.md — Patterns, 4. CONVENTIONS.md — Coding Standards, 5. INTEGRATIONS.md — External Services, 6. TESTING.md — Test Setup, 7. CONCERNS.md — Known Issues, Inquiry Checkpoints

### Community 121 - "Stack Detection Reference"
Cohesion: 0.25
Nodes (8): Docker Base Image → Runtime, Framework Detection (Node.js / TypeScript), Framework Detection (Python), Language Runtime Version Detection, Manifest File → Ecosystem, Monorepo Detection, Stack Detection Reference, TypeScript Path Alias Detection

### Community 122 - "Dependency Injection (DI) Fundamentals"
Cohesion: 0.25
Nodes (7): Creating a Service, Dependency Injection (DI) Fundamentals, How DI Works in Angular, Injecting Dependencies, Services, The `inject()` Function, Where can `inject()` be used? (Injection Context)

### Community 123 - "Route Loading Strategies"
Cohesion: 0.25
Nodes (7): Eager Loading, Injection Context and Lazy Loading, Lazy Loading, Lazy Loading Child Routes, Lazy Loading Components, Recommendation, Route Loading Strategies

### Community 124 - "Outputs (Custom Events)"
Cohesion: 0.25
Nodes (7): Best Practices, Configuration Options, Decorator-based Outputs (@Output), Function-based outputs, Outputs (Custom Events), Programmatic Subscription, Usage in Template

### Community 125 - "Async Reactivity with `resource`"
Cohesion: 0.25
Nodes (7): Aborting Requests, Async Reactivity with `resource`, Basic Usage, Local Mutation, Reactive Data Fetching with `httpResource`, Reloading Data, Resource Status Signals

### Community 126 - "Setting Up for Router Testing"
Cohesion: 0.25
Nodes (7): Best Practices, Example Setup, Example: Testing Navigation, Key Concepts, Setting Up for Router Testing, Testing with the RouterTestingHarness, Writing Router Tests

### Community 127 - "Angular Signals Overview"
Cohesion: 0.25
Nodes (7): Angular Signals Overview, Async Operations in Reactive Contexts, Computed Signals (`computed`), Exposing as Readonly, Reactive Contexts, Untracked Reads (`untracked`), Writable Signals (`signal`)

### Community 129 - "Ske"
Cohesion: 0.25
Nodes (7): Additional Resources, Building, Code scaffolding, Development server, Running end-to-end tests, Running unit tests, Ske

### Community 130 - "pagination.ts"
Cohesion: 0.25
Nodes (7): ColumnDefinitionData, DisplayColumnFilter, FieldType, FilterOperator, OperatorItem, OPERATORS_BY_TYPE, SelectType

### Community 131 - "directives/index.ts"
Cohesion: 0.29
Nodes (4): LayoutBreakpoint, TestHost, Component, Directive

### Community 132 - "Defining Dependency Providers"
Cohesion: 0.29
Nodes (6): Automatic Provision, Defining Dependency Providers, InjectionToken, Library Pattern: `provide*` functions, Manual Provision, Scopes of Providers

### Community 133 - "Environment configuration"
Cohesion: 0.29
Nodes (6): Build-time configuration, Choosing a strategy, Configuration strategies, Environment configuration, Example, Runtime configuration (advanced)

### Community 134 - "Navigate to Routes"
Cohesion: 0.29
Nodes (6): Declarative Navigation (`RouterLink`), Navigate to Routes, Programmatic Navigation (`Router`), `router.navigate()`, `router.navigateByUrl()`, URL Parameters

### Community 135 - "Rendering Strategies"
Cohesion: 0.29
Nodes (6): 1. Client-Side Rendering (CSR), 2. Static Site Generation (SSG / Prerendering), 3. Server-Side Rendering (SSR), Decision Matrix, Hydration, Rendering Strategies

### Community 136 - "Route Transition Animations"
Cohesion: 0.29
Nodes (6): Advanced Control, Best Practices, Customizing with CSS, Enabling View Transitions, How it Works, Route Transition Animations

### Community 137 - "Show Routes with Outlets"
Cohesion: 0.29
Nodes (6): Basic Usage, Named Outlets (Secondary Routes), Nested Outlets, Outlet Lifecycle Events, Passing Data via `routerOutletData`, Show Routes with Outlets

### Community 138 - "Features"
Cohesion: 0.29
Nodes (7): Console logs, Distributed traces, Features, GenAI Visualizer, Metrics, Resources view, Structured logs

### Community 139 - "GetAllCategoryImportBatchesRequest"
Cohesion: 0.38
Nodes (3): GetAllCategoryImportBatchesRequest, FilterForm, Component

### Community 140 - "item-dropdown.ts"
Cohesion: 0.29
Nodes (3): ItemDropdownOption, ItemDropdownStore, ItemDropdownFormModel

### Community 141 - "LoaderDirective"
Cohesion: 0.33
Nodes (4): Loader, LoaderDirective, Component, Directive

### Community 143 - "Side Effects with `effect` and `afterRenderEffect`"
Cohesion: 0.33
Nodes (5): Basic Usage, DOM Manipulation with `afterRenderEffect`, Render Phases, Side Effects with `effect` and `afterRenderEffect`, When to use `effect`

### Community 144 - "Hierarchical Injectors"
Cohesion: 0.33
Nodes (5): Hierarchical Injectors, `providers` vs `viewProviders`, Resolution Modifiers, Resolution Rules, Types of Injector Hierarchies

### Community 145 - "Component Host Elements"
Cohesion: 0.33
Nodes (5): Binding Collisions, Binding to the Host Element, Component Host Elements, Injecting Host Attributes, Legacy Decorators

### Community 146 - "Router Lifecycle and Events"
Cohesion: 0.33
Nodes (5): Common Router Events (Chronological), Common Use Cases, Debugging, Router Lifecycle and Events, Subscribing to Events

### Community 147 - "Anti-Patterns"
Cohesion: 0.33
Nodes (6): Anti-Patterns, Array Instead of withEntities, Async Without rxMethod, Direct State Mutation, External State Modification, Wrong Feature Order

### Community 148 - "Decision Framework"
Cohesion: 0.33
Nodes (6): Decision Framework, Quick Reference Table, rxMethod vs async/await in Methods, signalStore vs Traditional NgRx, When to Use SignalStore vs Alternatives, withState vs withEntities

### Community 149 - "StopClick"
Cohesion: 0.40
Nodes (3): HostListener, StopClick, Directive

### Community 150 - "home.routes.ts"
Cohesion: 0.33
Nodes (3): HomePage, Component, homeRoutes

### Community 151 - "StockAdjustmentModal"
Cohesion: 0.40
Nodes (3): StockAdjustmentFormModel, StockAdjustmentModal, Component

### Community 152 - "aspnetcore-https.js"
Cohesion: 0.40
Nodes (4): certFilePath, fs, keyFilePath, path

### Community 153 - "Dependent State with `linkedSignal`"
Cohesion: 0.40
Nodes (4): Advanced Usage: Accounting for Previous State, Basic Usage, Dependent State with `linkedSignal`, When to use `linkedSignal` vs `computed` vs `effect`

### Community 154 - "Automatic Migrations & Code Modernization"
Cohesion: 0.40
Nodes (4): Automatic Migrations & Code Modernization, Common Migration Schematics, Discovering Migrations, Specialized Workflow: Migrating to Standalone

### Community 155 - "Testing Fundamentals"
Cohesion: 0.40
Nodes (4): Basic Test Structure Example, Core Philosophy: Zoneless & Async-First, TestBed and ComponentFixture, Testing Fundamentals

### Community 156 - "generate-icon-catalog.mjs"
Cohesion: 0.40
Nodes (4): catalogFile, iconFileList, iconFileNames, iconsDirectory

### Community 163 - "StockBooleanField"
Cohesion: 0.40
Nodes (5): StockBooleanField, All, ExpiredOnly, IncludeHidden, LowStockOnly

## Knowledge Gaps
- **889 isolated node(s):** `$schema`, `version`, `packageManager`, `analytics`, `newProjectRoot` (+884 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1193 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **10 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `@angular/core` connect `@angular/core` to `school-class-overview.page.ts`, `review-modal.ts`, `problem-detail.feature.ts`, `class-analytics.page.ts`, `directives/index.ts`, `DynamicSizeVirtualScrollStrategy`, `item-import.store.ts`, `item-dropdown.ts`, `BaseTableWithFilter`, `LoaderDirective`, `school-class.ts`, `withProblemDetailsFeature`, `goods-receipts.feature.ts`, `items/list/tables/import/large/table.ts`, `ErrorAlert`, `StockItemDto`, `category-import.ts`, `home.routes.ts`, `item-import-review.store.ts`, `package.json`, `stock-category-cards-container.ts`, `stock.ts`, `@angular/router`, `errors/index.ts`, `StopClick`, `goods-receipt.ts`, `item-list-tab.ts`, `add-stock-batch-modal.ts`, `icon-catalog.ts`, `@ngrx/signals`, `auth.http.ts`, `category-list-tab.ts`, `school-class-detail.store.ts`, `category-detail.store.ts`, `PAGINATION_PAGE_SIZE`, `item-detail-modal.ts`, `stock-batch-collection.feature.ts`, `categories.page.ts`, `storage.http.ts`, `theme-switcher.ts`, `order-lists-tab.ts`, `GetAllSchoolClassesRequest`, `app.config.ts`, `stock/ui/list/filter/filter-form.ts`, `school-classes/ui/modals/form.ts`, `adjust/form.ts`, `review.store.ts`, `header-container.ts`, `category-dropdown.ts`, `categories/ui/modals/detail/form.ts`, `login.page.ts`, `categories/list/tables/import/large/table.ts`, `school-classes.page.ts`, `class-analytics.store.ts`, `category-import-review-line-row.ts`, `rxjs`, `simple.routes.ts`, `base-table-with-filter.ts`?**
  _High betweenness centrality (0.156) - this node is a cross-community bridge._
- **Why does `rxjs` connect `rxjs` to `school-class-overview.page.ts`, `directives/index.ts`, `class-analytics.page.ts`, `DynamicSizeVirtualScrollStrategy`, `item-import.store.ts`, `@angular/core`, `item-dropdown.ts`, `ErrorAlert`, `withProblemDetailsFeature`, `goods-receipts.feature.ts`, `category-import.ts`, `item-import-review.store.ts`, `package.json`, `stock.ts`, `@angular/router`, `errors/index.ts`, `add-stock-batch-modal.ts`, `@ngrx/signals`, `auth.http.ts`, `school-class-detail.store.ts`, `category-detail.store.ts`, `PAGINATION_PAGE_SIZE`, `item-detail-modal.ts`, `stock-batch-collection.feature.ts`, `GetAllSchoolClassesRequest`, `review.store.ts`, `category-dropdown.ts`, `login.page.ts`, `class-analytics.store.ts`, `base-table-with-filter.ts`?**
  _High betweenness centrality (0.018) - this node is a cross-community bridge._
- **Why does `@ngrx/signals` connect `@ngrx/signals` to `school-class-overview.page.ts`, `review-modal.ts`, `problem-detail.feature.ts`, `class-analytics.page.ts`, `item-import.store.ts`, `@angular/core`, `BaseTableWithFilter`, `ErrorAlert`, `withProblemDetailsFeature`, `goods-receipts.feature.ts`, `category-import.ts`, `item-import-review.store.ts`, `package.json`, `stock-category-cards-container.ts`, `stock.ts`, `errors/index.ts`, `school-class-detail.store.ts`, `category-detail.store.ts`, `PAGINATION_PAGE_SIZE`, `stock-batch-collection.feature.ts`, `GetAllSchoolClassesRequest`, `review.store.ts`, `category-dropdown.ts`, `school-classes.page.ts`, `class-analytics.store.ts`, `rxjs`?**
  _High betweenness centrality (0.016) - this node is a cross-community bridge._
- **What connects `$schema`, `version`, `packageManager` to the rest of the system?**
  _889 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `school-class-overview.page.ts` be split into smaller, more focused modules?**
  _Cohesion score 0.05647058823529412 - nodes in this community are weakly interconnected._
- **Should `review-modal.ts` be split into smaller, more focused modules?**
  _Cohesion score 0.05795918367346939 - nodes in this community are weakly interconnected._
- **Should `ske` be split into smaller, more focused modules?**
  _Cohesion score 0.04251700680272109 - nodes in this community are weakly interconnected._