import { TestBed } from '@angular/core/testing';
import { GetAllGoodsReceiptImportsRequest } from '@ske/models';
import { FilterForm } from './filter-form';

describe('GoodsReceiptImportsFilterForm', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<FilterForm>>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [FilterForm]
    }).overrideComponent(FilterForm, {
      set: { template: '' }
    });

    fixture = TestBed.createComponent(FilterForm);
    fixture.componentRef.setInput('filter', createFilter());
    fixture.componentRef.setInput('loading', false);
    fixture.detectChanges();
  });

  afterEach(() => {
    fixture.destroy();
    TestBed.resetTestingModule();
  });

  it('counts a non-default status as one active filter', () => {
    const component = fixture.componentInstance;

    expect(component.activeFilterCount()).toBe(0);
    component.goodsReceiptImportsFilterForm.status().value.set('failed');

    expect(component.activeFilterCount()).toBe(1);
    expect(component.filtersButtonLabel()).toBe('Filtre, 1 filtru activ');
  });

  it('clears status filters and resets the count', async () => {
    const component = fixture.componentInstance;
    component.goodsReceiptImportsFilterForm.status().value.set('confirmed');

    component.clear();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(component.activeFilterCount()).toBe(0);
  });
});

function createFilter(): GetAllGoodsReceiptImportsRequest {
  return {
    filters: [],
    cursor: null,
    pageSize: 50,
    sort: []
  };
}
