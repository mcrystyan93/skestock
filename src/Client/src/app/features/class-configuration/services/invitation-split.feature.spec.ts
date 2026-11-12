import { TestBed } from '@angular/core/testing';
import { InvitationSplitDto, SaveInvitationSplitRequest } from '@ske/models';
import { of, throwError } from 'rxjs';
import { ConfigurationHttp } from './configuration.http';
import { ConfigurationStore } from './configuration.store';

describe('ConfigurationStore invitation split', () => {
  let http: {
    getInvitationSplit: ReturnType<typeof vi.fn>;
    saveInvitationSplit: ReturnType<typeof vi.fn>;
  };

  const split: InvitationSplitDto = {
    studentInvitationCount: 10,
    instructorInvitationCount: 5,
    openingPrayerInvitationCount: 2,
    experienceInvitationCount: 3,
    changeInvitationCount: 4,
  };
  const request: SaveInvitationSplitRequest = {
    studentInvitationCount: 11,
    instructorInvitationCount: 6,
    openingPrayerInvitationCount: 3,
    experienceInvitationCount: 4,
    changeInvitationCount: 5,
  };

  beforeEach(() => {
    http = {
      getInvitationSplit: vi.fn(),
      saveInvitationSplit: vi.fn(),
    };
    TestBed.configureTestingModule({
      providers: [ConfigurationStore, { provide: ConfigurationHttp, useValue: http }],
    });
  });

  it('starts with zero counts', () => {
    const store = TestBed.inject(ConfigurationStore);

    expect(Object.values(store.invitationSplit()).every((value) => value === 0)).toBe(true);
    expect(store.invitationSplitLoading()).toBe(false);
  });

  it('loads the split and clears loading', () => {
    http.getInvitationSplit.mockReturnValue(of(split));
    const store = TestBed.inject(ConfigurationStore);

    store.loadInvitationSplit();

    expect(store.invitationSplit()).toEqual(split);
    expect(store.invitationSplitLoading()).toBe(false);
  });

  it('saves then reloads the confirmed split', () => {
    http.getInvitationSplit.mockReturnValue(of(split));
    http.saveInvitationSplit.mockReturnValue(of(request));
    const store = TestBed.inject(ConfigurationStore);

    store.saveInvitationSplit(request);

    expect(http.saveInvitationSplit).toHaveBeenCalledWith(request);
    expect(http.getInvitationSplit).toHaveBeenCalledOnce();
    expect(store.invitationSplit()).toEqual(split);
    expect(store.invitationSplitLoading()).toBe(false);
  });

  it('keeps the last confirmed values when saving fails', () => {
    http.getInvitationSplit.mockReturnValue(of(split));
    http.saveInvitationSplit.mockReturnValue(
      throwError(() => ({ status: 400, title: 'Validation error' })),
    );
    const store = TestBed.inject(ConfigurationStore);
    store.loadInvitationSplit();

    store.saveInvitationSplit(request);

    expect(store.invitationSplit()).toEqual(split);
    expect(store.invitationSplitProblemDetail()).toEqual({ status: 400, title: 'Validation error' });
    expect(store.invitationSplitLoading()).toBe(false);
    expect(http.getInvitationSplit).toHaveBeenCalledOnce();
  });
});
