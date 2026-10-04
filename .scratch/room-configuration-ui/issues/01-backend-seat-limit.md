# 01: Limita 200 în backend

Status: resolved

Type: task

Blocked by: None

## Descriere

Extindem validatorul comenzii existente; endpoint-urile și persistența rămân pe același contract.

Sursă: [specificația aprobată](../spec.md).

## Criterii de acceptare

- [x] Fiecare câmp acceptă 0 și 200 și respinge 201/int.MaxValue cu LessThanOrEqualTo; required/min rămân active.
- [x] PUT invalid returnează 400 și păstrează configurația persistată; teste pentru fiecare sală.
- [x] GET poate returna o valoare istorică peste 200 fără a modifica datele.

## Fișiere vizate

- `src/Application/Features/ClassConfiguration/Commands/SaveRoomConfiguration/SaveRoomConfigurationCommandValidator.cs`
- `tests/Application.UnitTests/Features/ClassConfiguration/RoomConfigurationValidatorTests.cs`
- `tests/Application.FunctionalTests/Features/ClassConfiguration/RoomConfigurationEndpointTests.cs`
- `tests/Application.FunctionalTests/Features/ClassConfiguration/RoomConfigurationTests.cs`

Dimensiune: 4 fișiere sursă/test sau raport; actualizările tracker-ului sunt administrative.

## Verificare

```bash
dotnet test tests/Application.UnitTests --filter FullyQualifiedName~RoomConfiguration
./run-functional-tests.sh --filter FullyQualifiedName~RoomConfiguration
dotnet build
```

## Comments

- 2026-10-04: task implementat și verificat conform criteriilor de acceptare.
