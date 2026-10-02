# Spec: Configurare departamente și invitații pentru clase

## Obiectiv

O pagină nouă, accesibilă din elementul de meniu „Configurare”, permite definirea modelelor de departamente și a
numărului de invitații pentru clase. Fiecare clasă păstrează o copie a configurării preluate și permite completarea
responsabilului fiecărui departament.

## Configurare globală

- Modificarea configurării este permisă doar administratorilor.
- Lista permite adăugarea, editarea și ștergerea modelelor de departamente.
- Fiecare model are nume și responsabilități, ambele text liber și obligatorii.
- Numele sunt unice în lista globală.
- Numărul departamentelor rezultă din lista definită; nu există un câmp numeric separat.
- Utilizatorul poate adăuga câte departamente are nevoie, fără un plafon funcțional prestabilit.
- Responsabilul este completat în clasă, nu în modelul global.
- Numărul de invitații este un întreg nenegativ de tip `int`; `0` este permis.
- Configurarea trebuie salvată explicit înainte de crearea claselor noi.
- Se poate salva explicit o configurare cu lista de departamente goală și `0` invitații.

## Clase noi

- La creare, clasa copiază automat lista modelelor, cu numele și responsabilitățile, precum și numărul de invitații din
  configurarea globală salvată.
- Responsabilul fiecărui departament este inițial gol și poate rămâne necompletat.
- Pagina clasei afișează departamentele și permite editarea responsabilului ca text liber, fără asociere cu un cont de
  utilizator.
- În clasă se poate modifica doar responsabilul departamentului; numele, responsabilitățile și lista departamentelor
  copiate nu sunt editabile.
- Numărul de invitații copiat este afișat și nu poate fi modificat manual în clasă.
- Editarea sau ștergerea modelelor globale nu modifică departamentele deja copiate.
- Schimbarea numărului global de invitații nu modifică valoarea claselor existente.

## Clase existente

- Clasele existente primesc o acțiune explicită de preluare a configurării.
- Preluarea copiază atât departamentele, cât și numărul de invitații din configurarea globală salvată la momentul
  acțiunii.
- Preluarea este permisă o singură dată pentru fiecare clasă existentă.
- O preluare cu lista goală și `0` invitații este tot o preluare finalizată.
- Preluarea nu poate fi repetată pentru a înlocui valorile inițiale sau responsabilii.
- Configurarea globală trebuie să fie salvată înainte de preluare.

## Criterii de acceptare

1. Administratorul poate salva, redeschide și gestiona lista modelelor și numărul de invitații.
2. Un utilizator fără rol de administrator nu poate modifica configurarea globală.
3. Sunt respinse numele duplicate, numele sau responsabilitățile goale și numerele negative de invitații.
4. Crearea unei clase înainte de salvarea explicită a configurării este respinsă cu un mesaj clar.
5. O clasă nouă primește configurarea salvată, cu responsabili inițial goi.
6. Responsabilul completat pentru un departament este păstrat la redeschiderea clasei.
7. Dacă o clasă A a copiat `30` invitații, schimbarea globală la `40` păstrează `30`
   în A, iar o clasă B creată ulterior primește `40`.
8. Modificarea sau ștergerea unui model global păstrează copia din A, iar B primește lista actualizată.
9. O clasă existentă poate prelua configurarea o singură dată, inclusiv o configurare goală.
10. Valorile copiate sunt protejate împotriva modificării directe, cu excepția responsabilului.

## Decizii asociate

- [Glosar](../../CONTEXT.md)
- [ADR 0002: Păstrarea configurării copiate în clase](../adr/0002-class-configuration-snapshots.md)

## Stare

Cerințele de mai sus au fost convenite în sesiunea `grill-with-docs` din 2026-10-02. Documentul descrie funcționalitatea
agreată; implementarea urmează separat.

Specificația pentru implementare este publicată în
[trackerul local](../../.scratch/class-configuration/spec.md), cu eticheta `ready-for-agent`. Verificarea agreată
include teste backend și inspecție vizuală cu screenshot-uri în browser la toate breakpoint-urile proiectului, inclusiv
imediat sub și peste praguri, plus evaluare UI/UX. Testele automate Playwright pentru această funcționalitate sunt
amânate.
