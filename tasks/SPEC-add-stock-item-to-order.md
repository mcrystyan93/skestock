# Spec: Adăugarea unui articol din lista de stoc într-o comandă (draft)

## Objective
Din lista de stoc a unei clase (rând de tabel / card), utilizatorul poate adăuga articolul într-o
comandă (`OrderList`) a clasei curente, aflată în starea **Draft**.

Reguli (confirmate):
1. Se pot alege doar comenzile **Draft ale clasei curente**.
2. Dacă nu există nicio comandă draft: utilizatorul introduce un **nume** și se creează o comandă nouă (Draft).
3. Dacă există una sau mai multe: utilizatorul alege în care adaugă (sau poate crea una nouă cu nume).
4. Cantitatea o introduce utilizatorul în dialog (implicit 1, > 0).
5. Dacă articolul e deja pe o linie a comenzii alese (același `ItemId`): **se adună cantitatea** la linia existentă.
6. Linia nouă: `ItemId`, `ProductName` = numele articolului (snapshot), `Unit` = unitatea articolului.

## Assumptions (de confirmat)
1. Operația e atomică pe server: un singur endpoint nou `POST /api/OrderLists/add-item`
   (nu compunem pe client GET + PUT cu înlocuire completă a liniilor – ar fi neatomic/racy).
2. Lista draft se încarcă cu endpoint-ul existent `POST /api/OrderLists/get-all` (filtre `classId` + `status=Draft`).
3. Articol dezactivat nu poate fi adăugat; articol inexistent => eroare de validare.
4. Comandă care nu mai e Draft (race) => `OrderListNotEditable` existent; comandă din altă clasă => eroare.
5. Nume nou: obligatoriu, trim, lungime max ca la `CreateOrderList` (se reutilizează regula).
6. Nicio migrare EF, nicio schimbare de domeniu.

## Tech Stack
.NET 10, Mediator, FluentValidation, EF Core; Angular 22 + ng-zorro + SignalStore.

## Commands
- `dotnet build`; `dotnet test tests/Application.UnitTests --filter AddItemToOrderList`
- `cd src/Client && npm test && npm run build`

## Project Structure
- `src/Application/Features/OrderLists/Commands/AddItemToOrderList/` (Command, Handler, Validator)
- `src/Application/Features/OrderLists/Models/OrderListRequests.cs` – `AddItemToOrderListRequest`
- `src/Web/Endpoints/OrderLists.cs` – `AddItemToOrderList`
- `tests/Application.UnitTests/Features/OrderLists/Commands/AddItemToOrderList/`
- Client: `shared/order-lists/services/order-lists.http.ts` (+ store method), modal nou
  `shared/stock/ui/modals/add-to-order/`, acțiune în `stock-category-item-row` (tabel) și `stock-category-items-small` (meniu ⋮), wiring în `stock-list-container`.

## Contract
Request: `{ classId, itemId, quantity, orderListId?: Guid, newOrderListName?: string }`
— exact unul dintre `orderListId` / `newOrderListName`.
Response: `OrderListDto` (comanda rezultată) – clientul afișează mesaj „Adăugat în «Nume»" cu link spre comandă.
Cache: invalidare `OrderListListTag` + `OrderListTag(id)`. Domain event: `OrderListCreatedEvent` la creare (existent).

## Code Style
Oglindește `CreateOrderList`/`UpdateOrderList`: `[Authorize]`, `ICacheInvalidation`, `Result<OrderListDto>`,
erori tipate din `OrderListErrors`, `OrderListProjection.LoadAsync` pentru răspuns.

## UI / Responsive
- Tabel (≥lg): buton icon (`icons:cart-plus` sau echivalent existent) în coloana de acțiuni; dacă nu încape,
  se mută în dropdown-ul de acțiuni ca să nu depășească lățimea.
- Carduri (<lg): intrare „Adaugă în comandă" în meniul ⋮.
- Modal: radio/Select „Comandă existentă" (draft-urile clasei) + opțiunea „Comandă nouă"; câmp nume (apare doar la
  comandă nouă, sau implicit când nu există drafturi); câmp cantitate (implicit 1); acțiuni cu spațiere corectă;
  lățime `modal-w-90 modal-w-md-50 modal-w-xl-30`. Accesibil (label, aria, focus).
- Verificat vizual la 1440 / 1024 / 390 px.

## Testing Strategy
Unit (NUnit/Shouldly/Moq, InMemory): comandă existentă + articol nou => linie nouă; articol existent => cantitate adunată;
nume nou => comandă Draft creată cu o linie; comandă ne-Draft / altă clasă / inexistentă => eroare; ambele sau niciuna
dintre `orderListId`/`newOrderListName` => validare; cantitate ≤ 0; articol dezactivat. Client: Vitest pentru store/modal.

## Boundaries
- Always: validare, cache invalidat, teste, ținte tactile ≥40px.
- Ask first: schimbări de schemă, tip nou de status, dependențe noi.
- Never: modificarea comenzilor ne-Draft; înlocuirea totală a liniilor existente; editarea migrărilor.

## Success Criteria
1. Din stoc pot adăuga un articol într-o comandă Draft existentă; linia apare în detaliul comenzii.
2. Fără drafturi: modalul cere un nume și creează o comandă Draft cu linia.
3. Adăugat de două ori același articol => o singură linie cu cantitatea însumată.
4. Comenzile Submitted/Cancelled și cele din alte clase nu apar și nu pot fi țintite.
5. Build fără warning, testele trec, UI fără overflow pe 3 lățimi.

## Open Questions
- Dacă există drafturi, afișăm „Comandă nouă" ca opțiune alăturată în același modal? (propun: da)
- Unde se vede confirmarea: doar toast, sau toast cu link „Deschide comanda"? (propun: toast cu link)
