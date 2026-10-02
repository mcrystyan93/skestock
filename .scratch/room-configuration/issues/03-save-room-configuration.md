# 03: Salvarea și validarea configurației sălilor

Status: resolved
Type: task
Blocked by: 01, 02

## Descriere

Adăugăm comanda de salvare pentru administratori, validatorul și handler-ul, cu teste unitare.

Sursa: [specificația aprobată](../spec.md) și [planul aprobat](../../../tasks/plan.md).

## Criterii de acceptare

- [x] Fiecare proprietate int? din command este obligatorie și nenegativă: null/lipsă/negativ sunt respinse,
  zero/pozitiv/int.MaxValue sunt acceptate.
- [x] Handler-ul creează sau actualizează singleton-ul, salvează toate trei valorile printr-un singur SaveChangesAsync
  și păstrează invitațiile/departamentele.
- [x] Command-ul cere rolul Administrator, implementează ICacheInvalidation pentru tag global și tag săli și returnează
  DTO-ul salvat.

## Verificare

```bash
dotnet test tests/Application.UnitTests --filter FullyQualifiedName~RoomConfiguration
dotnet build
```

Folosim ca-usecase și stilul use case-ului SaveInvitationCount. Testăm fiecare câmp independent și verificăm păstrarea
Id-ului singleton-ului la actualizare.

## Fișiere vizate

- `src/Application/Features/ClassConfiguration/Commands/SaveRoomConfiguration/SaveRoomConfigurationCommand.cs`
- `src/Application/Features/ClassConfiguration/Commands/SaveRoomConfiguration/SaveRoomConfigurationCommandValidator.cs`
- `src/Application/Features/ClassConfiguration/Commands/SaveRoomConfiguration/SaveRoomConfigurationCommandHandler.cs`
- `tests/Application.UnitTests/Features/ClassConfiguration/RoomConfigurationValidatorTests.cs`
- `tests/Application.UnitTests/Features/ClassConfiguration/RoomConfigurationCommandTests.cs`

## Comments

Sarcina este implementată și verificată conform criteriilor de acceptare.

