# 04: Cardul pentru numărul de locuri

Status: resolved

Type: task

Blocked by: 02

## Descriere

Generăm componenta de prezentare prin CLI și implementăm formularul local cu Signal Forms și ng-zorro.

Sursă: [specificația aprobată](../spec.md).

## Criterii de acceptare

- [x] Un nz-card conține trei input-uri nz-input cu etichetele sălilor și un buton Salvează; fără injectare store/HTTP.
- [x] Fiecare câmp acceptă întregi 0–200; teste pentru 0/200, gol/-1/fracții/201 și payload complet.
- [x] Loading blochează editarea/submit repetat; erorile în română sunt asociate câmpurilor și layout-ul este responsive.

## Fișiere vizate

- `src/Client/src/app/features/class-configuration/rooms/rooms-card.ts`
- `src/Client/src/app/features/class-configuration/rooms/rooms-card.html`
- `src/Client/src/app/features/class-configuration/rooms/rooms-card.spec.ts`
- `src/Client/src/app/features/class-configuration/services/configuration.constants.ts`

Dimensiune: 4 fișiere sursă/test sau raport; actualizările tracker-ului sunt administrative.

## Verificare

```bash
cd src/Client
npm test -- --watch=false --include="**/rooms-card.spec.ts"
npm run build
```

## Comments

- 2026-10-04: task implementat și verificat conform criteriilor de acceptare.
