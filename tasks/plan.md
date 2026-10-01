# Plan: Adăugare articol din stoc într-o comandă draft
Spec: `tasks/SPEC-add-stock-item-to-order.md` (propuneri acceptate: „Comandă nouă" în același modal; toast cu link spre comandă).

## Ordine și dependențe
Backend (T1→T2) → Client data layer (T3) → Modal (T4) → Wiring + acțiuni UI (T5) → Verificare responsive (T6) → Final (T7).

## Backend
- `AddItemToOrderListCommand` (`[Authorize]`, `ICacheInvalidation`, `Result<OrderListDto>`):
  - validator: ClassId/ItemId non-empty, Quantity > 0, exact unul dintre `OrderListId` / `NewOrderListName` (nume trim, lungime ca la CreateOrderList);
  - handler: validează articolul (există, activ); dacă `OrderListId` → încarcă cu `Lines`, verifică aceeași clasă și `IsEditable`, altfel erori existente `OrderListNotFound`/`OrderListNotEditable`; dacă nume nou → `OrderList.Create(classId, name, null)`;
    caută linie cu același `ItemId` → `Quantity +=`, altfel linie nouă (ProductName = Item.Name, Unit = Item.Unit); SaveChanges; răspuns prin `OrderListProjection.LoadAsync`.
- Endpoint `POST /api/OrderLists/add-item` + `AddItemToOrderListRequest`.
- Risc: cursă la adăugări simultane pe aceeași linie → verific dacă `OrderListLine` are concurrency token; dacă nu, acceptăm (ultima scriere) și notăm.

## Client
- `order-lists.http.ts`: `addItem(request)`; metodă în store-ul de stoc/comenzi cu `rxMethod` + `mapResponse`, toast cu link către comandă.
- Modal `shared/stock/ui/modals/add-to-order/`: încarcă drafturile clasei (`get-all`, filtre classId + status Draft), `nz-select` (comenzi + „Comandă nouă"), câmp nume (vizibil la „Comandă nouă" sau când nu există drafturi), cantitate (implicit 1, număr > 0). Returnează payload-ul la închidere; containerul apelează store-ul.
- Acțiune: icon în tabel + intrare în meniul ⋮ (carduri); `stock-list-container` deschide modalul.

## Redesign responsive
Coloana de acțiuni din tabel are acum: mută, [dropdown expirate], vizibilitate. Adaug butonul „Adaugă în comandă" doar dacă încape la 1024 px (`whitespace-nowrap` deja setat); altfel îl mut într-un dropdown general „⋮" al rândului. Decid după verificarea vizuală. Pe carduri: intrare nouă în meniu cu `me-2` pe iconiță.

## Verificări
Teste unit backend + Vitest client; `dotnet build`; Playwright la 1440/1024/390.
