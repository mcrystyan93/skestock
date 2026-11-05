import { LowStockItemDto, OrderListLineDto, PurchaseSegmentItemDto, PurchaseSegmentsDto, SupplyListDto } from '@ske/models';

export type SuggestedItem = {
  itemId: string;
  itemName: string;
  sku: string | null;
  imageFileId: string | null;
  unit: string;
  locations: string[];
};

/** The same item can be low on stock in several locations; suggest it only once. */
export function groupLowStockItems(items: LowStockItemDto[]): SuggestedItem[] {
  const byItem = new Map<string, SuggestedItem>();

  for (const item of items) {
    const existing = byItem.get(item.itemId);

    if (!existing) {
      byItem.set(item.itemId, {
        itemId: item.itemId,
        itemName: item.itemName,
        sku: item.sku ?? null,
        imageFileId: item.imageFileId ?? null,
        unit: item.unit,
        locations: [item.locationName]
      });
    } else if (!existing.locations.includes(item.locationName)) {
      existing.locations.push(item.locationName);
    }
  }

  return [...byItem.values()]
    .map((item) => ({ ...item, locations: [...item.locations].sort((a, b) => a.localeCompare(b)) }))
    .sort((a, b) => a.itemName.localeCompare(b.itemName));
}

const DEFAULT_UNIT = 'buc';

function toProductName(name: string, sku?: string | null): string {
  return sku ? `(${sku}) ${name}` : name;
}

/** Average quantities are fractional; order whole units, never below 1. */
export function wholeQuantity(quantity: number): number {
  return Math.max(1, Math.floor(quantity));
}

/** The four purchase segments in display order. */
export function purchaseSegmentLists(segments: PurchaseSegmentsDto | null): PurchaseSegmentItemDto[][] {
  return segments ? [segments.weekly, segments.everyTwoWeeks, segments.monthly, segments.oncePerClass] : [];
}

/** Order lines for the ticked items; a purchase segment's average quantity wins over the low-stock default of 1. */
export function suggestionsToLines(
  items: SuggestedItem[],
  segments: PurchaseSegmentsDto | null,
  selectedIds: readonly string[]
): OrderListLineDto[] {
  const selected = new Set(selectedIds);
  type Candidate = { name: string; sku: string | null; imageFileId: string | null; unit: string; quantity: number };
  const candidates = new Map<string, Candidate>();

  for (const item of items)
    candidates.set(item.itemId, { name: item.itemName, sku: item.sku, imageFileId: item.imageFileId, unit: item.unit, quantity: 1 });

  // An item in several segments or also in low stock is overwritten, so the segment quantity replaces the default 1.
  for (const item of purchaseSegmentLists(segments).flat())
    candidates.set(item.itemId, { name: item.name, sku: item.sku, imageFileId: item.imageFileId, unit: item.unit, quantity: wholeQuantity(item.averageQuantity) });

  return [...candidates]
    .filter(([itemId]) => selected.has(itemId))
    .map(([itemId, item]) => ({
      id: null,
      itemId,
      itemImageFileId: item.imageFileId,
      productName: toProductName(item.name, item.sku),
      quantity: item.quantity,
      unit: item.unit || DEFAULT_UNIT,
      notes: ''
    }));
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
