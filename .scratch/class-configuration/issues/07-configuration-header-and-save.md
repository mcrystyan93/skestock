# 07: Header ng-zorro și salvarea comună

**What to build:** Extrage antetul paginii într-o componentă cu `nz-page-header`, menu-toggle și butonul unic de salvare
în `nz-page-header-extra`.

**Blocked by:** 06 — Store-ul paginii Configuration.

**Status:** pending-review

**Labels:** refactor, frontend

## Acceptance criteria

- [ ] Header-ul este o componentă standalone cu `ske-menu-toggle`, titlul „Configurare” și descrierea paginii în
  `nz-page-header`; folosește `nz-typography` pentru textul potrivit.
- [ ] `nz-page-header-extra` conține singurul buton „Salvează configurarea”; acesta emite către container, care
  validează formularele și cheamă store-ul o singură dată.
- [ ] Butonul respectă `canManage` și starea de salvare; theme switcher-ul și comportamentul responsive rămân
  disponibile.

## Verification

- [ ] `cd src/Client && npm run build`
- [ ] Verifică manual poziția acțiunii în extra-ul header-ului, activarea doar pentru administratori și feedback-ul de
  salvare.

**Dependencies:** 06.

**Files likely touched:**

- `src/Client/src/app/features/class-configuration/header/configuration-header.ts`
- `src/Client/src/app/features/class-configuration/header/configuration-header.html`
- `src/Client/src/app/features/class-configuration/configuration.page.ts`
- `src/Client/src/app/features/class-configuration/configuration.page.html`

**Estimated scope:** Medium (4 files).
