# Spec: Liste recurente de aprovizionare (Supply Lists) — backend

## Obiectiv

Utilizatorul poate defini liste globale, reutilizabile, de articole (`Item`) care
se folosesc cu o anumita frecventa (saptamanal, o data la X saptamani, la
inceputul anului scolar etc.). Fiecare lista are un nume, o frecventa si linii;
fiecare linie contine un articol, cantitate, unitate si observatii.

Doar backend-ul (Domain, Application, Infrastructure, Web, teste). Clientul
Angular nu intra in scope.

## Presupuneri (confirmate)

1. Numele entitatilor: `SupplyList` si `SupplyListLine`; feature `SupplyLists`;
   grup de endpoint-uri `/api/SupplyLists`.
2. Listele sunt globale (nu sunt legate de `SchoolClass`).
3. Enum `SupplyListFrequency`: `Weekly`, `EveryXWeeks`, `Monthly`,
   `StartOfSchoolYear`, `EndOfSchoolYear`, `MiddleOfSemester`,
   `StartOfMonth`, `EndOfMonth`, `Once`. Persistat ca string in DB.
4. `IntervalWeeks` (int?) este obligatoriu si intre 2 si 52 doar pentru
   `EveryXWeeks`; pentru orice alta frecventa trebuie sa fie `null`.
   Frecventa este doar descriptiva — nu se genereaza nimic automat/programat.
5. `Name` obligatoriu, max 200 caractere, unic (case-insensitive) intre toate
   listele (active si inactive). `Note` optional, max 1000.
6. Linie: `ItemId` obligatoriu (fara text liber, spre deosebire de OrderList),
   `Quantity` `decimal(18,3)` > 0, implicit `1` daca lipseste din request;
   `Unit` max 50, daca lipseste/e gol se populeaza cu `Item.Unit`;
   `Notes` optional, max 500.
7. Un articol apare cel mult o data intr-o lista.
8. La creare/modificare, articolele noi adaugate trebuie sa existe si sa fie
   active; liniile deja existente cu articole dezactivate ulterior pot ramane.
9. Modificarea inlocuieste complet liniile (PUT cu lista completa), ca la
   OrderList. Nu exista stergere fizica a listei.
10. Lista dezactivata (`IsActive = false`) nu poate fi modificata (eroare de
    conflict 409); se poate reactiva. Disable/Enable sunt idempotente (cererile
    repetate reusesc), ca la `DisableItem`/`EnableItem`.
11. Toate endpoint-urile cer autentificare (`[Authorize]` simplu), fara roluri.
12. `Sku`, `Name` si `Category` ale articolului sunt citite live in DTO (nu se
    face snapshot).

## Criterii de acceptare

- `POST /api/SupplyLists` creeaza o lista activa cu linii optionale si returneaza
  `201` cu `SupplyListDto`. Linie fara cantitate → `Quantity = 1`; linie fara
  unitate → `Unit = Item.Unit`.
- `PUT /api/SupplyLists/{id}` actualizeaza nume, nota, frecventa, intervalul si
  inlocuieste liniile; `404` pentru lista inexistenta, `409` pentru lista
  inactiva.
- `POST /api/SupplyLists/{id}/disable` si `/enable` comuta `IsActive`; `404` daca
  lista nu exista; cererile repetate reusesc.
- `GET /api/SupplyLists/{id}` returneaza lista cu linii (articol id, nume, sku,
  categorie, cantitate, unitate, observatii); `404` daca nu exista.
- `POST /api/SupplyLists/get-all` returneaza `PaginatedResponse<SupplyListListItemDto>`
  cu keyset pagination (`pageSize + 1`, `NextCursor`, `HasNextPage`), filtre
  allowlist (`name`, `frequency`, `isActive`), sortare allowlist
  (`name`, `frequency`, `createdDate`, `lastModifiedDate`) si `SearchTerm` pe nume.
  Item-ul de lista contine id, nume, frecventa, interval, isActive,
  numarul de linii, created/lastModified.
- Validari (400 ValidationProblem): nume gol/prea lung, frecventa invalida,
  interval incoerent cu frecventa, cantitate <= 0, unitate/observatii prea
  lungi, `ItemId` duplicat in request.
- Articol inexistent → 400 validare (`InvalidReference`), ca la OrderList.
- Erori de business (Result + Error tipizat): nume duplicat (400, validare `DuplicateName`), articol
  inactiv adaugat nou in lista (400), lista inactiva la update (409).
