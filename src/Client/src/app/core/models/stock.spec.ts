import { groupStockByItem, StockItemDto, stockLocationLabel } from './stock';

const line = (itemId: string, locationId: string, overrides: Partial<StockItemDto> = {}): StockItemDto => ({
  itemId, itemName: itemId, categoryId: 'c', categoryName: 'Alimente', locationId, locationName: locationId,
  lastUpdatedAt: null, unit: 'kg', isPerishable: false, isExpired: false, expiredQuantity: 0, quantity: 1,
  isLowStock: false, hideWhenZeroStock: false, ...overrides
});

describe('groupStockByItem', () => {
  it('lists an item stocked in several locations once, summing quantity and keeping location order', () => {
    const groups = groupStockByItem([line('mere', 'l1', { quantity: 2 }), line('faina', 'l1'), line('mere', 'l2', { quantity: 5 })]);

    expect(groups.map(g => g.itemId)).toEqual(['mere', 'faina']);
    expect(groups[0].locations.map(l => l.locationId)).toEqual(['l1', 'l2']);
    expect(groups[0].totalQuantity).toBe(7);
  });

  it('flags the group when any location is low on stock or expired', () => {
    const [group] = groupStockByItem([line('mere', 'l1'), line('mere', 'l2', { isLowStock: true }), line('mere', 'l3', { isExpired: true })]);

    expect(group.isLowStock).toBe(true);
    expect(group.isExpired).toBe(true);
    expect(groupStockByItem([line('mere', 'l1')])[0]).toMatchObject({ isLowStock: false, isExpired: false });
  });

  it('uses the most recent update across locations, ignoring locations never updated', () => {
    const [group] = groupStockByItem([
      line('mere', 'l1', { lastUpdatedAt: '2026-09-01T10:00:00Z' }),
      line('mere', 'l2', { lastUpdatedAt: '2026-09-20T10:00:00Z' }),
      line('mere', 'l3', { lastUpdatedAt: null })
    ]);

    expect(group.lastUpdatedAt).toBe('2026-09-20T10:00:00Z');
  });
});

describe('stockLocationLabel', () => {
  it('announces quantity and flags so assistive tech does not lose them', () => {
    expect(stockLocationLabel(line('mere', 'Frigider', { quantity: 8, isLowStock: true, isExpired: true, expiredQuantity: 3 })))
      .toBe('Ajustează stocul pentru mere la Frigider: 8 kg, stoc redus, posibil expirat 3');
    expect(stockLocationLabel(line('mere', 'Frigider'))).toBe('Ajustează stocul pentru mere la Frigider: 1 kg');
  });
});
