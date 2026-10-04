# 02: Contractul HTTP client

Status: resolved

Type: task

Blocked by: 01

## Descriere

Adăugăm modelele și metodele HTTP pentru configurația sălilor, cu teste de contract.

Sursă: [specificația aprobată](../spec.md).

## Criterii de acceptare

- [x] DTO și request includ room4SeatCount, room2SeatCount și room6SeatCount ca number.
- [x] GET/PUT folosesc /api/ClassConfiguration/rooms; testele verifică verbul, URL-ul, payload-ul și răspunsul.
- [x] Autentificarea utilizează infrastructura HTTP existentă.

## Fișiere vizate

- `src/Client/src/app/core/models/class-configuration.ts`
- `src/Client/src/app/features/class-configuration/services/configuration.http.ts`
- `src/Client/src/app/features/class-configuration/services/configuration.http.spec.ts`

Dimensiune: 3 fișiere sursă/test sau raport; actualizările tracker-ului sunt administrative.

## Verificare

```bash
cd src/Client
npm test -- --watch=false --include="**/configuration.http.spec.ts"
```

## Comments

- 2026-10-04: task implementat și verificat conform criteriilor de acceptare.
