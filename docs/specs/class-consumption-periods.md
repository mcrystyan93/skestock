# Spec: Consum pe zi, saptamana si luna in graficul clasei

## Obiectiv

In tabul de statistici al unei clase, utilizatorul poate compara consumul de
stoc pe zile, saptamani si luni, folosind acelasi grafic si aceleasi filtre
pentru articol, locatie si categorie. Segmentul de selectie are trei optiuni:
Zilnic, Saptamanal, Lunar; Zilnic ramane optiunea initiala.

## Criterii de acceptare

- Zilnic pastreaza seria, totalurile si media existente; schimbarea segmentului
  nu reseteaza filtrele.
- Saptamanal grupeaza zilele dupa saptamani calendaristice locale,
  luni-duminica; Lunar grupeaza dupa luni calendaristice locale. Un punct
  reprezinta suma cantitatilor si a valorilor din zilele sale.
- Seria porneste de la prima tranzactie a clasei si se termina la data deja
  folosita de graficul zilnic (astazi sau sfarsitul clasei, cu exceptiile deja
  existente pentru consum in afara intervalului). Perioadele fara consum sunt
  incluse cu zero. Prima si ultima perioada pot fi incomplete si sunt incluse
  atat in grafic, cat si in media perioadelor.
- Pentru granularitatea selectata se afiseaza media cantitatii pe perioada si
  media valorii in RON pe perioada, calculate ca totalul / numarul de perioade
  afisate, cu rotunjirea existenta la doua zecimale. Linia mediei foloseste
  aceeasi cantitate si se actualizeaza odata cu segmentul.
- Etichetele, tooltipul, unitatea mediei si descrierea accesibila disting clar
  intre zi, saptamana si luna; tooltipul pastreaza cantitatea si valoarea
  intervalului. Selectia este operabila de la tastatura.
- Fara tranzactii sau fara consum pentru filtrele selectate, graficul continua
  sa afiseze mesajul gol corespunzator, fara impartire la zero.

## Stack si comenzi

- Backend: .NET 10, Application/Mediator si teste NUnit/Shouldly.
- Client: Angular 22, ng-zorro, ApexCharts si teste Vitest.
- Build backend: `dotnet build src/Application/Application.csproj`
- Test backend tintit: `dotnet test tests/Application.UnitTests --filter FullyQualifiedName~GetClassDailyConsumption`
- Build client: `cd src/Client && npm run build`
- Test client: `cd src/Client && npm test -- --watch=false`

## Structura proiectului

- `src/Application/Features/Statistics/` contine interogarea si DTO-urile
  pentru consumul unei clase.
- `src/Web/Endpoints/Statistics.cs` expune interogarea catre client.
- `src/Client/src/app/features/school-classes/overview/tabs/statistics/`
  contine cardul, selectorul si graficul.
- `src/Client/src/app/core/models/` contine tipurile contractului HTTP.
- `tests/Application.UnitTests/Features/Statistics/Queries/GetClassDailyConsumption/`
  contine testele pentru seria consumului.

## Stil si testare

Se pastreaza stilul existent: namespace-uri C# file-scoped, `DateOnly` pentru
zile, serii imutabile si `computed()` pentru date derivate in Angular. Exemplu:

```typescript
readonly selectedPeriod = signal<'daily' | 'weekly' | 'monthly'>('daily');
```

Testele acopera perioade cu zero consum, granita luni/duminica, trecerea
anului, luni de lungimi diferite, perioade partiale si mediile aferente.
Testele clientului acopera comutarea intre perioade cu filtrele pastrate si
afisarea corecta a mediei, a etichetelor si a tooltipului.

## Limite

- Intotdeauna: se reutilizeaza seria zilnica si se respecta fusul orar
  Europe/Bucharest si filtrele existente.
- Se cere aprobare inainte de schimbarea schemei bazei de date, a contractului
  public sau de adaugarea unei dependinte.
- Nu se modifica sumarul separat cu mediile ultimelor 7/30 zile si nu se
  schimba semantica datelor zilnice.

## Intrebari deschise

Niciuna. Intervalele calendaristice si includerea celor incomplete au fost
confirmate.
