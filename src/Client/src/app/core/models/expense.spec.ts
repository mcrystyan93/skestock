import { buildExpenseListFilter, EXPENSE_STATUS_COLORS, EXPENSE_STATUS_LABELS, GetAllExpensesRequest } from './expense';

describe('expense model', () => {
  const current: GetAllExpensesRequest = {
    sort: [], filters: [{ field: 'classId', operator: 'equals', fieldType: 'string', value: 'c1' }],
    cursor: 'next', pageSize: 20, searchTerm: null
  };

  it('labels every status in Romanian and gives it a color', () => {
    expect(EXPENSE_STATUS_LABELS).toEqual({ processing: 'În procesare', processed: 'Procesat', failed: 'Eroare' });
    expect(Object.keys(EXPENSE_STATUS_COLORS)).toEqual(Object.keys(EXPENSE_STATUS_LABELS));
  });

  it('resets the cursor and keeps the class filter when the filter changes', () => {
    const result = buildExpenseListFilter(current, { searchTerm: 'acme' });

    expect(result.cursor).toBeNull();
    expect(result.searchTerm).toBe('acme');
    expect(result.filters).toEqual(current.filters);
  });
});
