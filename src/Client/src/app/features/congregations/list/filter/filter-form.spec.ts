import { TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { type GetAllCongregationsRequest } from '@ske/models';
import { buildBooleanFilter, FilterForm } from './filter-form';

describe('FilterForm', () => {
  const baseFilter: GetAllCongregationsRequest = { filters: [], pageSize: 50, sort: [], cursor: null, searchTerm: null };

  function createFixture(filter: GetAllCongregationsRequest = baseFilter) {
    const fixture = TestBed.createComponent(FilterForm);
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('filter', filter);
    const emitted: GetAllCongregationsRequest[] = [];
    fixture.componentInstance.onFilterChange.subscribe((f) => emitted.push(f));
    fixture.detectChanges();
    return { fixture, form: fixture.componentInstance, emitted };
  }

  beforeEach(() =>
    TestBed.configureTestingModule({
      imports: [FilterForm],
      providers: [
        provideNzIconsTesting(),
        provideNzIcons([{ name: 'icons:magnifying-glass', icon: '<svg viewBox="0 0 24 24"></svg>' }])
      ]
    })
  );

  it('emits the initial filter without boolean filters when everything is "Toate"', () => {
    const { emitted } = createFixture();

    expect(emitted).toHaveLength(1);
    expect(emitted[0].filters).toEqual([]);
  });

  it('builds a boolean equals filter for "Da" and "Nu"', () => {
    expect(buildBooleanFilter('canReceiveStudents', 'yes')).toMatchObject({
      field: 'canReceiveStudents',
      operator: 'equals',
      value: true,
      fieldType: 'boolean'
    });
    expect(buildBooleanFilter('isSmallCongregation', 'no')).toMatchObject({ value: false, fieldType: 'boolean' });
    expect(buildBooleanFilter('isSmallCongregation', 'all')).toBeNull();
  });

  it('restores the segments from the active filters', () => {
    const { form } = createFixture({
      ...baseFilter,
      filters: [
        buildBooleanFilter('canReceiveStudents', 'yes')!,
        buildBooleanFilter('isSmallCongregation', 'no')!
      ]
    });

    expect(form.congregationListFilterForm.canReceiveStudents().value()).toBe('yes');
    expect(form.congregationListFilterForm.isSmallCongregation().value()).toBe('no');
  });

  it('emits the selected segments as filters and keeps unrelated filters', async () => {
    const other = { field: 'name', operator: 'contains', value: 'x', fieldType: 'string' } as const;
    const { form, emitted } = createFixture({ ...baseFilter, filters: [other] });
    emitted.length = 0;

    form.congregationListFilterForm.canReceiveStudents().value.set('yes');
    form.congregationListFilterForm.isSmallCongregation().value.set('no');
    await form.onSubmit();

    expect(emitted.at(-1)!.filters).toEqual([
      other,
      expect.objectContaining({ field: 'canReceiveStudents', value: true }),
      expect.objectContaining({ field: 'isSmallCongregation', value: false })
    ]);
  });

  it('clears the search term and the segments', async () => {
    const { form, emitted } = createFixture({
      ...baseFilter,
      searchTerm: 'abc',
      filters: [buildBooleanFilter('canReceiveStudents', 'yes')!]
    });
    emitted.length = 0;

    form.clear();
    await Promise.resolve();

    expect(form.congregationListFilterForm.searchTerm().value()).toBe('');
    expect(form.congregationListFilterForm.canReceiveStudents().value()).toBe('all');
    expect(emitted.at(-1)).toMatchObject({ searchTerm: '', filters: [] });
  });
});
