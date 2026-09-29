import { LowStockItemDto, OrderListLineDto, SupplyListDto } from '@ske/models';

export type SuggestedItem = {
  itemId: string;
  itemName: string;
  sku: string | null;
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

export function suggestionsToLines(items: SuggestedItem[], selectedIds: readonly string[]): OrderListLineDto[] {
  const selected = new Set(selectedIds);

  return items
    .filter((item) => selected.has(item.itemId))
    .map((item) => ({
      id: null,
      itemId: item.itemId,
      productName: toProductName(item.itemName, item.sku),
      quantity: 1,
      unit: item.unit || DEFAULT_UNIT,
      notes: ''
    }));
}

const MAX_ORDER_LIST_NAME_LENGTH = 200;
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

export function supplyListsToName(supplyLists: SupplyListDto[]): string | null {
  if (supplyLists.length === 0)
    return null;

  return supplyLists.map((list) => list.name).join(' + ').slice(0, MAX_ORDER_LIST_NAME_LENGTH);
}
