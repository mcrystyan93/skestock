# 08: Cardul de departamente cu Signal Forms

**What to build:** Extrage secțiunea de departamente într-o componentă standalone cu `nz-card`, formular Signal Forms și
acțiunea de adăugare în extra-ul cardului.

**Blocked by:** 07 — Header ng-zorro și salvarea comună.

**Status:** pending-review

**Labels:** refactor, frontend

## Acceptance criteria

- [ ] Cardul folosește un formular Signal Forms pentru nume și responsabilități; păstrează cerințele, limita de 100 de
  caractere, unicitatea numelui și mesajele lângă câmpurile relevante.
- [ ] „Adaugă departament” apare în `nz-card-extra`; add/remove/edit funcționează și lista goală are mesajul existent.
- [ ] Utilizatorii fără permisiune văd lista read-only; la salvarea comună formularul marchează erorile și raportează
  dacă departamentele sunt valide, fără a trimite un request separat.

## Verification

- [ ] `cd src/Client && npm run build`
- [ ] Verifică manual adăugarea, editarea, ștergerea, câmpurile invalide, numele duplicate și afișarea read-only.

**Dependencies:** 06, 07.

**Files likely touched:**

- `src/Client/src/app/features/class-configuration/departments/departments-card.ts`
- `src/Client/src/app/features/class-configuration/departments/departments-card.html`
- `src/Client/src/app/features/class-configuration/configuration.page.ts`
- `src/Client/src/app/features/class-configuration/configuration.page.html`

**Estimated scope:** Medium (4 files).
