# 06: Store-ul paginii Configuration

**What to build:** Mută datele și operațiile API ale paginii Configuration într-un NgRx SignalStore scoped la container,
păstrând UI-ul existent funcțional în această etapă.

**Blocked by:** None.

**Status:** pending-review

**Labels:** refactor, frontend

## Acceptance criteria

- [ ] Store-ul gestionează citirea și salvarea prin endpoint-urile existente, plus stările de încărcare, salvare,
  succes, eroare și permisiunea `canManage`; operațiile RxJS folosesc `rxMethod`.
- [ ] Lista salvată de departamente folosește `withEntities`; store-ul expune drafturi editabile pentru cele două
  formulare, iar salvarea construiește un singur payload cu `invitationCount` și `departments`.
- [ ] Pagina furnizează store-ul cu scope local, îl încarcă la deschidere și păstrează funcționale validările, stările
  read-only și salvarea curentă.

## Verification

- [ ] `cd src/Client && npm run build`
- [ ] Verifică manual încărcarea configurării, valoarea `canManage`, păstrarea valorilor după eroare și trimiterea
  payload-ului complet la salvare.

**Dependencies:** None.

**Files likely touched:**

- `src/Client/src/app/features/class-configuration/services/configuration.store.ts`
- `src/Client/src/app/features/class-configuration/configuration.page.ts`
- `src/Client/src/app/features/class-configuration/configuration.page.html`

**Estimated scope:** Medium (3 files).
