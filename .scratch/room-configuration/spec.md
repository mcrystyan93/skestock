# Specificație: Configurația locurilor pentru săli

Status: Approved — aprobat de utilizator la 2026-10-02.

## Obiectiv

Administratorul poate salva numărul de locuri pentru trei săli fixe în configurația globală `SharedClassConfiguration`:
Sala 4 (principală), Sala 2 și Sala 6 (secundare). Utilizatorii autentificați pot citi configurația. Implementarea
include doar backend, cu teste unitare și funcționale.

Aceasta este o singură capabilitate: configurarea capacității sălilor.

## Stack tehnic

.NET 10 (SDK 10.0.110), ASP.NET Core minimal endpoints, Mediator cu generare de cod, FluentValidation, EF Core/SQL
Server și HybridCache. Se păstrează versiunile centralizate existente. Testele folosesc NUnit, Shouldly și
infrastructura de test existentă; nu sunt necesare dependențe noi.

## Contract și comportament propus

- `SharedClassConfiguration` primește trei proprietăți `int`: `Room4SeatCount`,
  `Room2SeatCount`, `Room6SeatCount`. Identitatea și rolul sălilor sunt fixe.
- `RoomConfigurationDto` expune aceleași trei valori.
- `GetRoomConfigurationQuery` citește singleton-ul fără tracking, cu caching. Absența configurației produce trei
  zerouri, fără scriere în baza de date.
- `SaveRoomConfigurationCommand` salvează atomic toate cele trei valori; creează singleton-ul dacă lipsește sau
  actualizează înregistrarea existentă.
- `GET /api/ClassConfiguration/rooms`: autentificare obligatorie, `200 OK` cu DTO.
- `PUT /api/ClassConfiguration/rooms`: rol `Administrator`, `200 OK` cu DTO salvat.
- JSON pentru ambele operații:

```json
{
  "room4SeatCount": 120,
  "room2SeatCount": 30,
  "room6SeatCount": 40
}
```

- PUT cere explicit toate cele trei valori. Câmpurile lipsă/null și valorile negative produc `400 Bad Request` cu
  ProblemDetails, fără modificări persistate. Zero este valid; limita maximă este 200, conform completării aprobate la 2026-10-04
  în [specificația UI și limita API](../room-configuration-ui/spec.md).
- Cererile anonime sunt respinse cu `401`; PUT al unui utilizator autentificat fără rol de administrator este respins cu
  `403`. Autorizarea există și pe CQRS.
- Salvarea invalidează tag-ul global al configurației și tag-ul pentru săli. Următorul GET returnează valorile
  actualizate, inclusiv după un GET inițial cu zero.
- Invitațiile și departamentele sunt păstrate la salvarea sălilor. Salvările existente de invitații/departamente
  păstrează valorile sălilor.
- Migrația EF adaugă trei coloane obligatorii cu zero pentru datele existente. Fișierele generate sunt regenerate prin
  tooling, fără editare manuală.
- Configurația sălilor rămâne globală. Nu se copiază în `SchoolClass` și nu modifică inițializarea configurației
  claselor.

## Comenzi

Executate din rădăcina repository-ului, dacă nu se indică altfel:

```bash
dotnet build
dotnet test tests/Application.UnitTests --filter FullyQualifiedName~ClassConfiguration
./run-functional-tests.sh --filter FullyQualifiedName~ClassConfiguration
dotnet test tests/Application.UnitTests
dotnet ef migrations add AddRoomSeatCounts --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext --output-dir Data/Migrations
dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext
graphify update .
```

Functional tests necesită container runtime și folosesc resetarea existentă Respawn/TestBase. Pentru CQRS se folosește
template-ul `ca-usecase` din
`src/Application`, conform instrucțiunilor repository-ului.

## Structura proiectului

- `src/Domain/Entities/SchoolClasses/SharedClassConfiguration.cs`: proprietățile persistate.
- `src/Application/Features/ClassConfiguration/Models/`: DTO pentru săli.
- `src/Application/Features/ClassConfiguration/Commands/SaveRoomConfiguration/`: comandă, validator, handler.
- `src/Application/Features/ClassConfiguration/Queries/GetRoomConfiguration/`: query și handler.
- `src/Application/Features/ClassConfiguration/CacheConstants.cs`: tag pentru săli.
- `src/Infrastructure/Data/Configurations/`: mapping EF.
- `src/Infrastructure/Data/Migrations/`: migrația generată și snapshot.
- `src/Web/Endpoints/ClassConfiguration.cs`: GET/PUT pentru săli.
- `tests/Application.UnitTests/Features/ClassConfiguration/`: teste validator/handler.
- `tests/Application.FunctionalTests/Features/ClassConfiguration/`: teste persistență, caching și HTTP.
- `.scratch/room-configuration/spec.md`: această specificație; issues separate în `issues/` la etapa Tasks.

## Stil de cod

Se păstrează namespace-urile file-scoped, nullable, global usings și warnings-as-errors. Handlers utilizează exclusiv
`IApplicationDbContext`; endpoint-urile deleagă prin
`ISender` și folosesc mapper-ele existente pentru `Result`.

Exemplu de stil și naming în model:

```csharp
namespace skestock.Application.Features.ClassConfiguration.Models;

public sealed record RoomConfigurationDto(
    int Room4SeatCount,
    int Room2SeatCount,
    int Room6SeatCount);
```

## Strategia de testare

Unit tests verifică fiecare câmp (negativ, zero, pozitiv, valoarea maximă, lipsă/null), query fără configurație și cu
configurație, create/update pentru handler și păstrarea invitațiilor/departamentelor.

Functional tests folosesc SQL Server și pipeline-ul Mediator existent pentru persistență, singleton și invalidarea
cache-ului. Testele HTTP verifică GET/PUT, valorile inițiale, autentificarea, rolurile și ProblemDetails pentru payload
invalid. Se verifică păstrarea locurilor după salvarea invitațiilor/departamentelor și păstrarea comportamentului
existent de inițializare a claselor.

Nu se introduce un prag arbitrar de coverage; se acoperă toate regulile de mai sus.

## Limite

- Always: validare, autorizare pe endpoint și CQRS, respectarea dependențelor între layere, teste înainte de declararea
  implementării ca finalizată, graphify update.
- Ask first: schimbări de cerințe sau contract față de specificația aprobată, dependențe noi, schimbări CI și extinderea
  către frontend sau configurație per clasă. Migrația celor trei coloane face parte din această propunere pentru
  aprobare.
- Never: editare manuală designer/snapshot EF, resetarea modificărilor utilizatorului, secrete în repository, eliminarea
  testelor care eșuează pentru a masca regresii.

## Criterii de succes

1. Cele trei numere de locuri sunt persistate în singleton-ul existent.
2. GET returnează valorile salvate sau trei zerouri fără a crea singleton-ul.
3. PUT creează sau actualizează toate valorile într-o singură salvare.
4. Payload incomplet/null/negativ este respins fără scrieri parțiale.
5. Autorizarea și răspunsurile HTTP respectă contractul de mai sus.
6. Cache-ul nu returnează valori vechi după salvare.
7. Invitațiile, departamentele și inițializarea claselor își păstrează comportamentul.
8. Build, testele unitare și funcționale relevante trec; migrația corespunde modelului.

## Decizii confirmate prin aprobarea specificației

- Zero este permis și reprezintă și valoarea inițială pentru fiecare sală.
- Toate trei câmpurile sunt obligatorii la PUT (actualizare completă).
- Configurația este globală, fără copiere în fiecare clasă.
