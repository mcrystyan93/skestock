# Spec: Prelungirea expirării loturilor expirate

## Objective
Pentru un articol perisabil care apare ca expirat într-o clasă/locație, utilizatorul poate
prelungi expirarea loturilor (`StockBatch`) expirate. `Item.ShelfLifeDays` NU se modifică;
se schimbă doar `StockBatch.ExpiryDate`.

Utilizatorul introduce un număr de zile `ExtensionDays`; noua dată = **azi (UTC) + ExtensionDays**.
Se aplică tuturor loturilor cu `Quantity > 0`, `ExpiryDate <= azi`, pentru (ClassId, ItemId, LocationId).

## Assumptions
1. Articolul trebuie să fie `IsPerishable`.
2. `ExtensionDays` întreg, 1..365 (limită superioară ajustabilă).
3. Fiecare lot modificat generează un `StockTransaction` de audit (tip `Adjustment`, `QuantityChange = 0`) – fără tip nou de enum / migrare.
   (Alternativa: nicio tranzacție; de confirmat.)
4. Nu există migrare EF (nicio coloană nouă).
5. Fără loturi expirate => eroare tipată (ca `RemoveExpiredStock`).
6. Emitere eveniment realtime ca la `RemoveExpiredStock` (stock-adjusted), pentru refresh UI.

## Tech Stack
.NET 10, Mediator, FluentValidation, EF Core; Angular 22 (src/Client).

## Commands
- Build: `dotnet build`
- Test: `dotnet test tests/Application.UnitTests`
- Client: `cd src/Client && npm test && npm run build`

## Project Structure
- `src/Application/Features/Stock/Commands/ExtendExpiredStockExpiry/` – Command, Handler, Validator
- `src/Application/Features/Stock/Models/StockRequests.cs` – `ExtendExpiredStockExpiryRequest`
- `src/Application/Common/Errors` / `StockErrors` – reutilizează `NoExpiredQuantity`
- `src/Web/Endpoints/Stock.cs` – `POST /api/Stock/extend-expiry`
- `tests/Application.UnitTests/Features/Stock/Commands/`
- `src/Client` – acțiune „Prelungește expirarea" (dialog cu nr. zile) lângă „Elimină expirate"

## Code Style
Oglindește `RemoveExpiredStock*`: `[Authorize]`, `ICacheInvalidation` cu
`CacheConstants.BuildTag(ClassId, LocationId)` + `BuildClassTag(ClassId)`, `Result`,
file-scoped namespaces, `Guard.Against`, endpoint ca metodă statică numită.

## Testing Strategy
Unit (NUnit/Shouldly/Moq): validator (zile ≤0, >max, Guid.Empty), handler (doar loturi expirate
cu Quantity>0 modificate; neexpirate/goale neatinse; `Item.ShelfLifeDays` neschimbat; articol
neperisabil => eroare; zero loturi => `NoExpiredQuantity`; tranzacții de audit). Client: test Vitest pentru store/dialog.

## Boundaries
- Always: invalidare cache, validare, test per comportament.
- Ask first: tip nou de `StockTransactionType`, migrare, dependențe noi.
- Never: modificarea `Item.ShelfLifeDays`; editarea migrărilor generate; extindere cu dată în trecut.

## Success Criteria
1. După apel, loturile expirate au `ExpiryDate = azi + N`, iar `GetClassLocationStock` nu mai raportează `IsExpired` pentru ele.
2. `Item.ShelfLifeDays` identic.
3. Loturi neexpirate/Quantity=0 nemodificate.
4. Concurența: `StockBatch.Version` (rowversion) protejează; conflict => eroare existentă.
5. `dotnet build` (warnings=errors) și testele trec.

## Open Questions
- Audit via `StockTransaction` (Adjustment, qty 0) sau fără audit?
- Limita maximă de zile (365)?
- Include și UI acum sau doar backend în prima etapă?
