# 05: Persistența, cache-ul și regresiile configurației

Status: resolved
Type: task
Blocked by: 03, 04

## Descriere

Verificăm pe SQL Server și pipeline-ul Mediator interacțiunea configurației sălilor cu datele existente.

Sursa: [specificația aprobată](../spec.md) și [planul aprobat](../../../tasks/plan.md).

## Criterii de acceptare

- [x] GET → prima salvare → GET și GET → update → GET returnează imediat valorile noi; rămâne un singur singleton și
  toate câmpurile sunt persistate.
- [x] Salvarea sălilor păstrează invitațiile/departamentele; salvarea invitațiilor/departamentelor și ștergerea unui
  departament păstrează locurile.
- [x] Pipeline-ul CQRS aplică autorizarea și validarea fără scrieri la eșec; testele existente de inițializare/snapshot
  a claselor trec.

## Verificare

```bash
./run-functional-tests.sh --filter FullyQualifiedName~ClassConfiguration
```

Testăm autorizarea și la dispatch direct prin Mediator, independent de HTTP. Folosim TestBase/Respawn și resetarea
cache-ului între teste.

## Fișiere vizate

- `tests/Application.FunctionalTests/Features/ClassConfiguration/RoomConfigurationTests.cs`

## Comments

Sarcina este implementată și verificată conform criteriilor de acceptare.

