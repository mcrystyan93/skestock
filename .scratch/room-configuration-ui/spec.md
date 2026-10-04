# Specificație: Numărul de locuri în pagina Configuration

Status: Approved — aprobată de utilizator la 2026-10-04, cu limita 200 aplicată și în backend.

## Obiectiv

Administratorul poate consulta și salva numărul de locuri pentru Sala 4 (principală), Sala 2 și Sala 6 (secundare),
dintr-o secțiune nouă a paginii
Configuration. Aceasta este o singură capabilitate: editarea configurației
globale a locurilor, prin API-ul implementat anterior.

## Contract și comportament

- Pagina afișează secțiunea „Numărul de locuri”, după secțiunea Invitații.
- `RoomsSection` este containerul: injectează `ConfigurationStore`, transmite
  valorile și stările către card, afișează erorile API și coordonează salvarea.
- `RoomsCard` este componenta de prezentare: primește configurația/stările prin
  signal inputs, păstrează formularul local și emite intenția de salvare prin
  output. Nu injectează store-ul sau serviciul HTTP.
- Un singur `nz-card` conține trei elemente native `<input nz-input type="number">`,
  cu etichetele „Sala 4 — principală”, „Sala 2 — secundară”, „Sala 6 — secundară”.
- Fiecare valoare este obligatorie, întreagă, în intervalul inclusiv 0–200.
  Zero este valid. Câmpurile goale, valorile negative, fracționare sau peste 200
  împiedică salvarea și au mesaje în română asociate câmpului.
- Formularul folosește Angular Signal Forms; regulile required/min/max/integer
  sunt definite în schema formularului, în stilul paginii existente.
- Cardul conține butonul primar „Salvează”. Submit validează toate câmpurile;
  nu trimite cereri pentru formular invalid. Salvarea transmite toate trei valorile.
- Încărcarea inițială citește GET `/api/ClassConfiguration/rooms`. Pagina cheamă
  metoda de încărcare a feature-ului alături de metodele deja existente.
- PUT `/api/ClassConfiguration/rooms` persistă valorile. După succes, feature-ul
  reîncarcă configurația prin GET, conform feature-ului existent pentru invitații.
- Cardul reflectă configurația încărcată și valorile confirmate după salvare.
  În timpul cererii se afișează starea de loading și se blochează submit-urile
  repetate. La eșec, datele confirmate din store nu sunt înlocuite cu valori nesalvate.
- Stările rooms loading/error sunt independente de invitații și departamente;
  erorile vechi sunt curățate la începutul unei cereri pentru săli.
- Layout-ul urmează cardurile, spațierea, butoanele și mesajele ng-zorro existente;
  cele trei câmpuri se adaptează la ecranul mobil fără overflow.
- Label/id, mesaje de eroare asociate și stările aria-invalid/aria-busy permit
  utilizarea cu tastatură și cititoare de ecran.

## Tech stack

Versiunile declarate în clientul actual: Angular ^22.2.0, ng-zorro-antd ^22.1.1,
NgRx Signals/Operators ^22.0.1, TypeScript ~6.0.3 și Vitest ^5.0.2. Folosim
versiunile din lockfile și dependențele existente.

## State și HTTP

- Modelele client `RoomConfigurationDto` și `SaveRoomConfigurationRequest`
  conțin `room4SeatCount`, `room2SeatCount`, `room6SeatCount`, toate `number`.
- `ConfigurationHttp` primește metodele GET și PUT pentru ruta `rooms`.
- `withRooms()` extinde store-ul existent, cu configurația confirmată,
  `loadRooms` și `saveRooms`. Valorile inițiale sunt trei zerouri.
- Feature-ul compune `withLoadingFeature('rooms')` și
  `withProblemDetailsFeature('rooms')`; folosește `patchState`, `rxMethod` și
  `mapResponse`, după convențiile features existente.
- Containerul transmite request-ul valid către store, păstrând HTTP și state
  în stratul de servicii. Autentificarea folosește interceptorul existent.

## Comenzi

Din `src/Client`:

```bash
npm ci
npm exec ng generate component features/class-configuration/rooms/rooms-section --skip-tests
npm exec ng generate component features/class-configuration/rooms/rooms-card --skip-tests
npm run build
npm test -- --watch=false
```

