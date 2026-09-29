import { ComponentFixture, TestBed } from '@angular/core/testing';
import { buildSupplyListActiveFilter, type GetAllSupplyListsRequest, PAGINATION_PAGE_SIZE } from '@ske/models';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { SupplyListFilterForm } from './supply-list-filter-form';

describe('SupplyListFilterForm', () => {
  let fixture: ComponentFixture<SupplyListFilterForm>;
  let emitted: GetAllSupplyListsRequest[];

  const initialFilter: GetAllSupplyListsRequest = {
    sort: [],
    filters: [buildSupplyListActiveFilter('active')!],
    cursor: null,
    pageSize: PAGINATION_PAGE_SIZE,
    searchTerm: null
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [SupplyListFilterForm],
      providers: [
        provideNzIconsTesting(),
        provideNzIcons([{ name: 'icons:magnifying-glass', icon: '<svg viewBox="0 0 24 24"></svg>' }])
      ]
    });

    fixture = TestBed.createComponent(SupplyListFilterForm);
    emitted = [];
    fixture.componentInstance.onFilterChange.subscribe((filter) => emitted.push(filter));
    fixture.componentRef.setInput('filter', initialFilter);
    fixture.detectChanges();
  });

  it('preselects Active and emits the initial filter once', () => {
    expect(fixture.componentInstance.filterForm.active().value()).toBe('active');
    expect(emitted).toHaveLength(1);
    expect(emitted[0].filters).toEqual([expect.objectContaining({ field: 'isActive', value: true })]);
  });

  it('builds inactive and all filters', () => {
    const form = fixture.componentInstance;

    form.filterForm.active().value.set('inactive');
    expect(form.buildFilterCriteria().filters).toEqual([expect.objectContaining({ field: 'isActive', value: false })]);

    form.filterForm.active().value.set('all');
    expect(form.buildFilterCriteria().filters).toEqual([]);
  });

  it('resets to Active with an empty search term', async () => {
    const form = fixture.componentInstance;
    form.filterForm.searchTerm().value.set('abc');
    form.filterForm.active().value.set('all');

    form.clear();
    await fixture.whenStable();

    expect(form.filterForm().value()).toEqual({ searchTerm: '', active: 'active' });
    expect(emitted.at(-1)).toEqual(expect.objectContaining({ searchTerm: null }));
  });
});
