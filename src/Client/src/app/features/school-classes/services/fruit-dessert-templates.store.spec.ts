import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { FruitDessertMessageType, type MessageTemplatesDto } from '@ske/models';
import { FruitDessertTemplatesHttp } from './fruit-dessert-templates.http';
import { FruitDessertTemplatesStore } from './fruit-dessert-templates.store';

describe('FruitDessertTemplatesStore', () => {
  const response: MessageTemplatesDto = {
    templates: [
      { type: FruitDessertMessageType.Reminder, name: 'Reminder', body: '<p>a</p>', lastModified: '2026-01-01T00:00:00Z' },
      { type: FruitDessertMessageType.Thanks, name: 'Mulțumire', body: '<p>b</p>', lastModified: '2026-01-01T00:00:00Z' }
    ],
    placeholders: [{ token: '{Date}', label: 'Data', example: '10 noiembrie' }]
  };

  let http: { get: ReturnType<typeof vi.fn>; update: ReturnType<typeof vi.fn> };

  const create = () => {
    http.get.mockReturnValue(of(response));
    const store = TestBed.inject(FruitDessertTemplatesStore);
    store.load();
    return store;
  };

  beforeEach(() => {
    http = { get: vi.fn(), update: vi.fn() };
    TestBed.configureTestingModule({
      providers: [FruitDessertTemplatesStore, { provide: FruitDessertTemplatesHttp, useValue: http }]
    });
  });

  it('loads templates and placeholders', () => {
    const store = create();

    expect(store.templates()).toEqual(response.templates);
    expect(store.placeholders()).toEqual(response.placeholders);
    expect(store.templatesLoading()).toBe(false);
  });

  it('updates the local template when saving succeeds', () => {
    const store = create();
    http.update.mockReturnValue(of(undefined));

    store.save({ type: FruitDessertMessageType.Thanks, request: { name: 'Nou', body: '<p>nou</p>' } });

    expect(http.update).toHaveBeenCalledWith(FruitDessertMessageType.Thanks, { name: 'Nou', body: '<p>nou</p>' });
    expect(store.templates()[1]).toEqual(expect.objectContaining({ name: 'Nou', body: '<p>nou</p>' }));
    expect(store.templates()[0].name).toBe('Reminder');
    expect(store.saving()).toBe(false);
    expect(store.saveError()).toBeNull();
  });

  it('exposes the server message and keeps state when saving fails with 400', () => {
    const store = create();
    http.update.mockReturnValue(throwError(() => ({ status: 400, detail: 'Substituent necunoscut: {X}' })));

    store.save({ type: FruitDessertMessageType.Reminder, request: { name: 'Nou', body: '{X}' } });

    expect(store.saveError()).toBe('Substituent necunoscut: {X}');
    expect(store.templates()).toEqual(response.templates);
    expect(store.saving()).toBe(false);
  });
});
