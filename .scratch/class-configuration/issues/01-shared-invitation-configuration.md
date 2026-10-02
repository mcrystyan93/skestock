# 01: Configurare globală: invitații și acces administrativ

**What to build:** Administratorul deschide noua pagină „Configurare”, salvează explicit numărul global de invitații și
îl regăsește la redeschidere. Pagina diferențiază configurarea nesalvată de configurarea salvată cu valoarea zero.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

**Labels:** ready-for-agent

- [ ] Elementul „Configurare” este integrat în navigarea existentă pentru ecrane mici și mari; pagina folosește un
  formular clar și o acțiune principală de salvare.
- [ ] Numărul de invitații este un int nenegativ; zero este valid, iar valori negative, fracționare și în afara
  intervalului sunt respinse de server, cu feedback clar în UI.
- [ ] Configurarea este absentă/nesalvată până la prima salvare explicită; o salvare cu zero o marchează drept salvată
  și persistă după redeschidere.
- [ ] Doar Administrator poate modifica valoarea; cererile directe ale unui utilizator obișnuit sunt respinse chiar dacă
  ocolește UI-ul.
- [ ] Pagina gestionează încărcarea, erorile, succesul și păstrarea valorilor introduse când salvarea eșuează.
- [ ] Citirile după salvare reflectă noua valoare inclusiv după popularea cache-ului; testele acoperă persistența,
  validarea și accesul.
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
