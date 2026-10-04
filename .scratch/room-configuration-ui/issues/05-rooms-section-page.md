# 05: Secțiunea și integrarea în pagină

Status: resolved

Type: task

Blocked by: 03, 04

## Descriere

Generăm containerul, îl conectăm la store și îl afișăm în pagina Configuration.

Sursă: [specificația aprobată](../spec.md).

## Criterii de acceptare

- [x] Secțiunea Numărul de locuri apare după Invitații; pagina încarcă configurația sălilor la inițializare.
- [x] Containerul transmite date/loading, afișează erorile API și salvează toate trei valorile numai după validare.
- [x] Testele verifică salvare validă, lipsa salvării pentru input invalid/loading și sincronizarea valorilor confirmate.

## Fișiere vizate

- `src/Client/src/app/features/class-configuration/rooms/rooms-section.ts`
- `src/Client/src/app/features/class-configuration/rooms/rooms-section.html`
- `src/Client/src/app/features/class-configuration/rooms/rooms-section.spec.ts`
- `src/Client/src/app/features/class-configuration/configuration.page.ts`
- `src/Client/src/app/features/class-configuration/configuration.page.html`

Dimensiune: 5 fișiere sursă/test sau raport; actualizările tracker-ului sunt administrative.

## Verificare

```bash
cd src/Client
npm test -- --watch=false --include="**/rooms-section.spec.ts"
npm run build
```

## Comments

- 2026-10-04: task implementat și verificat conform criteriilor de acceptare.
