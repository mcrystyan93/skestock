# Plan de implementare: Configurația locurilor pentru săli

Status: Complete — specificația, planul și sarcinile au fost aprobate; implementarea finalizată la 2026-10-02.

## Obiectiv și sursa cerințelor

Implementăm configurația globală pentru Sala 4 (principală), Sala 2 și Sala 6 (secundare),
conform [specificației aprobate](../.scratch/room-configuration/spec.md). Scope: model persistat, migrație, CQRS,
GET/PUT și teste backend.

## Decizii tehnice

- Extindem singleton-ul `SharedClassConfiguration` cu trei proprietăți `int`. Nu introducem entități noi, identificatori
  dinamici sau configurare per clasă.
- Mapping-ul EF declară proprietățile obligatorii. Generăm migrația
  `AddRoomSeatCounts` cu `dotnet ef`; verificăm că noile coloane sunt populate cu zero pentru înregistrările existente.
- DTO-ul folosește trei `int`. Comanda folosește trei `int?`, pentru a distinge câmpurile lipsă/null de zero.
  Validatorul impune prezența și valori >= 0; handler-ul mapează valorile validate în modelul nenulabil.
- Generăm use case-urile cu template-ul `ca-usecase`, apoi adaptăm tipurile
  `Result`, validatorul, autorizarea și cache-ul la convențiile locale.
- Query-ul returnează un DTO cu zero dacă singleton-ul lipsește, fără scrieri. Comanda creează singleton-ul la nevoie și
  persistă cele trei valori printr-un singur `SaveChangesAsync`, păstrând invitațiile și departamentele.
- Query-ul utilizează tag-ul global și un tag dedicat `RoomConfigurationTag`, cu expirare de cinci minute, conform
  query-ului existent pentru invitații. Comanda invalidează aceleași tag-uri.
- Grupul existent `ClassConfiguration` expune ruta `rooms` prin GET și PUT. GET cere autentificare; PUT cere rolul
  Administrator. Aplicăm aceleași restricții prin atribute pe cererile Mediator.
- Păstrăm toate modificările deja prezente în working tree, inclusiv mutările entităților în subdirectoare. Nu
  refactorizăm codul adiacent.

## Dependențe și ordine

```text
Proprietăți domeniu + mapping EF
    ├── migrație generată
    └── DTO + tag cache
            ├── query + unit tests
            └── command + validator + unit tests
                        ↓
                endpoint-uri GET/PUT
                        ↓
             teste funcționale CQRS/HTTP
                        ↓
              build, regresii, graphify
```

## Etape

- [x] 1. **Persistență:** proprietăți, mapping și migrație (aproximativ cinci fișiere, incluzând designer-ul și snapshot-ul
   generate).
- [x] 2. **Citire:** DTO, tag cache, query/handler și teste unitare pentru valori salvate și configurație absentă (aproximativ
   cinci fișiere).
- [x] 3. **Salvare:** command, validator, handler și teste unitare pentru validare, create/update și păstrarea configurației
   existente (aproximativ cinci fișiere).
- [x] 4. **HTTP:** adăugarea GET/PUT în grupul existent și teste funcționale pentru contract, autorizare și payload invalid
   (aproximativ două fișiere).
- [x] 5. **Persistență/cache/regresii:** teste funcționale CQRS pentru singleton, refresh după GET inițial și update,
   păstrarea invitațiilor/departamentelor și păstrarea locurilor când acestea sunt actualizate (un fișier de teste nou).
- [x] 6. **Verificare finală:** build, teste relevante și toate unit tests Application, verificarea migrației/modelului,
   revizuirea diff-ului și `graphify update .`.

## Checkpoint-uri

- După etapele 1–2: build și testele unitare pentru citire; verificarea structurii migrației și absenței scrierilor la
  GET fără configurație.
- După etapele 3–4: build și testele unitare/funcționale focalizate; verificarea unui ciclu HTTP PUT → GET și a
  răspunsurilor 400/401/403.
- După etapele 5–6: toate criteriile specificației verificate; raportăm separat orice verificare blocată de mediu, fără
  a declara acele teste ca trecute.

## Verificare executabilă

```bash
dotnet build
dotnet test tests/Application.UnitTests --filter FullyQualifiedName~ClassConfiguration
./run-functional-tests.sh --filter FullyQualifiedName~ClassConfiguration
dotnet test tests/Application.UnitTests
dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext
graphify update .
```

Testele sunt scrise înaintea comportamentului corespunzător, apoi executate în ciclul red/green, folosind fixture-urile
existente. Testele funcționale folosesc
`TestBase`/Respawn și elimină cache-ul configurației între cazuri.

## Riscuri și măsuri

| Risc                                                      | Măsură                                                                             |
|-----------------------------------------------------------|------------------------------------------------------------------------------------|
| Payload lipsă confundat cu zero                           | Proprietăți nullable în command și validare explicită de prezență                  |
| GET returnează valori vechi                               | Teste cu cache încălzit înainte de create și update                                |
| Actualizarea suprascrie invitații/departamente sau locuri | Teste de păstrare a valorilor în ambele direcții                                   |
| Modificări locale preexistente în domeniu                 | Edităm fișierele în locațiile actuale și verificăm diff-ul limitat la feature      |
| Container runtime indisponibil                            | Verificăm setup-ul existent; raportăm concret orice blocaj pentru functional tests |
| Model EF diferit de migrație                              | Generare prin CLI și verificare `has-pending-model-changes`                        |

## Task tracker și execuție

Sarcinile sunt în `.scratch/room-configuration/issues/`, conform tracker-ului proiectului. Fiecare issue conține
dependențe, criterii de acceptare și verificare. Etapa Tasks este aprobată, implementată și verificată. Nu există un duplicat
`tasks/todo.md`.

1. [Persistența numărului de locuri](../.scratch/room-configuration/issues/01-room-seat-count-persistence.md)
2. [Citirea configurației sălilor](../.scratch/room-configuration/issues/02-get-room-configuration.md)
3. [Salvarea și validarea configurației sălilor](../.scratch/room-configuration/issues/03-save-room-configuration.md)
4. [Endpoint-urile GET și PUT pentru săli](../.scratch/room-configuration/issues/04-room-configuration-http.md)
5. [Persistența, cache-ul și regresiile configurației](../.scratch/room-configuration/issues/05-room-configuration-functional.md)
6. [Verificarea finală a implementării](../.scratch/room-configuration/issues/06-room-configuration-verification.md)

Implementarea se execută secvențial, în acest chat. Testele unitare și funcționale pot fi rulate independent când
resursele permit. Nu sunt necesari subagenți pentru această schimbare.

## Întrebări deschise

Nu există întrebări de cerințe rămase după aprobarea specificației.
