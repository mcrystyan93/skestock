# Spec: Close a school class and roll its stock over

Status: Draft v2 — open questions answered 2026-10-16, awaiting approval.

## Objective

An Administrator can close a school class and, in the same atomic operation, transfer all of its remaining stock
to a chosen open school class. Only stock with a positive remaining quantity moves. Scope: backend only (Application
command, validator, errors, Web endpoint, tests). No UI.

One capability, no capability map needed.

## Current model (what exists today)

- Stock is class-scoped via `StockBatch.ReceivedClassId`. The remaining quantity lives in `StockBatch.Quantity`,
  per (item, location, batch). There is no stock table per class.
- `StockTransactionType.Rollover` already exists ("system-generated, closing -> opening copy") and is unused.
- `ClassBalance` (`OpeningQty`, `ClosingQty` "null until the class is closed") exists. Only the demo seeder writes it.
- `ClassStatus` = `Upcoming | Active | Paused | Closed`. Today `PUT /api/SchoolClasses/{id}` can set `Closed` freely,
  with no stock handling.
- `MoveStockCommandHandler` is the reference pattern: it walks the batches, splits them into new batches, writes
  paired transactions, runs one `SaveChangesAsync` and maps `DbUpdateConcurrencyException` to `StockErrors.ConcurrencyConflict`.

## Contract

`POST /api/SchoolClasses/{id}/close`, role `Administrator`.

```json
{ "targetClassId": "guid" }
```

- `204 No Content` on success.
- `400` validation errors: `targetClassId` empty, `targetClassId == id`.
- `404` `school_classes.not_found` when the source or the target class does not exist.
- `409` `school_classes.already_closed` when the source is already `Closed`.
- `409` `school_classes.target_not_open` when the target class is `Closed`.
- `409` `stock.concurrency_conflict` (existing error) when a source batch changed during the close.

## Behavior

Command `CloseSchoolClassCommand { Id, TargetClassId }`, `[Authorize(Roles = Roles.Administrator)]`, `ICacheInvalidation`.

In one `SaveChangesAsync` (one DB transaction):

1. Load the source class (tracked). Fail if it is missing or already `Closed`. Load the target. Fail if it is missing
   or `Closed`. Every other status (`Upcoming`, `Active`, `Paused`) counts as open.
2. Load every source batch with `ReceivedClassId == Id && Quantity > 0`. Batches with zero stock are ignored.
3. For each of these batches:
   - create a target batch with the same `ItemId`, `LocationId`, `ExpiryDate`, `ReceivedDate`, `UnitPrice`,
     `GoodsReceiptId`, `Quantity`, and `ReceivedClassId = TargetClassId`;
   - set the source batch `Quantity = 0`;
   - write two `Rollover` transactions: `-qty` on the source batch (`ClassId = Id`) and `+qty` on the target batch
     (`ClassId = TargetClassId`), `UserId` = the current user.
4. Fill in `ClassBalance` (see below).
5. Set `source.Status = Closed`.
6. Invalidate the stock caches (coarse tag, the class tag of both classes, `StockBatchListTag`) and the school class cache tags.

The stock stays in the same location. Only the class that owns it changes. The source batches are kept at zero, not
deleted, so the source class keeps its history and statistics.

A class with no positive stock can still be closed: the transfer has nothing to move, and the status still changes.

### ClassBalance

`ClassBalance` has one row per (class, item, location). The close looks up existing rows and creates the missing ones
(`OpeningQty = 0`):

- **Source:** for every (item, location) that has at least one source batch (including zero-qty batches), set
  `ClosingQty` = the sum of the positive remaining quantity for that pair (0 if nothing remained).
- **Target:** for every (item, location) that received stock, add the transferred quantity to `OpeningQty`. The
  target's `ClosingQty` stays `null`. Adding, not overwriting, makes it correct when several classes roll into the
  same target.

There is no unique index on (ClassId, ItemId, LocationId). The handler reads the existing rows and updates them. Adding
the index needs a migration, so it is out of scope (see Boundaries).

### Statistics must ignore Rollover

`MaterializeDailyConsumptionCommandHandler` and `GetConsumptionBackfillStartHandler` count every negative
transaction whose type is not `Transfer` as consumption. Both filters must also exclude `Rollover`; otherwise closing
a class would record its whole remaining stock as consumed on the closing day. Purchase statistics only read `Order`
transactions, so they need no change.

### UpdateSchoolClass no longer changes Closed

`PUT /api/SchoolClasses/{id}` can no longer move a class into or out of `Closed`: if `request.Status == Closed`
differs from `current.Status == Closed`, the handler returns `409` `school_classes.status_change_not_allowed`.
Editing the name or dates of an already-closed class while keeping `Status = Closed` is still allowed. The check runs
in the handler because it needs the persisted status.

## Decisions I'm proposing (correct me)

