# 05: Preluarea unică a configurării pentru clase existente

**What to build:** O clasă existentă neinițializată poate prelua explicit configurarea globală curentă o singură dată.
După preluare afișează aceleași copii stabile ca o clasă nouă.

**Blocked by:** 03 — Copierea configurării în clase noi și afișarea valorilor.

**Status:** ready-for-agent

**Labels:** ready-for-agent

- [ ] Pagina clasei neinițializate oferă o acțiune explicită și explică faptul că preluarea este unică și copiază
  departamentele și numărul de invitații.
- [ ] Preluarea necesită configurare globală salvată; absența ei produce un mesaj clar și nu marchează clasa ca
  inițializată.
- [ ] Preluarea persistă atomic modelele curente, responsabili inițial goi și invitațiile curente, fără a afecta datele
  existente ale clasei.
- [ ] O preluare cu lista goală și zero invitații este finalizată și nu poate fi repetată.
- [ ] Acțiunea nu mai este disponibilă după preluare; serverul respinge repetarea și nu înlocuiește valorile copiate sau
  responsabilii deja completați.
- [ ] Cererile concurente nu produc copii duplicate și nu înlocuiesc prima preluare; o eroare înainte de finalizare
  permite reîncercare fără copie parțială.
- [ ] Modificările globale ulterioare nu afectează clasa; pagina afișează imediat valorile preluate inclusiv dacă
  existau citiri cache-uite.
- [ ] Testele backend acoperă preluarea, condiția salvării globale, repetarea/concurența, valorile goale și păstrarea
  copiei.
- [ ] Inspecția vizuală include starea înainte de preluare, indisponibilitatea configurării, preluarea în curs, erorile
  și clasa după preluare.
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
