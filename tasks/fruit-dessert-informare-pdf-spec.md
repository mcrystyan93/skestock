# Spec: Sablon „Informare fructe și desert" (descărcare PDF)

Extinde tab-ul „Fructe și desert" descris în `tasks/fruit-dessert-planning-spec.md`. Nu înlocuiește nimic din el.

## Objective

Personalul vrea un document de informare pentru congregația care aduce fructe și desert într-o zi anume. Pe fiecare rând din tabel (o zi) apare o coloană nouă cu un buton „Descarcă PDF". Apăsarea descarcă un PDF cu sablonul „Informare fructe și desert" completat cu datele zilei.

Utilizator: personalul care administrează clasa. Succes: pentru orice zi cu congregație aleasă, un click produce un PDF corect completat, fără copiere manuală.

## Ce există deja (verificat în cod)

- Tabul: `src/Client/src/app/features/school-classes/overview/tabs/fruit-dessert/` cu `table/fruit-dessert-table.html` (7 coloane: Data, Congregația, Persoana de contact, Telefon, Email trimis, Aducere aminte, Mulțumire).
- Șabloane de mesaje în DB: `MessageTemplate` (tipuri `Reminder`, `Thanks`), randare prin `IMessageTemplateRenderer`, editare în `templates-modal`.
- Date necesare: `SchoolClass.Name`, `Congregation.Name`, `Congregation.DessertContactPerson`, `Congregation.DessertContactPhone`, data din `FruitDessertPlanEntry.Date`.
- Export de fișiere: `src/Web/Endpoints/OrderLists.cs` (`ExportOrderList`) întoarce `FileContentHttpResult` prin `ToFile()`.
- **Nu există nicio bibliotecă PDF** în `Directory.Packages.props`, în `.csproj` sau în `src/Client/package.json`.
- SkiaSharp este deja folosit (`OrderListImageExporter`, `SkiaImageThumbnailService`). SkiaSharp poate scrie PDF (`SKDocument.CreatePdf`), fără dependență nouă.

## Decizii propuse (de confirmat)

| # | Propunere | Motiv |
|---|---|---|
| 1 | Un PDF per zi, generat din rândul respectiv (clasă + zi + congregație). | Butonul stă pe rând, deci documentul este al zilei. |
| 2 | Generare server-side cu SkiaSharp (`SKDocument.CreatePdf`). Fără pachet nou. | Respectă „fără dependențe noi" din `CLAUDE.md`. Layout-ul e simplu (o pagină A4, text). |
| 3 | Endpoint `GET /school-classes/{classId}/fruit-dessert-plan/entries/{entryId}/informare.pdf` → `FileContentHttpResult` `application/pdf`. Read-only: nu scrie istoric. | Descărcarea nu trimite nimic, deci nu intră în istoricul de mesaje. |
| 4 | Butonul este dezactivat dacă ziua nu are congregație. Nu este blocat pe clasă `Closed`, pentru că este doar citire. | Nu există ce completa fără congregație. |
| 5 | Sablonul „Informare fructe și desert" este un al treilea `MessageTemplate` cu tip nou `Informare` (append la enum), cu placeholderele `{ContactPerson}`, `{Date}`, `{Congregation}`, `{ClassName}`. Textul se randează cu `IMessageTemplateRenderer`, apoi se pune în PDF. | Refolosește sistemul existent de șabloane și editarea din modal. Dacă textul nu e editabil, vezi întrebarea 1. |

## Întrebări deschise (răspunsul trebuie să vină de la tine)

1. **Ce înseamnă „sablon" exact?**
   - (A) Text editabil din DB, ca Reminder/Thanks, randat în PDF. *Recomandat: reutilizează tot ce există.*
   - (B) Machetă PDF fixă, desenată în cod, cu date completate. Textul se schimbă doar cu modificare de cod.
   - (C) Ai deja un PDF model (cu câmpuri sau fără). Atunci spune-mi unde este și cum arată.
2. **Textul exact** al informării în română (sau un model cu placeholderele tale). Nu îl inventez.
3. **Antet / logo / semnătură** pe PDF? Dacă da, ai fișierul?
4. **Ce date** apar în PDF, în afară de dată, congregație, persoană de contact, telefon și numele clasei? Propun doar acestea.
5. **Numele fișierului**, de exemplu `Informare-fructe-si-desert-2026-11-10.pdf`. Propun acest format.