- Query-urile sunt cache-uite (`ICacheableQuery`) cu tag-urile
  `supply_lists` / `supply_list:{id}`; comenzile invalideaza ambele tag-uri.
- Migratie EF noua `AddSupplyLists`, fara modificari manuale in designer/snapshot.

## Stack si comenzi

- .NET 10, Mediator (source-gen), FluentValidation, EF Core SQL Server,
  HybridCache, NUnit/Shouldly/Moq.
- Build: `dotnet build`
- Unit tests: `dotnet test tests/Application.UnitTests --filter FullyQualifiedName~SupplyLists`
- Functional: `./run-functional-tests.sh --filter FullyQualifiedName~SupplyLists`
- Migratie:
  `dotnet ef migrations add AddSupplyLists --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext --output-dir Data/Migrations`
- Verificare: `dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext`

## Structura proiectului

- `src/Domain/Entities/SupplyList.cs`, `SupplyListLine.cs`;
  `src/Domain/Enums/SupplyListFrequency.cs`.
- `src/Application/Common/Interfaces/IApplicationDbContext.cs` — `DbSet`-uri noi.
- `src/Application/Features/SupplyLists/`:
  `CacheConstants.cs`, `SupplyListCursor.cs`, `SupplyListFilterConfiguration.cs`,
  `SupplyListSortConfiguration.cs`, `Models/` (DTO-uri, request-uri, line input),
  `Commands/{CreateSupplyList,UpdateSupplyList,DisableSupplyList,EnableSupplyList}/`,
  `Queries/{GetAllSupplyLists,GetSupplyListById}/`.
- `src/Infrastructure/Data/ApplicationDbContext.cs`,
  `Data/Configurations/SupplyListConfiguration.cs`,
  `SupplyListLineConfiguration.cs`, `Data/Migrations/*AddSupplyLists*`.
- `src/Web/Endpoints/SupplyLists.cs`.
- `tests/Application.UnitTests/Features/SupplyLists/...` (handlere + validatori).
- `tests/Application.FunctionalTests/Features/SupplyLists/...` (HTTP lifecycle +
  get-all).

## Stil

Se urmeaza slice-ul `OrderLists` (cel mai apropiat) si `Items` pentru
Disable/Enable. Exemplu:

```csharp
[Authorize]
public class UpdateSupplyListCommand : IRequest<Result<SupplyListDto>>, ICacheInvalidation
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public SupplyListFrequency Frequency { get; init; }
    public int? IntervalWeeks { get; init; }
    public string? Note { get; init; }
    public List<SupplyListLineInput> Lines { get; init; } = [];

    public IReadOnlyCollection<string> Tags =>
        [CacheConstants.SupplyListListTag, CacheConstants.SupplyListTag(Id)];
}
```

Endpoint-uri cu metode statice numite, `Results<...>`, `CancellationToken`,
`[EndpointSummary]`/`[EndpointDescription]`, maparea erorilor cu helper-ele
existente (`ToOk`, `ToProblemHttpResult`).

## Strategie de testare

- Unit (NUnit/Shouldly, DbContext de test ca in `OrderListTestDbContext`):
  validatori (frecventa/interval, cantitate, duplicate), Create (defaulturi
  cantitate/unitate, articol inactiv/inexistent, nume duplicat), Update
  (inlocuire linii, lista inactiva), Disable/Enable, GetById (404).
- Functional (Aspire + Respawn): lifecycle create → get → update → disable →
  update esuat → enable; get-all cu paginare si filtru `isActive`.

## Limite

- Mereu: `Guard.Against`, UTC `DateTimeOffset`, allowlist pentru filtre/sort,
  build fara warning-uri, teste verzi.
- Intreaba intai: schimbari la entitatea `Item`/`OrderList`, pachete noi,
  roluri/politici de autorizare.
- Niciodata: editare manuala a migratiilor generate, cod client, stergere fizica
  a listelor.

## Criterii de succes

- `dotnet build` fara erori/warning-uri.
- Unit tests `SupplyLists` verzi; teste functionale `SupplyLists` verzi.
- `has-pending-model-changes` raporteaza fara modificari dupa migratie.
- Endpoint-urile apar in OpenAPI/Scalar cu operation id-urile metodelor.

## Decizii

- `Once` nu are data asociata.
- Numele listei este unic (case-insensitive).
