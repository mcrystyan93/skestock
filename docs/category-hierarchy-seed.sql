-- Groups the existing categories under 14 parent categories (Categories.ParentId, migration AddCategoryParent).
-- Run manually against the target database, once the migration is applied. Re-runnable: parents are created
-- only if missing, and only categories that still have no parent are assigned.
-- The category list is cached by the app (HybridCache): restart the Web app or wait for expiry to see the result.
-- "Produse pentru animale de companie" is left without a parent on purpose.

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @Map TABLE (Parent nvarchar(100) NOT NULL, Child nvarchar(100) NOT NULL);

INSERT INTO @Map (Parent, Child) VALUES
(N'Lactate și ouă', N'Lapte'),
(N'Lactate și ouă', N'Lapte fără lactoză'),
(N'Lactate și ouă', N'Iaurt'),
(N'Lactate și ouă', N'Produse lactate fermentate (chefir, sana)'),
(N'Lactate și ouă', N'Smântână/frișcă'),
(N'Lactate și ouă', N'Unt'),
(N'Lactate și ouă', N'Brânză (cașcaval, telemea, brânză proaspătă)'),
(N'Lactate și ouă', N'Ouă'),

(N'Carne și mezeluri', N'Carne de curcan'),
(N'Carne și mezeluri', N'Carne de miel'),
(N'Carne și mezeluri', N'Carne de porc'),
(N'Carne și mezeluri', N'Carne de pui'),
(N'Carne și mezeluri', N'Carne de vită'),
(N'Carne și mezeluri', N'Carne tocată'),
(N'Carne și mezeluri', N'Mezeluri (șuncă, salam, cârnați)'),
(N'Carne și mezeluri', N'Conserve carne'),

(N'Pește și fructe de mare', N'Pește proaspăt'),
(N'Pește și fructe de mare', N'Pește afumat/sărat'),
(N'Pește și fructe de mare', N'Fructe de mare'),
(N'Pește și fructe de mare', N'Conserve pește'),

(N'Legume și verdețuri', N'Cartofi'),
(N'Legume și verdețuri', N'Ceapă, usturoi, praz'),
(N'Legume și verdețuri', N'Ciuperci'),
(N'Legume și verdețuri', N'Legume cu frunze (salată, spanac)'),
(N'Legume și verdețuri', N'Legume rădăcinoase (morcov, sfeclă)'),
(N'Legume și verdețuri', N'Roșii, ardei, castraveți'),
(N'Legume și verdețuri', N'Verdețuri și ierburi aromatice'),

(N'Fructe', N'Citrice'),
(N'Fructe', N'Fructe autohtone (mere, pere)'),
(N'Fructe', N'Fructe exotice (banane, kiwi)'),

(N'Conserve și muraturi', N'Conserve legume'),
(N'Conserve și muraturi', N'Muraturi'),
(N'Conserve și muraturi', N'Oțet'),

(N'Produse congelate', N'Aluaturi congelate'),
(N'Produse congelate', N'Cartofi congelați (pommes frites)'),
(N'Produse congelate', N'Deserturi congelate'),
(N'Produse congelate', N'Fructe congelate'),
(N'Produse congelate', N'Legume congelate'),
(N'Produse congelate', N'Pește/fructe de mare congelate'),
(N'Produse congelate', N'Pizza congelată'),
(N'Produse congelate', N'Semipreparate congelate (chiftele, șnițele)'),
(N'Produse congelate', N'Înghețată'),

(N'Panificație și patiserie', N'Pâine albă'),
(N'Panificație și patiserie', N'Pâine integrală/specială'),
(N'Panificație și patiserie', N'Covrigi/lipii'),
(N'Panificație și patiserie', N'Produse de patiserie'),
(N'Panificație și patiserie', N'Prăjituri/torturi'),

(N'Cereale, paste și făinoase', N'Cereale de mic dejun'),
(N'Cereale, paste și făinoase', N'Cereale/leguminoase uscate (linte, fasole)'),
(N'Cereale, paste și făinoase', N'Leguminoase uscate (linte, fasole)'),
(N'Cereale, paste și făinoase', N'Orez'),
(N'Cereale, paste și făinoase', N'Paste făinoase'),
(N'Cereale, paste și făinoase', N'Făină'),
(N'Cereale, paste și făinoase', N'Batoane de cereale'),

