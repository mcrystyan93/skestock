# Spec: Teste Playwright pentru login

## Objective
Adaugă teste browser end-to-end pentru pagina `/login`, verificând validarea locală, autentificarea prin backendul real și afișarea erorilor backend. Scopul este să protejeze traseul real al utilizatorului și contractul de eroare dintre ASP.NET Identity și Angular.

Decizii confirmate:
- Acoperim atât backendul real, cât și răspunsuri backend interceptate pentru erori dificil de reprodus determinist.
- Scenariul de succes folosește un cont dedicat mediului de test, furnizat prin variabile de mediu; nu adăugăm credențiale în repository.

## Tech Stack
- Angular 22, ASP.NET Core Identity cookie auth, Playwright Test (deja instalat/configurat).
- Aplicația completă se pornește prin .NET Aspire (`src/AppHost`); Playwright rulează proiectele existente `chromium` și `mobile`.
- Endpointul de autentificare este `POST /api/Users/login?useCookies=true`.

## Commands
Pornire aplicație, din rădăcina repository-ului:
```bash
dotnet run --project src/AppHost
```

Teste browser, din `src/Client`:
```bash
npm run e2e
PLAYWRIGHT_LOGIN_EMAIL=<test-account-email> PLAYWRIGHT_LOGIN_PASSWORD=<test-account-password> npm run e2e -- --project=chromium
```

Prima comandă rulează toate proiectele Playwright; a doua selectează Chromium și furnizează credențialele pentru testul de autentificare reușită. `PLAYWRIGHT_BASE_URL` poate suprascrie URL-ul aplicației, conform configurației existente.

## Project Structure
- `src/Client/e2e/specs/login.spec.ts` — scenariile browser pentru formă, autentificare și erori.
- `src/Client/playwright.config.ts` — reutilizăm proiectele și setările existente; nu adăugăm server lifecycle sau dependențe noi.
- Posibile ajustări strict necesare pentru comportamentul acoperit: `src/Client/src/app/features/login/` și serviciile existente pentru erori/autentificare.

## Code Style
Testează interfața prin roluri, placeholder-e și text vizibil; verifică și efectul asupra requestului:
```ts
await page.goto('/login');
await page.getByPlaceholder('Email').fill('invalid');
await page.getByPlaceholder('Parola').fill('test');
await page.getByRole('button', { name: 'Login' }).click();

await expect(page.getByText('Email invalid')).toBeVisible();
```
Folosește locatori accesibili și așteptări Playwright (`expect`) în locul timeout-urilor fixe. Interceptează doar requesturile necesare scenariilor simulate; nu înlocui backendul real în testele live.

## Testing Strategy
- **Validare frontend:** email și parolă goale, format de email invalid; mesajele corespunzătoare sunt vizibile și requestul `POST .../login` nu este trimis.
- **Backend real:** credențiale dedicate valide duc la `/school-classes`; sesiunea pe cookie supraviețuiește unui reload. Un email de test inexistent cu o parolă greșită este respins de backend, utilizatorul rămâne pe login și vede un mesaj generic în română.
- **Erori backend simulate:** răspuns `400` cu contractul `ProblemDetails.error.errors` afișează erorile de validare într-un mod lizibil; `500`/`503` și eroare de rețea afișează un fallback lizibil, fără JSON brut, stack trace sau credențiale. Un correlation ID din răspuns este afișat când există.
- **Interacțiune/recovery:** indicatorul de încărcare apare cât timp loginul este în așteptare, submitul repetat nu generează requesturi duplicate, iar o nouă încercare curăță mesajul de eroare anterior.
- **Stare/auth și responsive:** un login respins nu acordă acces la rute protejate; pagina rămâne utilizabilă în proiectele Chromium și mobile existente.
- Folosește contul valid doar pentru scenariul de succes; testele negative folosesc identități inexistente pentru a nu declanșa blocarea contului.

## Boundaries
- **Always:** păstrează requestul live pentru testele de integrare; folosește credențiale numai din env; nu persista cookies/storage state în repository; validează mesajul vizibil și faptul că requesturile sunt/no sunt emise.
- **Ask first:** modificări în workflow-urile CI/CD sau introducerea de pachete/dependințe noi.
- **Never:** expune parole în cod, loguri sau artefacte de test; folosi date de producție; ocoli autentificarea ori dezactiva rutarea protejată pentru a face testul să treacă.

## Success Criteria
1. Validarea clientului blochează trimiterea pentru câmpuri goale și email invalid, cu mesaje vizibile.
2. Un cont dedicat valid autentifică prin API-ul real, ajunge pe ruta protejată și rămâne autentificat după reload.
3. Un login respins real și răspunsurile simulate `400`, `5xx` și eroare de rețea sunt prezentate utilizatorului în română, fără conținut tehnic brut; erorile per-câmp și correlation ID-ul sunt vizibile când sunt furnizate.
4. Eroarea de autentificare nu lasă aplicația într-o stare locală autentificată; trimiterea în așteptare nu produce requesturi duplicate, iar retry-ul este posibil.
5. Testele trec cu `npm run e2e` în proiectele Playwright Chromium și mobile, fără credențiale committed.

## Open Questions
- Niciuna pentru această versiune. Schimbările CI/CD rămân în afara scope-ului până la o aprobare separată.
