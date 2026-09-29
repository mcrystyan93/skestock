import {
  buildSupplyListFilter,
  formatSupplyListFrequency,
  SUPPLY_LIST_FREQUENCY_LABELS,
  SUPPLY_LIST_FREQUENCY_OPTIONS
} from './supply-list';

describe('supply list models', () => {
  it('formats every frequency in Romanian', () => {
    expect(formatSupplyListFrequency('Weekly')).toBe('Săptămânal');
    expect(formatSupplyListFrequency('Monthly')).toBe('Lunar');
    expect(formatSupplyListFrequency('StartOfSchoolYear')).toBe('La începutul clasei');
    expect(formatSupplyListFrequency('EndOfSchoolYear')).toBe('La sfârșitul clasei');
    expect(formatSupplyListFrequency('MiddleOfSemester')).toBe('Mijlocul clasei');
    expect(formatSupplyListFrequency('StartOfMonth')).toBe('La începutul lunii');
    expect(formatSupplyListFrequency('EndOfMonth')).toBe('La sfârșitul lunii');
    expect(formatSupplyListFrequency('Once')).toBe('O singură dată');
  });

  it('includes the interval for EveryXWeeks', () => {
    expect(formatSupplyListFrequency('EveryXWeeks', 3)).toBe('La fiecare 3 săptămâni');
    expect(formatSupplyListFrequency('EveryXWeeks', null)).toBe('La fiecare X săptămâni');
  });

  it('exposes one option per frequency', () => {
    expect(SUPPLY_LIST_FREQUENCY_OPTIONS).toHaveLength(Object.keys(SUPPLY_LIST_FREQUENCY_LABELS).length);
  });

  it('resets the cursor when applying a new filter', () => {
    const result = buildSupplyListFilter(
      { searchTerm: null, filters: [], sort: [], cursor: 'c', pageSize: 25 },
      { searchTerm: 'x' }
    );

    expect(result.cursor).toBeNull();
    expect(result.searchTerm).toBe('x');
    expect(result.pageSize).toBe(25);
  });
});
