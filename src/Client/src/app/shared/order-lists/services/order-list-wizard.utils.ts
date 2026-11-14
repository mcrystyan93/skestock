import { OrderListLineDto, SupplyListDto } from '@ske/models';

export const DEFAULT_UNIT = 'buc';

export function toProductName(name: string, sku?: string | null): string {
  return sku ? `(${sku}) ${name}` : name;
}

/** Average quantities are fractional; order whole units, never below 1. */
export function wholeQuantity(quantity: number): number {
  return Math.max(1, Math.floor(quantity));
}

export const MAX_ORDER_LIST_NAME_LENGTH = 200;
export const MAX_ORDER_LIST_NOTE_LENGTH = 1000;
const MAX_LINE_NOTES_LENGTH = 1000;

const NOTES_SEPARATOR = ' · ';

/** Joins whole notes up to the length limit, so no note is cut in the middle. */
function joinNotes(notes: Iterable<string>): string {
  let joined = '';

  for (const note of notes) {
    const next = joined ? joined + NOTES_SEPARATOR + note : note;

    if (next.length > MAX_LINE_NOTES_LENGTH)
      return joined || note.slice(0, MAX_LINE_NOTES_LENGTH);

    joined = next;
  }

  return joined;
}

/** Merges lines of several supply lists by item: quantities add up, distinct notes are joined. */
export function supplyListsToLines(supplyLists: SupplyListDto[]): OrderListLineDto[] {
  const byItem = new Map<string, { line: OrderListLineDto; notes: Set<string> }>();

  for (const supplyList of supplyLists) {
    for (const line of supplyList.lines) {
      const note = line.notes?.trim();
      const existing = byItem.get(line.itemId);

      if (!existing) {
        byItem.set(line.itemId, {
          line: {
            id: null,
            itemId: line.itemId,
            itemImageFileId: line.itemImageFileId ?? null,
            productName: toProductName(line.itemName, line.itemSku),
            quantity: line.quantity,
            unit: line.unit || DEFAULT_UNIT,
            notes: ''
          },
          notes: new Set(note ? [note] : [])
        });
        continue;
      }

      existing.line.quantity += line.quantity;

      if (note)
        existing.notes.add(note);
    }
  }

  return [...byItem.values()].map(({ line, notes }) => ({ ...line, notes: joinNotes(notes) }));
}

export type ReviewedProduct = {
  itemId: string;
  productName: string;
  imageFileId: string | null;
  quantity: number;
  unit: string;
  listNames: string[];
};

/** The merged products the order would get, with the supply lists each one comes from. */
export function supplyListsToProducts(supplyLists: SupplyListDto[]): ReviewedProduct[] {
  const listNames = new Map<string, Set<string>>();

  for (const list of supplyLists)
    for (const line of list.lines)
      listNames.set(line.itemId, (listNames.get(line.itemId) ?? new Set()).add(list.name));

  return supplyListsToLines(supplyLists).map((line) => ({
    itemId: line.itemId ?? '',
    productName: line.productName,
    imageFileId: line.itemImageFileId ?? null,
    quantity: line.quantity,
    unit: line.unit,
    listNames: [...(listNames.get(line.itemId ?? '') ?? [])]
  }));
}

export function supplyListsToName(supplyLists: SupplyListDto[]): string | null {
  if (supplyLists.length === 0)
    return null;

  return supplyLists.map((list) => list.name).join(' + ').slice(0, MAX_ORDER_LIST_NAME_LENGTH);
}
