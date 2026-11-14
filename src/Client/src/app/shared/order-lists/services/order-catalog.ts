import type { OrderCatalogItemDto, OrderListLineDto } from '@ske/models';
import { DEFAULT_UNIT, toProductName, wholeQuantity } from './order-list-wizard.utils';

export const NO_LOCATION_TITLE = 'Fără locație';

export type CatalogRow = { item: OrderCatalogItemDto; isLowStock: boolean };

export type CatalogGroup = { key: string; title: string; rows: CatalogRow[] };

/** Lowercase without diacritics, so "paine" finds "Pâine". */
export function normalize(text: string): string {
  return text.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase();
}

function compareRows(a: CatalogRow, b: CatalogRow): number {
  const left = a.item.averageIntervalDays;
  const right = b.item.averageIntervalDays;

  if (left !== right) {
    if (left === null) return 1;
    if (right === null) return -1;
    return left - right;
  }

  return a.item.name.localeCompare(b.item.name);
}

/** One group per location (an item with stock in two locations is a row in both), "Fără locație" last. */
export function catalogGroups(items: OrderCatalogItemDto[], query: string): CatalogGroup[] {
  const needle = normalize(query.trim());
  const groups = new Map<string, CatalogGroup>();

  for (const item of items) {
    if (needle && !normalize(item.name).includes(needle) && !normalize(item.sku ?? '').includes(needle))
      continue;

    const places = item.locations.length > 0
      ? item.locations.map((location) => ({ key: location.locationId, title: location.locationName, isLowStock: location.isLowStock }))
      : [{ key: '', title: NO_LOCATION_TITLE, isLowStock: false }];

    for (const place of places) {
      const group = groups.get(place.key) ?? { key: place.key, title: place.title, rows: [] };

      group.rows.push({ item, isLowStock: place.isLowStock });
      groups.set(place.key, group);
    }
  }

  return [...groups.values()]
    .map((group) => ({ ...group, rows: group.rows.sort(compareRows) }))
    .sort((a, b) => (a.key === '' ? 1 : b.key === '' ? -1 : a.title.localeCompare(b.title)));
}

/** The line a ticked / added item becomes: its last purchased quantity, 1 when it was never bought. */
export function catalogLine(item: OrderCatalogItemDto): OrderListLineDto {
  return {
    id: null,
    itemId: item.itemId,
    itemImageFileId: item.imageFileId,
    productName: toProductName(item.name, item.sku),
    quantity: wholeQuantity(item.lastQuantity ?? 0),
    unit: item.unit || DEFAULT_UNIT,
    notes: ''
  };
}

export function catalogToLines(items: OrderCatalogItemDto[], selectedIds: readonly string[]): OrderListLineDto[] {
  const selected = new Set(selectedIds);

  return items.filter((item) => selected.has(item.itemId)).map(catalogLine);
}