(N'Băcănie și condimente', N'Condimente și mirodenii'),
(N'Băcănie și condimente', N'Ulei de gătit'),
(N'Băcănie și condimente', N'Zahăr'),
(N'Băcănie și condimente', N'Îndulcitori'),
(N'Băcănie și condimente', N'Miere și gemuri'),
(N'Băcănie și condimente', N'Sosuri (ketchup, maioneză, muștar)'),
(N'Băcănie și condimente', N'Tartinabile'),
(N'Băcănie și condimente', N'Nuci și semințe'),

(N'Dulciuri și snacks', N'Biscuiți'),
(N'Dulciuri și snacks', N'Ciocolată'),
(N'Dulciuri și snacks', N'Chipsuri'),

(N'Băuturi', N'Apă plată'),
(N'Băuturi', N'Apă minerală'),
(N'Băuturi', N'Băuturi carbogazoase'),
(N'Băuturi', N'Sucuri'),
(N'Băuturi', N'Sucuri naturale'),
(N'Băuturi', N'Cafea boabe/măcinată'),
(N'Băuturi', N'Cafea instant'),
(N'Băuturi', N'Ceai'),

(N'Curățenie și consumabile casă', N'Detergent de rufe'),
(N'Curățenie și consumabile casă', N'Detergent de vase'),
(N'Curățenie și consumabile casă', N'Balsam de rufe'),
(N'Curățenie și consumabile casă', N'Produse de curățat baie/WC'),
(N'Curățenie și consumabile casă', N'Produse de curățat geamuri'),
(N'Curățenie și consumabile casă', N'Produse de curățat universale'),
(N'Curățenie și consumabile casă', N'Pungi de gunoi'),
(N'Curățenie și consumabile casă', N'Șervețele de bucătărie/hârtie igienică'),
(N'Curățenie și consumabile casă', N'Consumabile pentru casă'),
(N'Curățenie și consumabile casă', N'Baterii/consumabile pentru casă'),
(N'Curățenie și consumabile casă', N'Aspirator'),

(N'Igienă personală', N'Gel de duș'),
(N'Igienă personală', N'Șampon'),
(N'Igienă personală', N'Balsam de păr'),
(N'Igienă personală', N'Deodorant'),
(N'Igienă personală', N'Pastă de dinți'),
(N'Igienă personală', N'Periuțe de dinți'),
(N'Igienă personală', N'Săpun lichid'),
(N'Igienă personală', N'Săpun solid'),
(N'Igienă personală', N'Produse cosmetice de bază (creme, loțiuni)');

-- A child name that does not exist (renamed since this script was written) aborts the run instead of
-- silently leaving it ungrouped.
DECLARE @Missing nvarchar(max) = (
    SELECT STRING_AGG(m.Child, N'; ')
    FROM @Map m
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories c WHERE c.Name = m.Child));
IF @Missing IS NOT NULL
BEGIN
    DECLARE @Message nvarchar(max) = CONCAT(N'Categories not found: ', @Missing);
    THROW 50000, @Message, 1;
END;

INSERT INTO dbo.Categories (Id, Name, CreatedDate, LastModifiedDate)
SELECT NEWID(), p.Parent, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()
FROM (SELECT DISTINCT Parent FROM @Map) p
WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories c WHERE c.Name = p.Parent);

UPDATE child
SET child.ParentId = parent.Id,
    child.LastModifiedDate = SYSDATETIMEOFFSET()
FROM dbo.Categories child
JOIN @Map m ON m.Child = child.Name
JOIN dbo.Categories parent ON parent.Name = m.Parent
WHERE child.ParentId IS NULL
  AND child.Id <> parent.Id;

COMMIT TRANSACTION;

-- Check: parents with their number of children.
SELECT p.Name AS Parent, COUNT(c.Id) AS Children
FROM dbo.Categories p
LEFT JOIN dbo.Categories c ON c.ParentId = p.Id
WHERE p.ParentId IS NULL
GROUP BY p.Name
HAVING COUNT(c.Id) > 0
ORDER BY p.Name;
