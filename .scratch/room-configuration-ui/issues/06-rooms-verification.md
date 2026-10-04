# 06: Verificarea fluxului complet

Status: resolved

Type: task

Blocked by: 01, 02, 03, 04, 05

## Descriere

Executăm regresiile și verificăm secțiunea în browser cu endpoint-ul rooms interceptat, iar API-ul real cu testele funcționale; documentăm rezultatele și actualizăm graphify.

Sursă: [specificația aprobată](../spec.md).

## Criterii de acceptare

- [x] Build-urile și suitele relevante backend/client trec; orice verificare blocată este raportată concret.
- [x] Browser: încărcare, salvare 0/200, reîncărcare, validare fără PUT invalid, loading/error și navigare cu tastatura; mobil și desktop fără overflow.
- [x] Diff-ul respectă separarea prezentare/container și protejează schimbările preexistente; graphify este actualizat după cod.

## Fișiere vizate

- `.scratch/room-configuration-ui/verification.md`

Dimensiune: 1 fișiere sursă/test sau raport; actualizările tracker-ului sunt administrative.

## Verificare

```bash
dotnet build
dotnet test tests/Application.UnitTests
./run-functional-tests.sh --filter FullyQualifiedName~ClassConfiguration
(cd src/Client && npm run build && npm test -- --watch=false)
npm run e2e -- --project=chromium --project=mobile e2e/specs/room-configuration.spec.ts
graphify update .
```

Pornim AppHost cu `dotnet run --project src/AppHost` pentru verificarea browserului.

## Comments

- 2026-10-04: task implementat și verificat conform criteriilor de acceptare.
