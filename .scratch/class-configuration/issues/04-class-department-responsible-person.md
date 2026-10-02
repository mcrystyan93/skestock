# 04: Responsabilul fiecărui departament al clasei

**What to build:** Utilizatorul clasei completează sau modifică responsabilul fiecărui departament direct pe pagina
clasei, independent de celelalte clase și de modelele globale.

**Blocked by:** 03 — Copierea configurării în clase noi și afișarea valorilor.

**Status:** ready-for-agent

**Labels:** ready-for-agent

- [ ] Responsabilul este text liber, opțional, fără selectarea unui cont de utilizator; poate fi completat, schimbat și
  golit.
- [ ] Salvarea și redeschiderea paginii păstrează responsabilul; UI-ul oferă feedback clar și păstrează textul introdus
  dacă salvarea eșuează.
- [ ] Modificarea responsabilului într-o clasă nu afectează modelul global sau departamentul altei clase.
- [ ] Operația permite doar schimbarea responsabilului; numele, responsabilitățile, lista și invitațiile rămân protejate
  pe server.
- [ ] Sunt respectate regulile existente de acces la clase; operația identifică departamentul împreună cu clasa sa și
  respinge o combinație nepotrivită.
- [ ] Testele backend acoperă persistența, ștergerea textului, izolarea între clase, protecția câmpurilor și
  actualizarea citirilor cache-uite.
- [ ] Screenshot-urile includ editarea, responsabili cu text lung, feedback de validare/salvare și starea cu responsabil
  necompletat.
- [ ] Comportamentul este implementat complet prin persistență, Application, API și UI, folosind convențiile existente
  și ADR 0002.
- [ ] Testele backend verifică comportamentul public prin infrastructura existentă; nu se adaugă teste automate
  Playwright.
- [ ] UI-ul respectă sistemul vizual existent și principiile UI/UX: ierarhie clară, etichete vizibile, feedback, focus
  și navigare de la tastatură, acțiuni accesibile și texte lungi lizibile.
- [ ] Sunt păstrate screenshot-uri și un raport de inspecție pentru UI-ul introdus/modificat la 320 și 375 px și la
  pragurile 480, 576, 768, 992, 1200, 1600 și 1920 px, fiecare verificat la prag minus 1, prag și prag plus 1. Se
  verifică și stările relevante la lățimi înguste și largi; se corectează suprapunerile, tăierile și overflow-ul
  orizontal.
- [ ] Build-urile relevante trec; graful proiectului este actualizat după modificările de cod.
