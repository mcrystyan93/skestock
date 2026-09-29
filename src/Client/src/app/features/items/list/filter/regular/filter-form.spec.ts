import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { GetAllItemsRequest, PAGINATION_PAGE_SIZE } from '@ske/models';
import { NzModalService } from 'ng-zorro-antd/modal';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { FilterForm } from './filter-form';

describe('FilterForm', () => {
  let fixture: ComponentFixture<FilterForm>;
  let emitted: GetAllItemsRequest[];

  const initialFilter: GetAllItemsRequest = {
    sort: [],
    filters: [],
    cursor: null,
    pageSize: PAGINATION_PAGE_SIZE,
    searchTerm: null
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [FilterForm],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        NzModalService,
        provideNzIconsTesting(),
        provideNzIcons([
          { name: 'icons:magnifying-glass', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:sliders', icon: '<svg viewBox="0 0 24 24"></svg>' }
        ])
      ]
    });

    fixture = TestBed.createComponent(FilterForm);
    emitted = [];
    fixture.componentInstance.onFilterChange.subscribe((filter) => emitted.push(filter));
    fixture.componentRef.setInput('filter', initialFilter);
    fixture.componentRef.setInput('loading', false);
    fixture.detectChanges();
  });

  it('counts no active filters without a category', () => {
    expect(fixture.componentInstance.activeFilterCount()).toBe(0);
    expect(fixture.componentInstance.filtersButtonLabel()).toBe('Filtre');
  });

  it('counts the selected category as an active filter', () => {
    const form = fixture.componentInstance;

    form.itemListFilterForm.category().value.set({ id: 'category-1', name: 'Papetărie' } as never);

    expect(form.activeFilterCount()).toBe(1);
    expect(form.filtersButtonLabel()).toBe('Filtre, 1 filtru activ');
  });

  it('clears the category and emits the reset filter', async () => {
    const form = fixture.componentInstance;
    form.itemListFilterForm.category().value.set({ id: 'category-1', name: 'Papetărie' } as never);
    emitted = [];

    form.clear();
    await fixture.whenStable();

    expect(form.activeFilterCount()).toBe(0);
    expect(emitted.at(-1)?.filters).toEqual([]);
  });
});
