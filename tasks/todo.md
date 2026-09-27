# Sarcini: graficul consumului pe perioade

## Sarcina 1: Gruparea si media pe perioade

**Acceptare:**
- [x] Seria zilnica ramane neschimbata; saptamanile incep luni, iar lunile la
      data de 1; punctele pastreaza cantitatea si valoarea.
- [x] Zilele fara consum si perioadele partiale intra in medie; seria goala nu
      produce impartire la zero.

**Verificare:** `cd src/Client && npm test -- --watch=false` cu testele
transformarii pentru luni/duminica, an nou, luna cu numar variabil de zile,
zero-uri si intervale partiale.

**Dependente:** Niciuna.
**Fisiere probabile:** modul de grupare langa grafic; testul acestuia.
**Dimensiune:** mica (2 fisiere).

## Checkpoint dupa sarcina 1

- [x] Testele de grupare trec si media este concordanta cu sumele si numarul de
      intervale afisate.

## Sarcina 2: Selector si grafic reactiv

**Acceptare:**
- [x] Selector accesibil Zilnic/Saptamanal/Lunar; Zilnic implicit, filtrele si
      datele incarcate raman neschimbate la comutare.
- [x] Barele, media cantitatii si valorii, linia mediei, tooltipul, etichetele,
      mesajul gol si descrierea accesibila reflecta perioada selectata.

**Verificare:** teste Vitest ale interactiunii in client si
`cd src/Client && npm run build`.

**Dependente:** Sarcina 1.
**Fisiere probabile:** cardul si graficul consumului, eventual testul
componentei.
**Dimensiune:** medie (2-3 fisiere).

## Checkpoint final

- [x] Build-ul si testele clientului trec; criteriile din spec sunt indeplinite.
- [x] Revizuire cu utilizatorul inainte de a considera functionalitatea incheiata.
