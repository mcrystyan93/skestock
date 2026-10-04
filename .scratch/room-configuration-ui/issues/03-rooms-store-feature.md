# 03: Feature rooms în ConfigurationStore

Status: resolved

Type: task

Blocked by: 02

## Descriere

Implementăm state-ul sălilor și cererile de încărcare/salvare în store-ul existent.

Sursă: [specificația aprobată](../spec.md).

## Criterii de acceptare

- [x] withRooms pornește cu trei zerouri și compune loading/ProblemDetails proprii, fără a afecta celelalte features.
- [x] loadRooms aplică GET; saveRooms execută PUT apoi GET și aplică numai configurația confirmată.
- [x] Testele verifică loading, curățarea erorilor, eșec GET/PUT/GET după PUT și păstrarea datelor confirmate.

## Fișiere vizate

- `src/Client/src/app/features/class-configuration/services/rooms.feature.ts`
- `src/Client/src/app/features/class-configuration/services/configuration.store.ts`
- `src/Client/src/app/features/class-configuration/services/rooms.feature.spec.ts`

Dimensiune: 3 fișiere sursă/test sau raport; actualizările tracker-ului sunt administrative.

## Verificare

```bash
cd src/Client
npm test -- --watch=false --include="**/rooms.feature.spec.ts"
```

## Comments

- 2026-10-04: task implementat și verificat conform criteriilor de acceptare.