- **New batches + zeroed source batches** instead of changing `ReceivedClassId` in place. This keeps the source's
  receipts and statistics, and mirrors `MoveStock`.
- **The source batch rowversion** catches a concurrent stock change. A second concurrent close fails on the
  same tokens, so stock cannot be transferred twice.
- **Expired batches move like any other.** Expiry is copied, so the target can still write them off.
- **No new domain event, no realtime notification, no migration.**

## Resolved questions (2026-10-16)

1. Only batches with quantity > 0 are transferred. Zero-qty batches are skipped and never block the close.
2. An open target is any status except `Closed`.
3. `UpdateSchoolClass` can no longer close or reopen a class (see above).
4. `ClassBalance` is filled in on close (see above).
5. Blocking stock writes on closed classes is out of scope.

## Open questions

None.

## Tech stack

.NET 10, ASP.NET Core minimal endpoints, Mediator (source-generated), FluentValidation, EF Core / SQL Server,
HybridCache. No new dependencies.

## Commands

```bash
dotnet build
dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~CloseSchoolClass"
./run-functional-tests.sh --filter "FullyQualifiedName~CloseSchoolClass"
```

## Project structure

```
src/Application/Features/SchoolClasses/Commands/CloseSchoolClass/
    CloseSchoolClassCommand.cs, CloseSchoolClassCommandValidator.cs, CloseSchoolClassCommandHandler.cs
src/Application/Common/Errors/SchoolClassErrors.cs          + AlreadyClosed, TargetNotOpen, StatusChangeNotAllowed
src/Web/Endpoints/SchoolClasses.cs                          + MapPost(CloseSchoolClass, "{id}/close")
src/Application/Features/SchoolClasses/Commands/UpdateSchoolClass/UpdateSchoolClassCommandHandler.cs   closed-status guard
src/Application/Features/Statistics/Commands/MaterializeDailyConsumption/MaterializeDailyConsumptionCommandHandler.cs  exclude Rollover
src/Application/Features/Statistics/Queries/GetConsumptionBackfillStart/GetConsumptionBackfillStartHandler.cs        exclude Rollover
tests/Application.UnitTests/Features/SchoolClasses/Commands/CloseSchoolClass/
tests/Application.FunctionalTests/Features/SchoolClasses/Commands/CloseSchoolClass/
```

## Code style

Follow `MoveStockCommandHandler`: guard on `user.Id`, return `Result.Fail(new XErrors.Y(...))` for domain failures,
one `SaveChangesAsync` wrapped in a `DbUpdateConcurrencyException` catch. Errors are `Error` subclasses with
`StatusCode`/`Title`/`Code` metadata, as in `SchoolClassErrors`. Generate the command with
`dotnet new ca-usecase -n CloseSchoolClass -fn SchoolClasses -ut command -rt Result` and adapt it.

## Testing strategy

- Unit (handler, in-memory test DbContext as in the existing SchoolClasses tests):
  - moves every positive batch to the target, with all its fields copied; source batches end at 0;
  - skips zero-qty batches (no batch is created for them, no transaction is written);
  - writes paired `Rollover` transactions with the correct class, signed qty and user;
  - closes a class that has no stock;
  - `ClassBalance`: the source `ClosingQty` per pair (including 0 for drained pairs); the target `OpeningQty` is
    created, or added to when a row already exists;
  - fails for an already-closed source, a closed target, a missing source or target, and source == target;
  - `UpdateSchoolClass`: rejects Active→Closed and Closed→Active, and allows editing a closed class that stays Closed;
  - daily consumption ignores `Rollover` transactions (extend the existing handler tests).
- Functional (SQL Server via TestAppHost):
  - end-to-end `POST .../close` → 204, then the target's class stock query shows the quantities and the source shows none;
  - non-admin → 403;
  - atomicity: a concurrency conflict leaves the source open and the stock unchanged (the existing
    `StockBatchConcurrencyTests` pattern).

## Boundaries

- Always: one `SaveChangesAsync`, invalidate the cache tags, run the unit and functional tests before declaring done,
  and run `graphify update .` after code changes.
- Ask first: any schema change or migration (including a unique index on `ClassBalance`), blocking stock writes on closed classes.
- Never: delete source batches or transactions, hand-edit migration designer/snapshot files, touch `src/Client`.

## Success criteria

- After the close, for every (item, location), the target's remaining quantity = its quantity before + the source's
  positive remaining quantity before. The source has 0 everywhere and its status is `Closed`.
- Source `ClassBalance.ClosingQty` equals what was transferred per pair. Target `OpeningQty` grew by the same amount.
- Closing a class does not change its daily consumption statistics.
- `PUT` can no longer change a class into or out of `Closed`.
- Each moved quantity has exactly one `-qty` and one `+qty` `Rollover` transaction.
- Any failure leaves no partial state: the status and every batch stay unchanged.
- Every error case returns the status code and error code listed in the Contract.
- `dotnet build` is clean (warnings are errors) and every new test passes.
