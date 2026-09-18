import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GetClassLocationStockRequest } from '@ske/models';
import { FilterForm } from './filter-form';

describe('FilterForm', () => {
  let fixture: ComponentFixture<FilterForm>;
  let component: FilterForm;
  let emittedFilters: GetClassLocationStockRequest[];

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [FilterForm]
    }).overrideComponent(FilterForm, {
      set: { template: '' }
    });
  });

  afterEach(() => {
    fixture?.destroy();
    TestBed.resetTestingModule();
  });

  it('does not emit a filter during initial form setup', async () => {
    render();

    await settle();

    expect(emittedFilters).toEqual([]);
  });

  it('emits a filter when a form value changes', async () => {
    render();
    await settle();

    component.stockListFilterForm.category().value.set({
      id: 'category-1',
      name: 'Papetarie'
    });

    await settle();

    expect(emittedFilters).toEqual([
      createFilter({
        searchTerm: '',
        filters: [
          {
            field: 'categoryId',
            operator: 'equals',
            value: 'category-1',
            fieldType: 'select',
            displayValue: 'Papetarie'
          }
        ]
      })
    ]);
  });

  it('does not emit when an equivalent nested form value is recreated', async () => {
    const currentFilter = createFilter({
      searchTerm: '',
      filters: [
        {
          field: 'categoryId',
          operator: 'equals',
          value: 'category-1',
          fieldType: 'select',
          displayValue: 'Papetarie'
        }
      ]
    });
    render(currentFilter);
    await settle();

    component.stockListFilterForm.category().value.set({
      id: 'category-1',
      name: 'Papetarie'
    });

    await settle();

    expect(emittedFilters).toEqual([]);
  });

  it('does not emit when the current filter is replaced by an equivalent object', async () => {
    const currentFilter = createFilter({
      searchTerm: '',
      filters: [
        {
          field: 'categoryId',
          operator: 'equals',
          value: 'category-1',
          fieldType: 'select',
          displayValue: 'Papetarie'
        }
      ]
    });
    render(currentFilter);
    await settle();

    fixture.componentRef.setInput('filter', {
      ...currentFilter,
      filters: currentFilter.filters.map(filter => ({ ...filter }))
    });

    await settle();

    expect(emittedFilters).toEqual([]);
  });

  async function settle() {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  function render(filter = createFilter()) {
    fixture = TestBed.createComponent(FilterForm);
    component = fixture.componentInstance;
    emittedFilters = [];
    component.onFilterChange.subscribe(filter => emittedFilters.push(filter));
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('filter', filter);
    fixture.detectChanges();
  }
});

function createFilter(
  overrides: Partial<GetClassLocationStockRequest> = {}
): GetClassLocationStockRequest {
  return {
    classId: 'class-1',
    filters: [],
    searchTerm: null,
    includeHidden: false,
    lowStockOnly: false,
    expiredOnly: false,
    ...overrides
  };
}
