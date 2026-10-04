# Verificarea implementării numărului de locuri

Executată la 2026-10-04 după integrarea secțiunii Configuration.

- `dotnet build` — trecut, 0 warnings și 0 errors.
- `dotnet test tests/Application.UnitTests` — trecut, 765 teste.
- `./run-functional-tests.sh --filter FullyQualifiedName~ClassConfiguration` — trecut, 40 teste înaintea adăugării regresiei pentru valoarea istorică.
- `./run-functional-tests.sh --filter FullyQualifiedName~RoomConfiguration` — trecut după ultima schimbare, 9 teste; confirmă HTTP 200 la limita maximă, 400 peste limită, lipsa scrierii la payload invalid și GET-ul valorii istorice 250.
- `cd src/Client && npm run build` — trecut; compilatorul raportează numai warnings Angular nefolosite în alte componente existente.
- `cd src/Client && npm test -- --watch=false` — trecut, 237 teste în 58 fișiere. După ultima ajustare de tipografie: `npm test -- --watch=false --include="**/rooms-*.spec.ts"` a trecut cu 26 teste; testele store/HTTP au trecut în suita completă.
- `cd src/Client && npm run e2e -- --project=chromium --project=mobile e2e/specs/room-configuration.spec.ts` — trecut pe Chromium desktop și Pixel 7 mobil. E2E folosește API-ul real pentru autentificarea inițială, interceptează endpoint-ul rooms pentru a nu persista date și verifică validarea, payload-ul, layout-ul, navigarea cu tastatura și reîncărcarea paginii.

`graphify update .` — trecut după ultima modificare de cod.
- Redenumirea `Room1SeatCount` în `Room2SeatCount` și `room1SeatCount` în `room2SeatCount` — build și suitele de mai sus trecute; migrarea inițială neaplicată a fost regenerată cu coloana SQL `Room2SeatCount`, astfel nu este necesară migrare de date. AppHost a pornit cu Podman; origin-ul Playwright folosește host-ul `webfrontend-skestock.dev.localhost` din allowlist-ul CORS, iar browserul de test acceptă certificatul HTTPS local al API-ului.
