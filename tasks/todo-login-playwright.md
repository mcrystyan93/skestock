# Tasks: login Playwright

## Task 1: Validarea formularului și interacțiunile de bază (done)

**Description:** Adaugă teste browser pentru email/parolă obligatorii, format de email, lipsa requestului la input invalid și vizibilitatea parolei.

**Acceptance criteria:**
- [x] Email și parolă goale afișează mesajele frontend și nu trimit `POST /api/Users/login`.
- [x] Email invalid afișează mesajul frontend și nu trimite request.
- [x] Toggle-ul de vizibilitate schimbă tipul câmpului parolei fără să piardă valoarea.

**Verification:**
- [x] Scenariile Chromium și mobile au trecut cu backendul simulat.
- [x] Testul existent `seed.spec.ts` continuă să treacă.

**Dependencies:** None

**Files likely touched:**
- `src/Client/e2e/specs/login.spec.ts`

**Estimated scope:** Small

## Task 2: Afișarea erorilor backend și correlation ID (done)

**Description:** Testează răspunsurile 400 cu validări per-câmp și 5xx; ajustează prezentarea formularului să redea erorile server-side și să afișeze correlation ID-ul fără a expune detalii tehnice.

**Acceptance criteria:**
- [x] Răspunsul 400 prezintă erorile de validare într-un mod lizibil și correlation ID-ul când este furnizat.
- [x] Răspunsurile 500/503 prezintă mesajul localizat, correlation ID-ul și nu expun JSON brut sau stack trace.

**Verification:**
- [x] Scenariile Chromium și mobile au trecut cu backendul simulat.
- [x] `cd src/Client && npm test -- --watch=false`
- [x] `cd src/Client && npm run build`

**Dependencies:** Task 1

**Files likely touched:**
- `src/Client/e2e/specs/login.spec.ts`
- `src/Client/src/app/features/login/form/login-form.html`
- `src/Client/src/app/shared/errors/ui/error-alert.html`
- `src/Client/src/app/shared/errors/ui/problem-detail/problem-detail-text.ts`

**Estimated scope:** Medium

## Task 3: Fallback pentru erori de rețea, stare auth și retry/loading (done)

**Description:** Normalizează erorile de transport fără payload ProblemDetails, nu marchează utilizatorul autentificat înainte de succes, apoi testează fallbackul, starea după eșec, loading, prevenirea dublării requesturilor și retry-ul.

**Acceptance criteria:**
- [x] În timpul requestului submitul indică loading și clickurile repetate nu produc requesturi duplicate.
- [x] O încercare nouă după eșec golește mesajul anterior și poate fi trimisă.
- [x] Eroarea de rețea afișează fallback generic în română.
- [x] O rută protejată rămâne inaccesibilă după login respins.

**Verification:**
- [x] Scenariile Chromium și mobile au trecut cu backendul simulat.

**Dependencies:** Task 2

**Files likely touched:**
- `src/Client/e2e/specs/login.spec.ts`
- `src/Client/src/app/shared/errors/services/problem-detail.feature.ts`
- `src/Client/src/app/core/auth/services/auth.store.ts`
- `src/Client/src/app/features/login/form/login-form.ts`
- `src/Client/src/app/features/login/form/login-form.html`

**Estimated scope:** Small

## Task 4: Login real, sesiune pe cookie și respingerea credențialelor

**Description:** Folosește un cont dedicat disponibil în mediul de test pentru login real; verifică navigarea, persistența cookie-ului după reload și respingerea reală a unui email inexistent cu mesaj vizibil.

**Status:** Testele de integrare sunt scrise; rularea live este blocată local deoarece containerul SQL Server pentru AppHost iese cu cod 255 (`Access is denied`) pe volumul persistent `skestock-db-data`. Volumul nu a fost modificat sau șters.

**Acceptance criteria:**
- [ ] Cu `PLAYWRIGHT_LOGIN_EMAIL` și `PLAYWRIGHT_LOGIN_PASSWORD` configurate, loginul real ajunge la `/school-classes`.
- [ ] După reload, utilizatorul rămâne autentificat și ruta protejată rămâne accesibilă.
- [ ] Identitatea inexistentă este respinsă de API-ul real și utilizatorul rămâne pe login cu mesaj generic.
- [ ] Fără variabilele de mediu, numai scenariile care necesită contul live sunt omise explicit; restul E2E rulează.

**Verification:**
- [ ] Pornește `dotnet run --project src/AppHost`.
- [ ] `cd src/Client && PLAYWRIGHT_LOGIN_EMAIL=<test-account-email> PLAYWRIGHT_LOGIN_PASSWORD=<test-account-password> npm run e2e -- --project=chromium`

**Dependencies:** Tasks 1-3

**Files likely touched:**
- `src/Client/e2e/specs/login.spec.ts`

**Estimated scope:** Small

## Task 5: Rulare E2E completă și actualizarea graphify

**Description:** Rulează toate testele din proiectele desktop și mobile, remediază doar regresiile din scope și actualizează knowledge graph-ul după schimbările de cod.

**Status:** Scenariile simulate au trecut în Chromium și mobile; testele live rămân blocate de problema SQL Server documentată la Task 4.

**Acceptance criteria:**
- [ ] Suita E2E trece în proiectele Chromium și mobile.
- [x] Nu sunt introduse credențiale sau browser storage state în repository.
- [x] `graphify update .` finalizează după schimbările de cod.

**Verification:**
- [ ] `cd src/Client && npm run e2e`
- [x] `graphify update .`

**Dependencies:** Task 4

**Files likely touched:**
- `src/Client/e2e/specs/login.spec.ts`
- `tasks/SPEC-login-playwright.md`

**Estimated scope:** Small

## Checkpoint: După Tasks 1-3
- [x] Testele simulate și cele de validare trec în Chromium și mobile.
- [x] Buildul Angular reușește; CLI raportează câteva avertismente NG8113.
- [x] Loginul respins nu permite accesul la rute protejate.

## Checkpoint: Complete
- [ ] Criteriile de succes din spec sunt îndeplinite.
- [ ] Suita E2E desktop/mobile și scenariile live configurate trec.
