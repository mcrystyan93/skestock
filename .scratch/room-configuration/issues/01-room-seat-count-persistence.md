# 01: Persistența numărului de locuri

Status: resolved Type: task Blocked by: —

## Descriere

Extindem configurația globală cu numărul de locuri pentru cele trei săli și generăm migrația prin tooling-ul EF
existent.

Sursa: [specificația aprobată](../spec.md) și [planul aprobat](../../../tasks/plan.md).

## Criterii de acceptare

- [x] Modelul conține Room4SeatCount, Room2SeatCount și Room6SeatCount de tip int, obligatorii în mapping.
- [x] Migrația adaugă cele trei coloane cu zero pentru datele existente; designer-ul și snapshot-ul sunt generate prin
  CLI.
- [x] Nu schimbăm schema claselor sau comportamentul invitațiilor/departamentelor.

## Verificare

```bash
dotnet build
dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext
```

Inspectăm migrația generată: numai cele trei coloane noi, nenulabile, cu defaultValue 0. Aplicarea pe SQL Server este
verificată de setup-ul testelor funcționale.

## Fișiere vizate

- `src/Domain/Entities/SchoolClasses/SharedClassConfiguration.cs`
- `src/Infrastructure/Data/Configurations/SharedClassConfigurationConfiguration.cs`
- `src/Infrastructure/Data/Migrations/20261002191243_AddRoomSeatCounts.cs`
- `src/Infrastructure/Data/Migrations/20261002191243_AddRoomSeatCounts.Designer.cs`
- `src/Infrastructure/Data/Migrations/ApplicationDbContextModelSnapshot.cs`

## Comments

Sarcina este implementată și verificată conform criteriilor de acceptare.