## Commands

```
dotnet build
dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~FruitDessert"
./run-functional-tests.sh --filter "FullyQualifiedName~FruitDessert"
cd src/Client && npm run build && npm test -- --watch=false
```

Fără migrare, dacă alegi (B) sau (C). Dacă alegi (A) și tipul `Informare` intră în seed, este nevoie de migrare nouă: `dotnet ef migrations add AddInformareTemplate --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext --output-dir Data/Migrations`.

## Project structure

```
src/Application/Features/FruitDessertPlans/Queries/GetInformarePdf/   handler + validator
src/Application/Features/FruitDessertPlans/Abstractions/IInformarePdfRenderer.cs
src/Infrastructure/Documents/InformarePdfRenderer.cs                   SkiaSharp, SKDocument.CreatePdf
src/Web/Endpoints/FruitDessertPlans.cs                                 rută nouă
tests/Application.UnitTests/Features/FruitDessertPlans/
tests/Application.FunctionalTests/Features/FruitDessertPlans/
src/Client/.../tabs/fruit-dessert/table/ + row/                        coloană nouă + buton
src/Client/.../services/fruit-dessert-plans.http.ts                    metodă nouă pentru descărcare blob
```

## Code style

Ca în repo: file-scoped namespaces, `Guard.Against.*`, `Result<T>` pentru erori de business, `CancellationToken` peste tot. Client: `input()`/`output()`, control flow nativ, fără `any`, fără Prettier.

## Testing strategy

- Unit (NUnit/Shouldly/Moq): handler (succes, zi fără congregație, zi inexistentă), randarea placeholderelor, ca în renderer-ul existent.
- Funcțional: endpoint întoarce `200` cu `Content-Type: application/pdf` și începe cu `%PDF`. Zi fără congregație întoarce problem details. Descărcarea nu creează rând în istoric.
- Client (Vitest, colocat): rândul afișează coloana, butonul e dezactivat fără congregație, click apelează `output()` corect.
- Responsive (vezi mai jos).

## Responsive (cerință explicită)

- Coloana nouă are lățime fixă (icon + „PDF" pe mobil, text pe desktop). Tabelul păstrează scroll orizontal în container, ca acum (`nzScroll`).
- Verificare manuală sau browser la lățimile 375, 768, 1024 și 1440 px: butonul rămâne vizibil, nu sparge rândul și nu deformează celelalte coloane.
- Nu se adaugă breakpoint-uri noi în CSS dacă `nz-table` cu scroll le rezolvă deja.

## Boundaries

- **Always:** teste înainte de implementare; `dotnet build` și teste relevante; UI în română; descărcare read-only.
- **Ask first:** orice pachet NuGet nou (inclusiv QuestPDF, dacă alegi o altă bibliotecă); migrare; ecran de editare a sablonului.
- **Never:** scriere în istoricul de mesaje la descărcare; date ale altei congregații în PDF.

## Success criteria

1. Fiecare rând cu congregație are buton „Descarcă PDF" activ; fără congregație, butonul e dezactivat.
2. Apăsarea descarcă un PDF valid, cu textul sablonului completat cu ziua, congregația, contactul și clasa.
3. Descărcarea nu creează intrări în istoricul de mesaje și nu schimbă `EmailSent`.
4. Layout-ul funcționează la 375, 768, 1024 și 1440 px.
5. `dotnet build`, testele unit și funcționale, `npm run build` și `npm test` trec.

## Decizii aprobate

- **Sablon = opțiunea A:** text editabil în DB, tip nou `MessageTemplate` `Informare` (append la enum), randat cu `IMessageTemplateRenderer`, apoi pus în PDF. Editarea se face din `templates-modal` existent. Migrare EF pentru seed-ul tipului (aprobată prin spec).
- **Text:** provizoriu, generic, în română. Textul final se schimbă din modal, fără cod.
- **Layout:** text simplu pe o pagină A4, fără antet, logo sau semnătură.
- **Nume fișier:** `Informare-fructe-si-desert-<yyyy-MM-dd>.pdf`.
- Din spec rămân valabile toate celelalte decizii: read-only, buton dezactivat fără congregație, fără pachete NuGet noi (SkiaSharp).

## Open Questions

Nicio întrebare deschisă.
