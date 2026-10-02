# 04: Endpoint-urile GET și PUT pentru săli

Status: resolved
Type: task
Blocked by: 02, 03

## Descriere

Expunem query-ul și command-ul în grupul existent și verificăm contractul prin cereri HTTP reale.

Sursa: [specificația aprobată](../spec.md) și [planul aprobat](../../../tasks/plan.md).

## Criterii de acceptare

- [x] GET/PUT /api/ClassConfiguration/rooms returnează 200 cu cele trei valori; GET înainte de salvare returnează zero
  fără scrieri, PUT creează și actualizează configurația.
- [x] Anonimii primesc 401 pe ambele endpoint-uri; utilizatorul obișnuit poate citi, dar primește 403 la PUT;
  administratorul poate salva.
- [x] Payload cu orice câmp lipsă/null/negativ produce 400 application/problem+json și nu creează/modifică configurația.

## Verificare

```bash
./run-functional-tests.sh --filter FullyQualifiedName~RoomConfigurationEndpointTests
dotnet build
```

Endpoint-urile folosesc metode statice numite, ISender și ToOk. Testele folosesc autentificarea cookie existentă,
TestBase/Respawn și resetarea tag-ului cache. Checkpoint după 03–04: teste unitare focalizate și ciclul HTTP PUT → GET.

## Fișiere vizate

- `src/Web/Endpoints/ClassConfiguration.cs`
- `tests/Application.FunctionalTests/Features/ClassConfiguration/RoomConfigurationEndpointTests.cs`

## Comments

Sarcina este implementată și verificată conform criteriilor de acceptare.

