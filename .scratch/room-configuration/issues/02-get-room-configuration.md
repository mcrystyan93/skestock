# 02: Citirea configurației sălilor

Status: resolved
Type: task
Blocked by: 01

## Descriere

Adăugăm query-ul autentificat, DTO-ul și caching-ul, împreună cu teste unitare pentru citire.

Sursa: [specificația aprobată](../spec.md) și [planul aprobat](../../../tasks/plan.md).

## Criterii de acceptare

- [x] Query-ul returnează valorile persistate; când singleton-ul lipsește, returnează trei zerouri fără să creeze
  înregistrări.
- [x] Query-ul citește fără tracking, implementează ICacheableQuery și folosește tag global plus RoomConfigurationTag,
  cu expirare de cinci minute.
- [x] DTO-ul expune cele trei câmpuri int; query-ul are atributul de autorizare pentru utilizator autentificat.

## Verificare

```bash
dotnet test tests/Application.UnitTests --filter FullyQualifiedName~RoomConfigurationQueryTests
dotnet build
```

Folosim ca-usecase din src/Application și fixture-ul ClassConfigurationTestDbContext. Testele sunt scrise înainte de
handler; verificăm și absența înregistrărilor/tracking-ului după query. Checkpoint după 01–02: build și testele
ClassConfiguration.

## Fișiere vizate

- `src/Application/Features/ClassConfiguration/Models/RoomConfigurationDto.cs`
- `src/Application/Features/ClassConfiguration/CacheConstants.cs`
- `src/Application/Features/ClassConfiguration/Queries/GetRoomConfiguration/GetRoomConfigurationQuery.cs`
- `src/Application/Features/ClassConfiguration/Queries/GetRoomConfiguration/GetRoomConfigurationQueryHandler.cs`
- `tests/Application.UnitTests/Features/ClassConfiguration/RoomConfigurationQueryTests.cs`

## Comments

Sarcina este implementată și verificată conform criteriilor de acceptare.

