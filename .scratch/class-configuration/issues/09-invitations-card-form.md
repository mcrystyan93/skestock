# 09: Cardul de invitații cu Signal Forms

**What to build:** Extrage numărul de invitații într-o componentă standalone cu `nz-card` și un formular Signal Forms,
conectat la acțiunea comună din header.

**Blocked by:** 08 — Cardul de departamente cu Signal Forms.

**Status:** pending-review

**Labels:** refactor, frontend

## Acceptance criteria

- [ ] Cardul folosește un formular Signal Forms pentru numărul de invitații și păstrează validarea întregului nenegativ
  în intervalul `0`–`2147483647`.
- [ ] Utilizatorii fără permisiune văd valoarea read-only; textul explicativ și ajutorul folosesc `nz-typography` acolo
  unde se potrivește.
- [ ] Formularul nu adaugă o acțiune locală de salvare: butonul din header validează ambele carduri, iar store-ul
  trimite o singură cerere cu numărul și departamentele.

## Verification

- [ ] `cd src/Client && npm run build`
- [ ] Verifică manual valoarea zero, valorile invalide, feedback-ul de eroare/succes, permisiunea read-only și salvarea
  comună cu modificări în ambele carduri.

**Dependencies:** 08.

**Files likely touched:**

- `src/Client/src/app/features/class-configuration/invitations/invitations-card.ts`
- `src/Client/src/app/features/class-configuration/invitations/invitations-card.html`
- `src/Client/src/app/features/class-configuration/configuration.page.ts`
- `src/Client/src/app/features/class-configuration/configuration.page.html`

**Estimated scope:** Medium (4 files).
