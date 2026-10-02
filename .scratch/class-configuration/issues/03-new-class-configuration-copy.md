# 03: Copierea configurării în clase noi și afișarea valorilor

**What to build:** La crearea unei clase, aceasta primește automat copii ale modelelor de departamente și numărului de
invitații. Pagina clasei afișează aceste valori, care rămân stabile după schimbarea configurării globale.

**Blocked by:** 02 — Modele de departamente în configurarea globală.

**Status:** ready-for-agent

**Labels:** ready-for-agent

- [ ] Crearea unei clase înainte de prima salvare explicită a configurării este respinsă de server și UI-ul arată
  motivul.
- [ ] Crearea copiază atomic numele și responsabilitățile tuturor modelelor și numărul de invitații din configurarea
  salvată; responsabilii sunt inițial goi.
- [ ] Pagina clasei afișează departamentele și numărul de invitații ca valori proprii clasei; numele,
  responsabilitățile, lista și invitațiile nu pot fi modificate prin UI sau operațiile publice ale clasei.
- [ ] Clasa A cu 30 invitații își păstrează valoarea după schimbarea globală la 40; clasa B creată ulterior primește 40.
- [ ] Editarea sau ștergerea unui model global nu schimbă și nu șterge departamentul copiat în A; B primește modelele
  actualizate.
- [ ] O configurare salvată cu lista goală și zero invitații este copiată valid și marcată explicit ca preluată.
- [ ] Evoluția schemei păstrează datele claselor existente, care rămân utilizabile și distinct marcate ca
  neinițializate; nu sunt populate automat.
- [ ] Testele backend acoperă blocarea înainte de salvare, copii persistente, independența față de schimbările globale,
  valorile goale, protejarea câmpurilor și citirile după popularea cache-ului.
- [ ] UI-ul distinge lista goală a unei clase inițializate de o clasă veche încă neinițializată.
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
