# 02: Modele de departamente în configurarea globală

**What to build:** Administratorul definește lista modelelor de departamente pe pagina „Configurare”, cu nume și
responsabilități, și poate adăuga, edita sau șterge modele. Numărul departamentelor rezultă din listă.

**Blocked by:** 01 — Configurare globală: invitații și acces administrativ.

**Status:** ready-for-agent

**Labels:** ready-for-agent

- [ ] Pagina permite creare, editare și ștergere de modele, fără plafon funcțional prestabilit și fără câmp numeric
  separat pentru numărul departamentelor.
- [ ] Numele și responsabilitățile sunt text liber obligatoriu; numele duplicate sunt respinse și UI-ul explică erorile
  lângă câmpurile relevante.
- [ ] Responsabilul nu apare ca proprietate editabilă a modelului global.
- [ ] Lista și numărul de invitații rămân coerente la salvare; modificarea listei nu resetează numărul de invitații.
- [ ] O listă goală poate fi salvată explicit, inclusiv împreună cu zero invitații; redeschiderea restabilește
  configurarea salvată.
- [ ] Toate modificările modelelor sunt protejate de rolul Administrator și validate pe server.
- [ ] Testele backend acoperă CRUD, duplicate, text gol, lista goală, persistență și invalidarea citirilor cache-uite.
- [ ] Inspecția vizuală include departamente cu nume și responsabilități lungi, o listă cu mai multe intrări și starea
  goală.
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
