# Seed admin: note pentru producție (manual, nu automatizat)

Codul nu mai creează `administrator@localhost` / `cosmin@local` cu parole din sursă. Dar conturile deja create în producție rămân în DB, iar parolele vechi sunt în istoricul git.

Pași de făcut manual în producție (după deploy):
1. Setează `SeedAdministratorEmail` / `SeedAdministratorPassword` ca parametri AppHost pentru deploy (vezi secțiunea parametri din AppHost). Seed-ul creează acel admin dacă lipsește; parola unui cont existent NU se schimbă niciodată din config.
2. Verifică conturile existente: `SELECT UserName FROM AspNetUsers`. Pentru `administrator@localhost` și `cosmin@local`:
   - schimbă parola prin flow-ul Identity (reset/schimbare), sau
   - dezactivează contul (lockout) dacă nu mai e folosit.
3. `cosmin@local` nu mai e seed-uit: dacă e un utilizator real, rămâne în DB cu rolul Administrator, dar parola veche trebuie schimbată.
4. Consideră parolele vechi compromise (sunt în istoricul git); rescrierea istoricului nu e necesară dacă parolele sunt rotite.
5. Dev local: `dotnet user-secrets set "Parameters:SeedAdministratorEmail" "<email>" --project src/AppHost` (idem `...Password`); folosește aceleași valori în `PLAYWRIGHT_LOGIN_EMAIL` / `PLAYWRIGHT_LOGIN_PASSWORD`. Baza locală existentă păstrează vechile conturi, deci adminul nou se adaugă lângă ele.
