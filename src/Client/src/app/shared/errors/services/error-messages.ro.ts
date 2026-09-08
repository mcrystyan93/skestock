import { ErrorCodes, ErrorParams } from '@ske/models';

/**
 * A Romanian message entry: either a static phrase or a factory that
 * interpolates backend-supplied `params` (e.g. `maxLength`, `comparisonValue`).
 */
export type RoMessageEntry = string | ((params: ErrorParams) => string);

/** Reads a param as a display string, falling back to an empty string. */
function p(params: ErrorParams, key: string): string {
  const value = params?.[key];
  return value === undefined || value === null ? '' : String(value);
}

/**
 * Romanian copy for every known backend error code.
 *
 * Keyed by the exact wire code (see {@link ErrorCodes}). Factory entries
 * interpolate the camelCase params surfaced by the backend
 * (`maxLength`, `minLength`, `comparisonValue`, `from`, `to`).
 */
export const RO_ERROR_MESSAGES: Readonly<Record<string, RoMessageEntry>> = {
  // ── Common / cross-cutting errors ────────────────────────────────────────
  [ErrorCodes.common.notFound]: 'Resursa solicitată nu a fost găsită.',
  [ErrorCodes.common.unexpected]: 'A apărut o eroare neașteptată. Vă rugăm să încercați din nou.',

  // ── Authentication / authorization errors ────────────────────────────────
  [ErrorCodes.auth.unauthorized]: 'Trebuie să fiți autentificat pentru a efectua această acțiune.',
  [ErrorCodes.auth.forbidden]: 'Nu aveți permisiunea de a efectua această acțiune.',

  // ── Resource / domain errors ─────────────────────────────────────────────
  [ErrorCodes.resource.categoryNotFound]: 'Categoria nu a fost găsită.',
  [ErrorCodes.resource.itemNotFound]: 'Articolul nu a fost găsit.',
  [ErrorCodes.resource.locationNotFound]: 'Locația nu a fost găsită.',
  [ErrorCodes.resource.schoolClassNotFound]: 'Clasa nu a fost găsită.',
  [ErrorCodes.resource.goodsReceiptNotFound]: 'Recepția de marfă nu a fost găsită.',
  [ErrorCodes.resource.goodsReceiptImportNotFound]: 'Importul de recepție nu a fost găsit.',
  [ErrorCodes.resource.goodsReceiptImportNotInReview]:
    'Importul de recepție nu este în așteptarea confirmării și nu poate fi confirmat.',
  [ErrorCodes.resource.fileNotFound]: 'Fișierul nu a fost găsit.',
  [ErrorCodes.resource.blobNotFound]: 'Încărcarea fișierului nu a fost finalizată.',

  // ── Validation: top-level / request-shape codes ──────────────────────────
  [ErrorCodes.validation.failed]: 'Datele trimise nu sunt valide.',
  [ErrorCodes.validation.invalidRequest]: 'Cererea nu este validă.',
  [ErrorCodes.validation.invalidJson]: 'Corpul cererii nu este un JSON valid.',
  [ErrorCodes.validation.invalidType]: 'Tipul valorii trimise nu este corect.',
  [ErrorCodes.validation.invalidFormat]: 'Formatul valorii trimise nu este valid.',
  [ErrorCodes.validation.outOfRange]: 'Valoarea trimisă este în afara intervalului permis.',

  // ── Validation: standard built-in validators ─────────────────────────────
  [ErrorCodes.validation.required]: 'Câmpul este obligatoriu.',
  [ErrorCodes.validation.maxLength]: (params) => {
    const max = p(params, 'maxLength');
    return max
      ? `Valoarea depășește lungimea maximă de ${max} caractere.`
      : 'Valoarea depășește lungimea maximă permisă.';
  },
  [ErrorCodes.validation.minLength]: (params) => {
    const min = p(params, 'minLength');
    return min
      ? `Valoarea trebuie să aibă cel puțin ${min} caractere.`
      : 'Valoarea este prea scurtă.';
  },
  [ErrorCodes.validation.email]: 'Adresa de e-mail nu este validă.',
  [ErrorCodes.validation.invalidEnum]: 'Valoarea selectată nu este validă.',
  [ErrorCodes.validation.greaterThan]: (params) => {
    const value = p(params, 'comparisonValue');
    return value
      ? `Valoarea trebuie să fie mai mare decât ${value}.`
      : 'Valoarea este prea mică.';
  },
  [ErrorCodes.validation.greaterThanOrEqualTo]: (params) => {
    const value = p(params, 'comparisonValue');
    return value
      ? `Valoarea trebuie să fie mai mare sau egală cu ${value}.`
      : 'Valoarea este prea mică.';
  },
  [ErrorCodes.validation.lessThan]: (params) => {
    const value = p(params, 'comparisonValue');
    return value
      ? `Valoarea trebuie să fie mai mică decât ${value}.`
      : 'Valoarea este prea mare.';
  },
  [ErrorCodes.validation.lessThanOrEqualTo]: (params) => {
    const value = p(params, 'comparisonValue');
    return value
      ? `Valoarea trebuie să fie mai mică sau egală cu ${value}.`
      : 'Valoarea este prea mare.';
  },
  [ErrorCodes.validation.between]: (params) => {
    const from = p(params, 'from');
    const to = p(params, 'to');
    return from && to
      ? `Valoarea trebuie să fie între ${from} și ${to}.`
      : 'Valoarea este în afara intervalului permis.';
  },

  // ── Validation: pagination / query codes ─────────────────────────────────
  [ErrorCodes.validation.invalidSortKey]: 'Criteriul de sortare nu este valid.',
  [ErrorCodes.validation.invalidSortDirection]: 'Direcția de sortare nu este validă.',
  [ErrorCodes.validation.invalidCursor]: 'Cursorul de paginare nu este valid.',
  [ErrorCodes.validation.cursorSortMismatch]:
    'Cursorul de paginare nu corespunde criteriului de sortare selectat.',

  // ── Validation: domain-specific uniqueness codes ─────────────────────────
  [ErrorCodes.validation.duplicateName]: 'Există deja o înregistrare cu acest nume.',
  [ErrorCodes.validation.duplicateSku]: 'Există deja un articol cu acest cod SKU.',

  // ── Validation: domain-specific reference codes ──────────────────────────
  [ErrorCodes.validation.invalidReference]: 'Referința selectată nu există.',
  [ErrorCodes.validation.selfReference]: 'O înregistrare nu se poate referi la ea însăși.',
  [ErrorCodes.validation.circularReference]:
    'Modificarea ar crea o referință circulară și nu este permisă.',
  [ErrorCodes.validation.invalidDateRange]:
    'Intervalul de date nu este valid: data de început trebuie să fie înainte de data de sfârșit.',

  // ── Validation: goods receipt codes ──────────────────────────────────────
  [ErrorCodes.validation.emptyLines]: 'Recepția trebuie să conțină cel puțin o linie.',
  [ErrorCodes.validation.duplicateReceiptLine]:
    'Există linii duplicate pentru aceeași combinație de articol, locație și dată de expirare.',
  [ErrorCodes.validation.expiryDateRequired]:
    'Data de expirare este obligatorie pentru articolele perisabile.',
  [ErrorCodes.validation.expiryDateNotAllowed]:
    'Data de expirare nu este permisă pentru articolele neperisabile.',
  [ErrorCodes.validation.splitQuantityMismatch]:
    'Cantitățile împărțite nu corespund cantității inițiale a liniei.',

  // ── Validation: stock adjustment codes ───────────────────────────────────
  [ErrorCodes.validation.noAdjustmentNeeded]:
    'Cantitatea numărată este egală cu stocul curent; nu este necesară nicio ajustare.',

  // ── Fallback ─────────────────────────────────────────────────────────────
  [ErrorCodes.validation.unknown]: 'A apărut o eroare de validare.',
};

/** Generic Romanian fallback used when a code has no dedicated phrase. */
export const RO_DEFAULT_ERROR_MESSAGE = 'A apărut o eroare neașteptată.';
