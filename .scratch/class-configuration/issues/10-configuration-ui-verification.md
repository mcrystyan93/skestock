# 10: Verificarea UI și actualizarea graphify

**What to build:** Verifică build-ul și experiența integrată a paginii în browser, păstrează dovezile vizuale și
actualizează graful repo-ului după modificarea codului.

**Blocked by:** 09 — Cardul de invitații cu Signal Forms.

**Status:** pending-review

**Labels:** refactor, frontend, verification

## Acceptance criteria

- [ ] Pagina trece build-ul clientului, iar `graphify update .` actualizează graful după modificările de cod.
- [ ] Screenshot-urile acoperă 320 și 375 px și fiecare prag 480, 576, 768, 992, 1200, 1600, 1920 px la `prag - 1`,
  `prag` și `prag + 1`; se verifică stările populate și reprezentative de validare/read-only.
- [ ] Inspecția confirmă navigarea cu tastatura, focus vizibil, feedback-ul formularelor, butonul comun din header,
  împachetarea textului și lipsa overflow-ului orizontal; raportul și screenshot-urile sunt păstrate în
  `.scratch/class-configuration/verification/`.

## Verification

- [ ] `cd src/Client && npm run build`
- [ ] `graphify update .`
- [ ] Inspecție manuală în browser la toate dimensiunile de mai sus; salvează imaginile într-un singur fișier ZIP și
  completează raportul cu viewports, stările, concluziile și eventualele probleme rămase.

**Dependencies:** 09.

**Files likely touched:**

- `.scratch/class-configuration/verification/configuration-ui-refactor.md`
- `.scratch/class-configuration/verification/configuration-ui-refactor-screenshots.zip`
- `graphify-out/` (fișiere generate de comanda de actualizare)

**Estimated scope:** Medium (artefacte de verificare și output graphify).
