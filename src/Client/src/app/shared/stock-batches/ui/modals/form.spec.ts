import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { provideNzDateFnsAdapter } from 'ng-zorro-antd/core/time';
import { provideNzI18n, ro_RO } from 'ng-zorro-antd/i18n';
import { ItemDto, LocationDto } from '@ske/models';
import { Form } from './form';

describe('AddStockBatch Form item location prefill', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNzIconsTesting(),
        provideNzI18n(ro_RO),
        provideNzDateFnsAdapter()
      ]
    });
  });

  function create(location: Partial<LocationDto> | null) {
    const fixture = TestBed.createComponent(Form);
    fixture.componentRef.setInput('location', location);
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  const pick = (form: Form, item: Partial<ItemDto>) => {
    form.stockBatchForm.item().value.set(item as ItemDto);
    TestBed.tick();
  };

  it('prefills the location from the picked item when the caller passed none', () => {
    const form = create(null);
    pick(form, { id: 'i1', locationId: 'l1' });

    expect(form.stockBatchForm.location().value()?.id).toBe('l1');
  });

  it('keeps the caller location over the item location', () => {
    const form = create({ id: 'caller' });
    pick(form, { id: 'i1', locationId: 'l1' });

    expect(form.stockBatchForm.location().value()?.id).toBe('caller');
  });

  it('leaves the location as is for an item without one', () => {
    const form = create(null);
    pick(form, { id: 'i1', locationId: null });

    expect(form.stockBatchForm.location().value()).toBeNull();
  });
});
