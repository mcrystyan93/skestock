import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { LocationDto } from '@ske/models';
import { LocationsHttp } from '@ske/shared/locations';
import { ItemDto, ItemLocationThreshold } from '@ske/models';
import { Form } from './form';

describe('Item Form imageFileId', () => {
  let locations$ = of<LocationDto[]>([]);
  const locationsHttp = { getAllPages: () => locations$ };

  beforeEach(() => {
    locations$ = of([]);
    TestBed.configureTestingModule({
      providers: [
        { provide: LocationsHttp, useValue: locationsHttp },
        provideNzIconsTesting(),
        provideNzIcons([
          { name: 'icons:pencil', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:plus', icon: '<svg viewBox="0 0 24 24"></svg>' },
        ]),
      ],
    });
  });

  function create(imageFileId: string | null | undefined, extra: Partial<ItemDto> = {}) {
    const fixture = TestBed.createComponent(Form);
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('item', {
      id: 'i1',
      name: 'N',
      unit: 'buc',
      categoryId: 'c1',
      imageFileId,
      ...extra,
    });
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  it('maps the item image into the form model and submits it', async () => {
    const form = create('f1');
    const { isValid, formData } = await form.submit();

    expect(isValid).toBe(true);
    expect(formData?.imageFileId).toBe('f1');
  });

  it('submits null when the image was removed', async () => {
    const form = create('f1');
    form.itemForm.imageFileId().value.set(null);
    const { formData } = await form.submit();

    expect(formData?.imageFileId).toBeNull();
  });

  it('blocks submit while an image upload is in flight and allows it afterwards', async () => {
    const form = create(null);
    const state = form as unknown as { imageUploading: { set: (v: boolean) => void } };

    state.imageUploading.set(true);
    expect((await form.submit()).isValid).toBe(false);

    state.imageUploading.set(false);
    form.itemForm.imageFileId().value.set('f2');
    const { isValid, formData } = await form.submit();

    expect(isValid).toBe(true);
    expect(formData?.imageFileId).toBe('f2');
  });
});

const plain = (rows: ItemLocationThreshold[] | undefined) =>
  rows?.map(({ locationId, minThreshold }) => ({ locationId, minThreshold }));

describe('Item Form location thresholds', () => {
  const loc = (id: string, name: string, parentLocationId: string | null = null) =>
    ({ id, name, parentLocationId }) as LocationDto;
  let locations$ = of<LocationDto[]>([]);

  beforeEach(() => {
    locations$ = of([loc('a', 'Sala A'), loc('b', 'Dulap', 'a')]);
    TestBed.configureTestingModule({
      providers: [
        { provide: LocationsHttp, useValue: { getAllPages: () => locations$ } },
        provideNzIconsTesting(),
        provideNzIcons([
          { name: 'icons:pencil', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:plus', icon: '<svg viewBox="0 0 24 24"></svg>' },
        ]),
      ],
    });
  });

  function create(extra: Partial<ItemDto> = {}) {
    const fixture = TestBed.createComponent(Form);
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('item', {
      id: 'i1',
      name: 'N',
      unit: 'buc',
      categoryId: 'c1',
      ...extra,
    });
    fixture.detectChanges();
    return fixture;
  }

  it('renders one labelled box per location, prefilled from the item', () => {
    const fixture = create({ locationThresholds: [{ locationId: 'b', minThreshold: 7 }] });
    const labels = Array.from<Element>(fixture.nativeElement.querySelectorAll('label')).map((l) =>
      l.textContent?.trim(),
    );
    const inputs = fixture.nativeElement.querySelectorAll('nz-input-number input');

    expect(labels).toContain('Sala A');
    expect(labels).toContain('Sala A / Dulap');
    expect(inputs.length).toBeGreaterThanOrEqual(2);
    expect(plain(fixture.componentInstance.itemForm.locationThresholds().value())).toEqual([
      { locationId: 'a', minThreshold: 0 },
      { locationId: 'b', minThreshold: 7 },
    ]);
  });

  it('rejects a negative threshold', async () => {
    const fixture = create();
    fixture.componentInstance.itemForm.locationThresholds[0].minThreshold().value.set(-1);

    expect((await fixture.componentInstance.submit()).isValid).toBe(false);
  });

  it('submits the thresholds', async () => {
    const fixture = create();
    fixture.componentInstance.itemForm.locationThresholds[0].minThreshold().value.set(3);
    const { formData } = await fixture.componentInstance.submit();

    expect(plain(formData?.locationThresholds)).toEqual([
      { locationId: 'a', minThreshold: 3 },
      { locationId: 'b', minThreshold: 0 },
    ]);
  });

  it('applies one value to every location', async () => {
    const fixture = create();
    fixture.componentInstance.bulkForm.value().value.set(7);
    fixture.componentInstance.applyToAll();
    const { formData } = await fixture.componentInstance.submit();

    expect(plain(formData?.locationThresholds)).toEqual([
      { locationId: 'a', minThreshold: 7 },
      { locationId: 'b', minThreshold: 7 },
    ]);
  });

  it('submits an empty list when locations fail to load', async () => {
    locations$ = throwError(() => new Error('boom'));
    const fixture = create();
    const { isValid, formData } = await fixture.componentInstance.submit();

    expect(isValid).toBe(true);
    expect(formData?.locationThresholds).toEqual([]);
  });
});
