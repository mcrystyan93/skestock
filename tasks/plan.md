# Plan: graficul consumului pe zi, saptamana si luna

Spec aprobata: `docs/specs/class-consumption-periods.md`.

## Decizii

- Refolosim punctele zilnice, deja completate cu zero, primite prin
  `GET /api/statistics/class/{classId}/daily-consumption`. Nu modificam API,
  store-ul, persistenta sau statisticile globale pentru ultimele 7/30 zile.
- Grupam datele in client dupa saptamana luni-duminica si luna calendaristica
  folosind chei `YYYY-MM-DD` in UTC pentru calcule, fara sa schimbam semantica
  zilelor locale din payload. Mediile pe perioada includ prima/ultima perioada
  chiar daca sunt partiale. Totalurile zilnice si media zilnica raman cele din
  raspunsul API.
- Selectorul cu trei segmente sta in card, iar graficul primeste granularitatea
  si deriveaza seria, linia mediei, etichetele si tooltipul. Schimbarea
  segmentului nu declanseaza un nou request si nu atinge filtrele.

## Dependente si ordine

1. Gruparea pura si testele sale stabilesc semantica numerica si granita dintre
   perioade.
2. Cardul si graficul folosesc transformarea verificata; apoi se verifica
   interactiunea si accesibilitatea.

## Lista de sarcini

Sarcinile si checkpoint-urile executabile sunt in `tasks/todo.md`.

## Riscuri si mitigari

| Risc | Mitigare |
|---|---|
| Decalaj de fus orar la limita de saptamana | Operatii UTC pe datele civile `DateOnly` serializate, fara convertirea in ora locala a browserului |
| Perioade fara consum sau partiale | Grupare din seria zilnica deja completata, teste pentru zero-uri si prima/ultima perioada |
| Tooltip ambigu pentru perioada | Eticheta explicita de interval pentru saptamana si de luna/an pentru luna |

## Verificare finala

Testele Vitest vizate si build-ul Angular trec; comutarea pastreaza filtrele,
mesajul gol, totalurile si mediile conform specificatiei.

## Intrebari deschise

Niciuna.
