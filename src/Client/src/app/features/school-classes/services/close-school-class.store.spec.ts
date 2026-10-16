import { TestBed } from '@angular/core/testing';
import { Events } from '@ngrx/signals/events';
import { of, throwError } from 'rxjs';
import { ClassStatus, type SchoolClassDto } from '@ske/models';
import { SchoolClassesHttp } from '@ske/shared/school-classes';
import { CloseSchoolClassStore, closeSchoolClassEvents } from './close-school-class.store';

describe('CloseSchoolClassStore', () => {
  const classOf = (id: string, status: ClassStatus): SchoolClassDto => ({
    id,
    name: `Clasa ${id}`,
    startDate: '2026-09-01',
    endDate: '2027-06-15',
    status,
    createdDate: '2026-01-01T00:00:00Z',
    lastModifiedDate: '2026-01-01T00:00:00Z'
  });

  let http: { getAll: ReturnType<typeof vi.fn>; close: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    http = {
      getAll: vi.fn().mockReturnValue(of({ data: [], hasNextPage: false, nextCursor: null, sort: [] })),
      close: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [CloseSchoolClassStore, { provide: SchoolClassesHttp, useValue: http }]
    });
  });

  it('loads only open classes other than the one being closed', () => {
    const store = TestBed.inject(CloseSchoolClassStore);

    store.loadTargets('source');

    const request = http.getAll.mock.calls[0][0];
    expect(request.filters).toEqual(expect.arrayContaining([
      expect.objectContaining({ field: 'status', operator: 'notEquals', value: 'closed' }),
      expect.objectContaining({ field: 'id', operator: 'notEquals', value: 'source' })
    ]));
  });

  it('never offers a closed class or the source class as a target', () => {
    http.getAll.mockReturnValue(of({
      data: [classOf('source', ClassStatus.Active), classOf('old', ClassStatus.Closed), classOf('next', ClassStatus.Upcoming)],
      hasNextPage: false,
      nextCursor: null,
      sort: []
    }));
    const store = TestBed.inject(CloseSchoolClassStore);

    store.loadTargets('source');

    expect(store.targets().map(c => c.id)).toEqual(['next']);
  });

  it('closes the class into the chosen target and announces success', () => {
    http.close.mockReturnValue(of(void 0));
    const store = TestBed.inject(CloseSchoolClassStore);
    const closed = vi.fn();
    TestBed.inject(Events).on(closeSchoolClassEvents.closed).subscribe(closed);

    store.close({ classId: 'source', targetClassId: 'next' });

    expect(http.close).toHaveBeenCalledWith('source', 'next');
    expect(store.closeLoading()).toBe(false);
    expect(store.closeProblemDetail()).toBeNull();
    expect(closed).toHaveBeenCalledOnce();
  });

  it('keeps the error and does not announce success when closing fails', () => {
    http.close.mockReturnValue(throwError(() => ({ status: 409, error: { title: 'Conflict', status: 409 } })));
    const store = TestBed.inject(CloseSchoolClassStore);
    const closed = vi.fn();
    TestBed.inject(Events).on(closeSchoolClassEvents.closed).subscribe(closed);

    store.close({ classId: 'source', targetClassId: 'next' });

    expect(store.closeLoading()).toBe(false);
    expect(store.closeProblemDetail()).not.toBeNull();
    expect(closed).not.toHaveBeenCalled();
  });
});