Scaffolding-ul CLI se adaptează la numele fișierelor și template-urile separate
ale paginii actuale. `npm ci` este necesar doar pentru instalarea curată a
dependențelor. Din rădăcina repository-ului:

```bash
dotnet run --project src/AppHost
graphify update .
```

AppHost asigură API-ul și clientul pentru verificarea fluxului real în browser.

## Structura proiectului

- `src/Client/src/app/core/models/class-configuration.ts`: DTO/request client.
- `src/Client/src/app/features/class-configuration/services/configuration.http.ts`: HTTP.
- `src/Client/src/app/features/class-configuration/services/rooms.feature.ts`: feature nou.
- `src/Client/src/app/features/class-configuration/services/configuration.store.ts`: compunere feature.
- `src/Client/src/app/features/class-configuration/services/configuration.constants.ts`: limita 200.
- `src/Client/src/app/features/class-configuration/rooms/`: container și card prezentational,
  cu fișiere TypeScript/template separate și teste focalizate.
- `src/Client/src/app/features/class-configuration/configuration.page.ts` și `.html`: integrare.
- `.scratch/room-configuration-ui/spec.md`: această specificație; issues separate
  în `issues/` după aprobarea planului.

## Stil de cod

Standalone implicit, signal inputs/outputs, `inject()`, `linkedSignal` pentru
modelul local al formularului și control flow nativ Angular. Importăm doar
componentele/directivele ng-zorro necesare și folosim aliasurile existente.

Exemplu de contract al componentei de prezentare:

```ts
export class RoomsCard {
  readonly configuration = input.required<RoomConfigurationDto>();
  readonly loading = input(false);
  readonly onSave = output<void>();
}
```

Acesta urmează separarea card/container deja folosită pentru invitații.

## Strategia de verificare

- Teste Vitest focalizate pentru formular: 0 și 200 acceptate, valori goale,
  negative, fracționare și 201 respinse pentru fiecare sală; payload-ul valid
  include toate trei câmpurile.
- Teste feature/HTTP: încărcare, PUT urmat de GET, rezultat aplicat în state,
  stări loading și tratarea erorilor fără pierderea configurației confirmate.
- Build Angular obligatoriu și testele clientului pentru regresii.
- Verificare în browser pe desktop și mobil: încărcare, editare/salvare,
  validare inline și layout. E2E interceptează ruta rooms pentru a evita
  modificarea bazei de dezvoltare; testele funcționale verifică endpoint-ul real.
  Orice verificare blocată de mediu se raportează explicit.

## Boundaries

- Always: respectăm convențiile ng-zorro existente, Signal Forms,
  `patchState`, loading/error features, accesibilitatea și verificarea build-ului.
- Ask first: dependențe noi sau schimbări de cerințe în afara acestui scope.
- Never: HTTP/store injectat în cardul de prezentare, salvarea unui formular
  invalid, modificarea paginilor adiacente sau a schimbărilor locale preexistente,
  suprimarea erorilor pentru a obține un build verde.

Scope-ul aprobat aplică limita 200 în formular și în validatorul backend.
Configurațiile existente peste limită rămân citibile și pot fi corectate prin UI;
nu este necesară o migrație de bază de date.

## Criterii de succes

1. Secțiunea nouă conține un card cu trei nz-input-uri și un buton Salvează.
2. Cardul este prezentational; containerul coordonează store-ul existent.
3. Store-ul compune un feature dedicat pentru locuri, cu GET/PUT și stări separate.
4. Doar numerele întregi 0–200 inclusiv se pot salva din formular.
5. Valorile salvate sunt vizibile după confirmare și după reîncărcarea paginii.
6. Mesajele API/validare și loading-ul urmează componentele existente.
7. Build-ul, testele relevante și verificarea în browser confirmă comportamentul.

## Decizii aprobate

- Zero este valid; „pozitiv” în cerere este interpretat drept nenegativ, datorită min 0.
- Secțiunea este plasată după Invitații.
- Max 200 se aplică formularului frontend și validatorului backend, conform completării aprobate la 2026-10-04.
