import {
  buildItemImportBatchListFilter,
  ITEM_IMPORT_BATCH_STATUS_COLORS,
  ITEM_IMPORT_BATCH_STATUS_LABELS
} from './item-import';

describe('item import models', () => {
  it('resets pagination when applying a new list filter', () => {
    const result = buildItemImportBatchListFilter({
      searchTerm: 'old',
      filters: [],
      sort: [{ key: 'createdDate', value: 'descend' }],
      cursor: 'cursor',
      pageSize: 25
    }, { searchTerm: 'new' });

    expect(result.searchTerm).toBe('new');
    expect(result.cursor).toBeNull();
    expect(result.pageSize).toBe(25);
  });

  it('keeps labels and colors aligned for every import status', () => {
    expect(Object.keys(ITEM_IMPORT_BATCH_STATUS_LABELS)).toEqual(Object.keys(ITEM_IMPORT_BATCH_STATUS_COLORS));
    expect(ITEM_IMPORT_BATCH_STATUS_LABELS.pendingReview).toBe('In asteptare');
    expect(ITEM_IMPORT_BATCH_STATUS_COLORS.confirmed).toBe('success');
  });
});
