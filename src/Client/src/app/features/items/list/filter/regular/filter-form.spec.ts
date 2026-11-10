import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { buildItemActiveFilter, ColumnFilter, GetAllItemsRequest, PAGINATION_PAGE_SIZE } from '@ske/models';
import { NzModalService } from 'ng-zorro-antd/modal';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { FilterForm } from './filter-form';

describe('FilterForm', () => {
  let fixture: ComponentFixture<FilterForm>;
  let emitted: GetAllItemsRequest[];

  const initialFilter: GetAllItemsRequest = {
    sort: [],
    filters: [buildItemActiveFilter('active')!],
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
    expect(emitted.at(-1)?.filters).toEqual([buildItemActiveFilter('active')]);
  });

  it('counts a non-default status as an active filter and emits it', async () => {
    const form = fixture.componentInstance;
    emitted = [];

    form.itemListFilterForm.active().value.set('inactive');
    await fixture.whenStable();

    expect(form.activeFilterCount()).toBe(1);
    expect(emitted.at(-1)?.filters).toEqual([buildItemActiveFilter('inactive')]);
  });

  it('drops the status filter for "all"', async () => {
    const form = fixture.componentInstance;
    emitted = [];

    form.itemListFilterForm.active().value.set('all');
    await fixture.whenStable();

    expect(emitted.at(-1)?.filters.some((f: ColumnFilter) => f.field === 'isActive')).toBe(false);
  });

  describe('merge button', () => {
    const mergeButton = () =>
      Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button'))
        .find((button) => button.textContent?.includes('Unește'));

    it('is hidden with zero or one selected item', async () => {
      for (const count of [0, 1]) {
        fixture.componentRef.setInput('selectedCount', count);
        fixture.detectChanges();
        await fixture.whenStable();

        expect(mergeButton()).toBeUndefined();
      }
    });

    it('shows the count and emits onMerge with two selected items', async () => {
      const merges: unknown[] = [];
      fixture.componentInstance.onMerge.subscribe(() => merges.push(true));
      fixture.componentRef.setInput('selectedCount', 2);
      fixture.detectChanges();
      await fixture.whenStable();

      expect(mergeButton()?.textContent?.trim()).toBe('Unește (2)');

      mergeButton()?.click();

      expect(merges).toHaveLength(1);
    });
  });
});
