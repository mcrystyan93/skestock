# Spec: Impărțirea invitațiilor

## Objective
Pe pagina Configurare, în secțiunea Invitații, lângă „Număr de invitații", adăugăm o secțiune „Impărțirea invitațiilor": cinci câmpuri numerice care definesc câte invitații sunt alocate fiecărei categorii:
Cursanți, Instructori, Rugăciune deschidere, Experiențe, Schimbări.
Utilizator: administratorul (doar el poate salva; ceilalți văd valorile).
Succes: valorile se salvează, se reîncarcă identic și sunt validate (întreg, 0..max).

## Assumptions (confirmate)
1. Setările sunt globale (singleton `SharedClassConfiguration`), ca `InvitationCount` și locurile din camere, și se copiază pe clasă la creare/inițializare (vezi 6).
2. Cele 5 valori sunt coloane noi `int` pe `SharedClassConfigurations` (default 0) și `int?` pe `SchoolClasses` (null = neinițializat, ca `InvitationCount`), în aceeași migrație, nu o tabelă separată. Nume propuse: `StudentInvitationCount`, `InstructorInvitationCount`, `OpeningPrayerInvitationCount`, `ExperienceInvitationCount`, `ChangeInvitationCount`.
3. `InvitationCount` (totalul) rămâne neschimbat. Nu impunem nicio relație între sumă și total: cardul afișează doar informativ „Total alocat: X din Y” (X = suma celor 5 din formular, Y = „Număr de invitații” salvat), fără blocare și fără logică în backend.
4. Un card separat, cu propriul buton „Salvează" și propriul endpoint, după modelul `rooms` (grid `md:grid-cols-3`, signal forms, `nz-input-number`).
5. Maxim per câmp: 200 (ca la camere, `MAX_ROOM_SEAT_COUNT`). Doar admin poate scrie, orice utilizator autentificat citește.
6. Cele 5 valori se copiază pe clasă exact ca `InvitationCount`: în `CreateSchoolClassCommandHandler` (la creare) și în `InitializeSchoolClassConfigurationCommandHandler` (în același `ExecuteUpdate` care face claim-ul pe `InvitationCount == null`). `SchoolClassDto` primește cele 5 câmpuri, mapate în `CreateSchoolClassCommandHandler` și `GetSchoolClassByIdHandler`. Clasele existente rămân cu null (nu se completează retroactiv). Nu afișăm valorile pe clasă în UI acum (YAGNI).

## Commands
- `dotnet build`
- `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~ClassConfiguration|FullyQualifiedName~SchoolClass"`
- `dotnet test tests/Infrastructure.IntegrationTests --filter "FullyQualifiedName~TemporalSchema"`
- `./run-functional-tests.sh --filter "FullyQualifiedName~ClassConfiguration"` (cu AppHost pornit)
- `dotnet ef migrations add AddInvitationSplit --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext --output-dir Data/Migrations`
- `cd src/Client && npm run build && npm test -- --watch=false`
- `./run-e2e.sh -- --project=chromium e2e/specs/invitation-split.spec.ts`

## Project Structure
- Domain: `SharedClassConfiguration.cs` (+5 `int`), `SchoolClass.cs` (+5 `int?`) · Infrastructure: `SharedClassConfigurationConfiguration.cs` (`IsRequired().HasDefaultValue(0)`) + o singură migrație nouă `AddInvitationSplit` (ambele tabele).
- Application `Features/SchoolClasses/`: `CreateSchoolClassCommandHandler`, `InitializeSchoolClassConfigurationCommandHandler`, `GetSchoolClassByIdHandler`, `Models/SchoolClassDto.cs`.
- Application `Features/ClassConfiguration/`: `Commands/SaveInvitationSplit`, `Queries/GetInvitationSplit`, `Models/InvitationSplitDto`, tag `InvitationSplitTag` în `CacheConstants.cs`.
- Web `Endpoints/ClassConfiguration.cs`: GET/PUT `invitation-split`.
- Client `features/class-configuration/`: `invitation-split/` (section + card), `services/invitation-split.feature.ts`, extindere `configuration.http.ts`, `configuration.store.ts`, `core/models/class-configuration.ts`, `configuration.constants.ts`; plasat în `invitations-section.html` sau imediat după, în `configuration.page.html`.
- Teste: unit `tests/Application.UnitTests/Features/ClassConfiguration/`, functional `tests/Application.FunctionalTests/Features/ClassConfiguration/`, spec-uri colocate Vitest, e2e `src/Client/e2e/specs/invitation-split.spec.ts`.

## Code Style
Backend ca `SaveRoomConfiguration` (nullable `int?` + `NotNull` + `GreaterThanOrEqualTo(0)` + `LessThanOrEqualTo(200)`, `[Authorize(Roles = Roles.Administrator)]`, `ICacheInvalidation` cu `ConfigurationTag` + `InvitationSplitTag`). Frontend ca `rooms-card`: signal forms, mesaje în română, `class="w-full!"`, aria-invalid/aria-describedby, `nz-form-label nzRequired`. Verificat în `.github/instructions/llms-full.txt` pentru `nz-input-number`/`nz-form`.

## Testing Strategy
- Unit: validator (limite, null) + handler (creează singleton, păstrează departamente/invitații/camere).
- Functional: round trip GET/PUT, 403 non-admin, 400 valori invalide, refresh de cache.
- Copiere pe clasă: unit `CreateSchoolClassCommandHandlerTests`, `GetSchoolClassByIdHandlerTests`; functional `ClassConfigurationSnapshotTests` (creare + inițializare copiază cele 5 valori; modificarea ulterioară a configurației nu schimbă clasa).
- Temporal: `tests/Infrastructure.IntegrationTests/TemporalSchemaTests.cs` după migrație (prima adăugare de coloane după `SetTemporalRetention`).
- Client: spec pentru card/section/feature/http (model: rooms), inclusiv linia „Total alocat”.
- Cache: salvarea nu strică celelalte secțiuni (PUT-urile existente nu trebuie să reseteze cele 5 valori și invers).
- Vizual (e2e Playwright): ecran desktop și mobil, stări normal, eroare, loading; capturi salvate în `screenshot-checks/invitation-split/` (git-ignored), verificate: aliniere, etichete, spațiere, focus, contrast, fără overflow.

## Boundaries
- Always: migrație cu `defaultValue: 0`; UI în română; ng-zorro; build + teste înainte de PR; branch nou + PR; review independent cu Haiku.
- Ask first: orice constrângere (blocantă) între total și sumă; afișarea valorilor pe clasă; dependențe noi.
- Never: edit pe migrații aplicate; Prettier pe client; commit pe `master`.

## Success Criteria
1. Cele 5 câmpuri apar sub „Număr de invitații", cu valorile salvate, propriul buton „Salvează” și linia „Total alocat: X din Y”.
2. Admin salvează; valorile persistă după reload; non-admin nu poate scrie (403).
3. Valori negative, zecimale, goale sau >200 sunt respinse în UI și în backend.
4. Salvarea altor secțiuni nu modifică valorile (și invers).
5. O clasă nouă sau inițializată primește cele 5 valori curente; schimbările ulterioare din configurare nu o afectează.
6. `dotnet build`, testele unit/functional/client și e2e trec; capturile desktop+mobil arată corect.
