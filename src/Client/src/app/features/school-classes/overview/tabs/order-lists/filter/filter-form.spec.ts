import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GetAllOrderListsRequest } from '@ske/models';
import { FilterForm } from './filter-form';

describe('OrderListFilterForm', () => {
  let fixture: ComponentFixture<FilterForm>;
  let component: FilterForm;
  let emitted: GetAllOrderListsRequest[];

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [FilterForm]
    }).overrideComponent(FilterForm, {
      set: { template: '' }
    });

    fixture = TestBed.createComponent(FilterForm);
    component = fixture.componentInstance;
    emitted = [];
    component.onFilterChange.subscribe(filter => emitted.push(filter));
    fixture.componentRef.setInput('filter', createFilter());
    fixture.componentRef.setInput('loading', false);
    fixture.detectChanges();
  });

  afterEach(() => {
    fixture.destroy();
    TestBed.resetTestingModule();
  });

  it('starts without active status filters', () => {
    expect(component.activeFilterCount()).toBe(0);
    expect(component.filtersButtonLabel()).toBe('Filtre');
  });

  it('counts any non-default status as one active filter', () => {
    component.orderListFilterForm.status().value.set('Submitted');

    expect(component.activeFilterCount()).toBe(1);
    expect(component.filtersButtonLabel()).toBe('Filtre, 1 filtru activ');
  });

  it('emits the selected status and resets the active-filter count', async () => {
    component.orderListFilterForm.status().value.set('Cancelled');
    await settle();

    expect(emitted.at(-1)?.filters).toEqual([
      {
        displayValue: 'Cancelled',
        field: 'status',
        operator: 'equals',
        value: 'Cancelled',
        fieldType: 'string'
      }
    ]);

    component.clear();
    await settle();

    expect(component.activeFilterCount()).toBe(0);
    expect(emitted.at(-1)?.filters).toEqual([]);
  });

  async function settle() {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }
});

function createFilter(): GetAllOrderListsRequest {
  return {
    filters: [],
    cursor: null,
    pageSize: 50,
    sort: [],
    searchTerm: null
  };
}
