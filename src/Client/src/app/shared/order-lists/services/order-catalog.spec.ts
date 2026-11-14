import type { OrderCatalogItemDto } from '@ske/models';
import { catalogGroups, catalogLine, catalogToLines } from './order-catalog';

const loc = (locationId: string, locationName: string, isLowStock = false) => ({ locationId, locationName, isLowStock });

const item = (itemId: string, name: string, overrides: Partial<OrderCatalogItemDto> = {}): OrderCatalogItemDto => ({
  itemId, name, sku: null, imageFileId: null, unit: 'buc', lastQuantity: null, averageIntervalDays: null, isFrequent: false,
  locations: [loc('a', 'Cămară')], ...overrides
});

describe('order catalog', () => {
  it('lists a multi-location item in both groups with that location low-stock flag, "Fără locație" last', () => {
    const groups = catalogGroups([
      item('1', 'Făină', { locations: [loc('b', 'Bucătărie', true), loc('a', 'Cămară')] }),
      item('2', 'Hârtie', { locations: [] })
    ], '');

    expect(groups.map((g) => g.title)).toEqual(['Bucătărie', 'Cămară', 'Fără locație']);
    expect(groups[0].rows[0]).toMatchObject({ isLowStock: true });
    expect(groups[1].rows[0]).toMatchObject({ isLowStock: false });
    expect(groups[0].rows[0].item.itemId).toBe(groups[1].rows[0].item.itemId);
  });

  it('sorts rows by interval ascending, nulls last, then by name', () => {
    const [group] = catalogGroups([
      item('1', 'B', { averageIntervalDays: null }),
      item('2', 'A', { averageIntervalDays: null }),
      item('3', 'Z', { averageIntervalDays: 14 }),
      item('4', 'Y', { averageIntervalDays: 7 })
    ], '');

    expect(group.rows.map((r) => r.item.itemId)).toEqual(['4', '3', '2', '1']);
  });

  it('filters by name and SKU ignoring diacritics and drops empty groups', () => {
    const items = [
      item('1', 'Pâine'),
      item('2', 'Lapte', { sku: 'LPT-1', locations: [loc('b', 'Frigider')] })
    ];

    expect(catalogGroups(items, 'paine').flatMap((g) => g.rows.map((r) => r.item.itemId))).toEqual(['1']);
    expect(catalogGroups(items, 'lpt').map((g) => g.title)).toEqual(['Frigider']);
    expect(catalogGroups(items, 'zzz')).toEqual([]);
  });

  it('turns the last quantity into a whole line quantity, defaulting to 1', () => {
    expect(catalogLine(item('1', 'A', { lastQuantity: 3.0 })).quantity).toBe(3);
    expect(catalogLine(item('1', 'A', { lastQuantity: null })).quantity).toBe(1);
    expect(catalogLine(item('1', 'A', { lastQuantity: 0 })).quantity).toBe(1);
    expect(catalogLine(item('1', 'A', { sku: 'S' })).productName).toBe('(S) A');
  });

  it('builds one line per selected item', () => {
    const items = [item('1', 'A', { locations: [loc('a', 'X'), loc('b', 'Y')] }), item('2', 'B')];

    expect(catalogToLines(items, ['1', '1']).map((l) => l.itemId)).toEqual(['1']);
  });
});
