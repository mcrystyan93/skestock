# Implementation Plan: Teste Playwright pentru login

## Overview
Extindem suita E2E existentă din `src/Client/e2e` pentru a verifica atât interacțiunile locale ale formularului de login, cât și răspunsurile contractuale ale backendului și un login real prin cookie auth. Nu se adaugă dependențe sau infrastructură nouă; AppHost rămâne responsabil de pornirea aplicației, iar contul live este configurat separat prin environment.

## Architecture Decisions
- Păstrăm `playwright.config.ts` și proiectele Chromium/mobile existente; nu adăugăm alt runner sau `webServer`.
- Testele UI și erorile simulate interceptează numai requesturile relevante; loginul pozitiv și respingerea credențialelor sunt verificate cu Identity API real.
- Testul de succes citește `PLAYWRIGHT_LOGIN_EMAIL` și `PLAYWRIGHT_LOGIN_PASSWORD`; dacă variabilele lipsesc, doar scenariile live care depind de cont sunt omise explicit. Credențialele nu se scriu în teste, fixture-uri sau storage state.
- Pentru 400 se folosește contractul `ProblemDetails.error.errors` al aplicației. Dacă verificarea relevă că forma de login nu prezintă erorile de validare deși componenta primește datele, ajustarea rămâne locală loginului. Eșecul loginului trebuie să readucă și starea locală `isAuthenticated` la `false`.
- Nu modificăm workflow-urile CI/CD în acest scope.

## Task List

### Phase 1: Teste UI independente de backend
- [ ] Task 1: Validarea formularului și interacțiunile de bază

### Checkpoint: Contract UI
- [ ] Testele de input invalid nu emit login request.
- [ ] Formularul poate fi testat în Chromium și mobile cu locatori accesibili.

### Phase 2: Contractul erorilor și starea de autentificare
- [ ] Task 2: Afișarea erorilor backend și correlation ID
- [ ] Task 3: Fallback pentru erori de rețea, stare auth și retry/loading

### Checkpoint: Erori
- [ ] Validarea server-side, erorile generale, eroarea de rețea și retry-ul au rezultat vizibil și determinist.
- [ ] Un eșec nu permite navigarea către rute protejate.

### Phase 3: Autentificarea reală și completarea suitei
- [ ] Task 4: Login real, sesiune pe cookie și respingerea credențialelor
- [ ] Task 5: Rulare E2E completă și actualizarea graphify

### Checkpoint: Complete
- [ ] `npm run e2e` trece în proiectele Chromium și mobile (scenariile live rulează când env-ul contului este configurat).
- [ ] Toate criteriile din `tasks/SPEC-login-playwright.md` sunt acoperite.

## Risks and Mitigations
| Risk | Impact | Mitigation |
|------|--------|------------|
| Testul happy-path depinde de SQL/Redis/AppHost și de utilizator provisionat | Mediu | Separă testele simulate de cele live; omite doar happy-path când env-ul lipsește și rulează-l explicit în mediu dedicat. |
| Răspunsurile standard Identity diferă de contractul custom al aplicației | Mediu | Folosește API real pentru rezultatul credentialelor; folosește payload custom simulat doar pentru contractul error-display. |
| Teste rulate paralel pot împărți aceeași stare backend | Scăzut | Loginul nu modifică profilul; negativele folosesc utilizator inexistent; fiecare context Playwright are propriile cookies. |
| Eroare server-side de validare poate fi primită dar nedesenată de UI | Mediu | Testul verifică textul efectiv; remediază legătura locală dintre login form și componenta de eroare dacă e necesar. |
| Eroarea loginului păstrează starea locală autentificată | Ridicat | Include verificare de acces la ruta protejată după eșec și revino cu state update minim în ramura de eroare. |

## Open Questions
- Niciuna; spec-ul și locațiile task/plan dedicate au fost aprobate.
