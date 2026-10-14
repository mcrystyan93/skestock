# Plan: Numărul de locuri în Configuration și limita API

Status: Complete — plan aprobat și implementat la 2026-10-04.

## Obiectiv

Implementăm [specificația aprobată](../.scratch/room-configuration-ui/spec.md):
secțiune cu un card pentru Sala 4, Sala 2 și Sala 6, feature dedicat în
ConfigurationStore și validare 0–200 în frontend și backend.
[Planul backend finalizat anterior](room-configuration-backend-plan.md) este păstrat separat.

## Decizii tehnice

- Backend: adăugăm limita superioară în validatorul existent, folosind codurile
  standard de validare. Păstrăm required/min, autorizarea, cache-ul și handler-ul.
  Nu modificăm schema sau datele existente.
- Client: DTO/request cu trei numere și GET/PUT prin ConfigurationHttp.
  Folosim interceptorul existent și ruta `/api/ClassConfiguration/rooms`.
- Store: `withRooms()` compune state, loading și ProblemDetails independente.
  `rxMethod`/`mapResponse` gestionează GET și PUT → GET; numai răspunsurile
  confirmate înlocuiesc datele. Erorile se curăță înaintea fiecărei cereri.
- Prezentare: `RoomsCard` folosește signal inputs/outputs și Signal Forms,
  trei input-uri native `nz-input`, validare required/integer/min/max și
  butonul Salvează. Nu injectează store sau HTTP.
- Container: `RoomsSection` injectează store-ul, afișează erorile și coordonează
  submit-ul valid; pagina îl adaugă după Invitații și încarcă sălile la inițializare.
- Scaffolding prin Angular CLI; teste focalizate scrise manual. Refolosim
  componentele ng-zorro, loader-ul și stilurile existente, fără dependențe noi.
- Lucrăm secvențial în acest chat și păstrăm modificările locale preexistente.

## Dependențe și task-uri

```text
01 Limita API + teste
    ↓
02 Contract client + HTTP
    ├── 03 Feature rooms în store
    └── 04 Card prezentational
                ↓ (03 + 04)
        05 Container + pagină
                ↓
        06 Verificare finală
```

03 și 04 sunt independente după stabilirea contractului, dar nu necesită
agenți paraleli. Fiecare task include verificarea propriului comportament.

1. [x] [Limita 200 în backend](../.scratch/room-configuration-ui/issues/01-backend-seat-limit.md)
2. [x] [Contractul HTTP client](../.scratch/room-configuration-ui/issues/02-client-room-http.md)
3. [x] [Feature rooms în ConfigurationStore](../.scratch/room-configuration-ui/issues/03-rooms-store-feature.md)
4. [x] [Cardul pentru numărul de locuri](../.scratch/room-configuration-ui/issues/04-rooms-presentation-card.md)
5. [x] [Secțiunea și integrarea în pagină](../.scratch/room-configuration-ui/issues/05-rooms-section-page.md)
6. [x] [Verificarea fluxului complet](../.scratch/room-configuration-ui/issues/06-rooms-verification.md)

Task tracker: câte un issue Markdown în `.scratch/room-configuration-ui/issues/`,
conform `docs/agents/issue-tracker.md`; nu duplicăm lista în `tasks/todo.md`.

## Checkpoint-uri

- [x] După 01–02: build backend, teste pentru limita API și testele HTTP client.
- [x] După 03–04: testele feature/card și build Angular; fiecare sală acceptă
  0/200 și respinge câmp gol, -1, fracții și 201.
- [x] După 05–06: flux GET → editare → PUT → GET, stări de eroare/loading,
  accesibilitate și layout mobil/desktop verificate. Browserul folosește un API
  rooms interceptat; testele funcționale verifică endpoint-ul real.

## Verificare executabilă

Din rădăcina repository-ului:

```bash
dotnet build
dotnet test tests/Application.UnitTests --filter FullyQualifiedName~ClassConfiguration
./run-functional-tests.sh --filter FullyQualifiedName~ClassConfiguration
dotnet test tests/Application.UnitTests
dotnet run --project src/AppHost
graphify update .
```

Din `src/Client`:

```bash
npm run build
npm test -- --watch=false
npm run e2e -- --project=chromium --project=mobile e2e/specs/room-configuration.spec.ts
```

În task-uri folosim `--include` pentru testele Angular focalizate; comportamentul
este implementat în ciclul red/green. E2E folosește aplicația reală și interceptează
ruta rooms pentru a evita scrieri în baza locală; testele funcționale verifică API-ul
real. Orice blocaj de mediu este raportat explicit.

## Riscuri și măsuri

| Risc | Măsură |
|---|---|
| Câmp gol interpretat ca zero sau NaN | Teste care golesc input-ul și verifică lipsa cererii HTTP |
| Regula max anulează required/min | Reguli FluentValidation separate și teste pentru fiecare câmp |
| Valori istorice peste 200 | GET le păstrează, formularul indică eroarea și permite corectarea |
| Reîncărcarea suprascrie editări | Sincronizăm formularul numai cu datele confirmate; loading blochează editarea și submit repetat |
| PUT reușit, GET de confirmare eșuat | Afișăm eroarea și păstrăm ultima configurație confirmată, fără succes fals |
| Alte features pierd state/error | State și metode rooms distincte; testăm independența |
| Resurse AppHost indisponibile | Folosim setup-ul existent și raportăm verificările neexecutate |

## Întrebări deschise

Nu există întrebări de cerințe. Aprobarea planului și task-urilor precede implementarea.
