import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import {
  FruitDessertMessageStatus,
  FruitDessertMessageType,
  type FruitDessertPlanEntryDto
} from '@ske/models';
import { FruitDessertPlansHttp } from './fruit-dessert-plans.http';
import { FruitDessertStore } from './fruit-dessert.store';

describe('FruitDessertStore', () => {
  const entry = (id: string, patch: Partial<FruitDessertPlanEntryDto> = {}): FruitDessertPlanEntryDto => ({
    id,
    date: '2026-11-10',
    congregationDeleted: false,
    emailSent: false,
    reminderSent: false,
    thanksSent: false,
    ...patch
  });

  let http: {
    get: ReturnType<typeof vi.fn>;
    setCongregation: ReturnType<typeof vi.fn>;
    setEmailSent: ReturnType<typeof vi.fn>;
    sendMessage: ReturnType<typeof vi.fn>;
  };

  const create = (entries = [entry('e1'), entry('e2')]) => {
    http.get.mockReturnValue(of(entries));
    const store = TestBed.inject(FruitDessertStore);
    store.load('class-1');
    return store;
  };

  const dispatch = (type: FruitDessertMessageType) => ({
    url: 'https://wa.me/40712345678?text=x',
    type,
    status: FruitDessertMessageStatus.Opened,
    sentAt: '2026-11-10T08:00:00Z'
  });

  beforeEach(() => {
    http = { get: vi.fn(), setCongregation: vi.fn(), setEmailSent: vi.fn(), sendMessage: vi.fn() };
    TestBed.configureTestingModule({
      providers: [FruitDessertStore, { provide: FruitDessertPlansHttp, useValue: http }]
    });
  });

  it('loads the plan of the class', () => {
    const store = create();

    expect(http.get).toHaveBeenCalledWith('class-1');
    expect(store.entries().map(e => e.id)).toEqual(['e1', 'e2']);
    expect(store.entriesLoading()).toBe(false);
    expect(store.entriesProblemDetail()).toBeNull();
  });

  it('sets the problem detail when loading fails', () => {
    http.get.mockReturnValue(throwError(() => ({ status: 500, title: 'Eroare' })));
    const store = TestBed.inject(FruitDessertStore);

    store.load('class-1');

    expect(store.entriesProblemDetail()?.status).toBe(500);
    expect(store.entriesLoading()).toBe(false);
  });

  it('changes the congregation and patches the entry from the response', () => {
    const store = create();
    http.setCongregation.mockReturnValue(of(entry('e1', {
      congregationId: 'c1', congregationName: 'Central', contactPerson: 'Ana', contactPhone: '0712345678'
    })));

    store.setCongregation({ entryId: 'e1', congregationId: 'c1' });

    expect(http.setCongregation).toHaveBeenCalledWith('class-1', 'e1', 'c1');
    expect(store.entries()[0]).toEqual(expect.objectContaining({
      congregationName: 'Central', contactPerson: 'Ana', contactPhone: '0712345678'
    }));
    expect(store.entries()[1].congregationId).toBeUndefined();
  });

  it('toggles the email flag and patches the entry', () => {
    const store = create();
    http.setEmailSent.mockReturnValue(of(entry('e2', { emailSent: true })));

    store.setEmailSent({ entryId: 'e2', emailSent: true });

    expect(http.setEmailSent).toHaveBeenCalledWith('class-1', 'e2', true);
    expect(store.entries()[1].emailSent).toBe(true);
    expect(store.entries()[0].emailSent).toBe(false);
  });

  it('sets the action problem detail when a change fails', () => {
    const store = create();
    http.setEmailSent.mockReturnValue(throwError(() => ({ status: 403 })));

    store.setEmailSent({ entryId: 'e1', emailSent: true });

    expect(store.actionProblemDetail()?.status).toBe(403);
    expect(store.entries()[0].emailSent).toBe(false);
  });

  it('opens the dispatch url in the pre-opened tab and marks the message as sent', () => {
    const store = create();
    http.sendMessage.mockReturnValue(of(dispatch(FruitDessertMessageType.Reminder)));
    const win = { location: { replace: vi.fn() }, close: vi.fn() };

    store.sendMessage({ entryId: 'e1', type: FruitDessertMessageType.Reminder, win });

    expect(http.sendMessage).toHaveBeenCalledWith('class-1', 'e1', FruitDessertMessageType.Reminder);
    expect(win.location.replace).toHaveBeenCalledWith('https://wa.me/40712345678?text=x');
    expect(store.entries()[0].reminderSent).toBe(true);
    expect(store.entries()[0].thanksSent).toBe(false);
    expect(store.lastDispatchUrl()).toBeNull();
  });

  it('marks thanks as sent', () => {
    const store = create();
    http.sendMessage.mockReturnValue(of(dispatch(FruitDessertMessageType.Thanks)));

    store.sendMessage({ entryId: 'e1', type: FruitDessertMessageType.Thanks, win: null });

    expect(store.entries()[0].thanksSent).toBe(true);
  });

  it('closes the pre-opened tab and reports the error when sending fails', () => {
    const store = create();
    http.sendMessage.mockReturnValue(throwError(() => ({ status: 422, title: 'Telefon invalid' })));
    const win = { location: { replace: vi.fn() }, close: vi.fn() };

    store.sendMessage({ entryId: 'e1', type: FruitDessertMessageType.Reminder, win });

    expect(win.close).toHaveBeenCalledOnce();
    expect(win.location.replace).not.toHaveBeenCalled();
    expect(store.actionProblemDetail()?.status).toBe(422);
    expect(store.entries()[0].reminderSent).toBe(false);
  });

  it('keeps the url for a link fallback when the popup was blocked', () => {
    const store = create();
    http.sendMessage.mockReturnValue(of(dispatch(FruitDessertMessageType.Reminder)));

    store.sendMessage({ entryId: 'e1', type: FruitDessertMessageType.Reminder, win: null });

    expect(store.lastDispatchUrl()).toEqual({ entryId: 'e1', url: 'https://wa.me/40712345678?text=x' });
  });

  it('replaces the entry with a new object when a change fails, so the row resets its local value', () => {
    const store = create();
    const before = store.entries()[0];
    http.setEmailSent.mockReturnValue(throwError(() => ({ status: 500 })));

    store.setEmailSent({ entryId: 'e1', emailSent: true });

    expect(store.entries()[0]).not.toBe(before);
    expect(store.entries()[0]).toEqual(before);
  });

  it('applies changes of the same kind in order', () => {
    const store = create();
    const first = new Subject<FruitDessertPlanEntryDto>();
    http.setEmailSent.mockReturnValueOnce(first).mockReturnValueOnce(of(entry('e1', { emailSent: false })));

    store.setEmailSent({ entryId: 'e1', emailSent: true });
    store.setEmailSent({ entryId: 'e1', emailSent: false });
    expect(http.setEmailSent).toHaveBeenCalledTimes(1);

    first.next(entry('e1', { emailSent: true }));
    first.complete();

    expect(http.setEmailSent).toHaveBeenCalledTimes(2);
    expect(store.entries()[0].emailSent).toBe(false);
  });

  it('ignores a second send while one is in flight', () => {
    const store = create();
    const pending = new Subject<ReturnType<typeof dispatch>>();
    http.sendMessage.mockReturnValue(pending);

    store.sendMessage({ entryId: 'e1', type: FruitDessertMessageType.Reminder, win: null });
    store.sendMessage({ entryId: 'e1', type: FruitDessertMessageType.Reminder, win: null });

    expect(http.sendMessage).toHaveBeenCalledTimes(1);
    expect(store.sending()).toBe(true);

    pending.next(dispatch(FruitDessertMessageType.Reminder));
    pending.complete();

    expect(store.sending()).toBe(false);
  });
});
