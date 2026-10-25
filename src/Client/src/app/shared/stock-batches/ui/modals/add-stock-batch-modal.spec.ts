import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { signal } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { NZ_MODAL_DATA, NzModalRef } from 'ng-zorro-antd/modal';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { provideNzDateFnsAdapter } from 'ng-zorro-antd/core/time';
import { provideNzI18n, ro_RO } from 'ng-zorro-antd/i18n';
import { AddStockBatchModal } from './add-stock-batch-modal';
import { Form, StockBatchFormModel } from './form';
import { StockBatchStore } from '../../services/stock-batch.store';

describe('AddStockBatchModal', () => {
  const store = { save: vi.fn(), stockBatchLoading: signal(false) };

  function setup() {
    TestBed.configureTestingModule({
      imports: [AddStockBatchModal],
      providers: [
        { provide: NZ_MODAL_DATA, useValue: { schoolClassId: 'c1', category: null, location: null } },
        { provide: NzModalRef, useValue: { close: vi.fn(), updateConfig: vi.fn() } },
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNzIconsTesting(),
        provideNzI18n(ro_RO),
        provideNzDateFnsAdapter()
      ]
    });
    TestBed.overrideComponent(AddStockBatchModal, { set: { providers: [{ provide: StockBatchStore, useValue: store }] } });
    const fixture = TestBed.createComponent(AddStockBatchModal);
    fixture.detectChanges();
    // footer buttons live in the modal's template outlet, so drive the component API directly
    return { fixture, modal: fixture.componentInstance as any };
  }

  const detailsWrapper = (fixture: ReturnType<typeof setup>['fixture']) =>
    fixture.debugElement.query(By.css('[data-testid="details-step"]')).nativeElement as HTMLElement;

  beforeEach(() => store.save.mockClear());

  it('starts on the source step with order preselected and the form hidden', () => {
    const { fixture } = setup();
    const radios = fixture.debugElement.queryAll(By.css('input[type=radio]')).map(d => d.nativeElement as HTMLInputElement);

    expect(radios.length).toBe(2);
    expect(radios[0].checked).toBe(true);
    expect(detailsWrapper(fixture).hidden).toBe(true);
  });

  it('shows the form after choosing Donatie and going next', () => {
    const { fixture, modal } = setup();
    fixture.debugElement.queryAll(By.css('input[type=radio]'))[1].nativeElement.click();
    modal.next();
    fixture.detectChanges();

    expect(detailsWrapper(fixture).hidden).toBe(false);
    expect(fixture.debugElement.query(By.css('input[type=radio]'))).toBeNull();
  });

  it('sends the chosen source in the save request', async () => {
    const { fixture, modal } = setup();
    fixture.debugElement.queryAll(By.css('input[type=radio]'))[1].nativeElement.click();
    modal.next();
    fixture.detectChanges();

    const formData: StockBatchFormModel = {
      category: null,
      item: { id: 'i1' } as never,
      location: { id: 'l1' } as never,
      quantity: 2,
      unitPrice: 3,
      receivedDate: new Date(2026, 0, 2),
      expiryDate: null
    };
    const form = fixture.debugElement.query(By.directive(Form)).componentInstance as Form;
    vi.spyOn(form, 'submit').mockResolvedValue({ isValid: true, formData });

    await modal.save(false);

    expect(store.save).toHaveBeenCalledWith(expect.objectContaining({ source: 'donation', itemId: 'i1', receivedClassId: 'c1' }));
  });
});
