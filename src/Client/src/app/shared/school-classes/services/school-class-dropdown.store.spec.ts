import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { SchoolClassDropdownStore } from './school-class-dropdown.store';
import { SchoolClassesHttp } from './school-classes.http';
import { ClassStatus, type SchoolClassDto } from '@ske/models';

describe('SchoolClassDropdownStore', () => {
  const firstClass: SchoolClassDto = {
    id: 'class-1',
    name: 'Clasa I',
    startDate: '2026-09-01',
    endDate: '2027-06-15',
    status: ClassStatus.Active,
    createdDate: '2026-01-01T00:00:00Z',
    lastModifiedDate: '2026-01-01T00:00:00Z'
  };
  const secondClass: SchoolClassDto = {
    ...firstClass,
    id: 'class-2',
    name: 'Clasa a II-a'
  };

  let http: { getAll: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    http = {
      getAll: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        SchoolClassDropdownStore,
        { provide: SchoolClassesHttp, useValue: http }
      ]
    });
  });

  it('loads the first page with the requested filter', () => {
    http.getAll.mockReturnValue(of({
      data: [firstClass],
      hasNextPage: true,
      nextCursor: 'next-page',
      sort: [{ key: 'name', value: 'ascend' }]
    }));

    const store = TestBed.inject(SchoolClassDropdownStore);
    const filter = {
      searchTerm: 'Clasa',
      filters: [],
      sort: [{ key: 'name', value: 'ascend' as const }],
      cursor: null,
      pageSize: 10
    };

    store.load(filter);

    expect(http.getAll).toHaveBeenCalledWith(filter);
    expect(store.schoolClasses()).toEqual([firstClass]);
    expect(store.hasNextPage()).toBe(true);
    expect(store.nextCursor()).toBe('next-page');
    expect(store.schoolClassesLoading()).toBe(false);
  });

  it('appends the next page without replacing the first page', () => {
    http.getAll
      .mockReturnValueOnce(of({
        data: [firstClass],
        hasNextPage: true,
        nextCursor: 'next-page',
        sort: [{ key: 'name', value: 'ascend' }]
      }))
      .mockReturnValueOnce(of({
        data: [secondClass],
        hasNextPage: false,
        nextCursor: null,
        sort: [{ key: 'name', value: 'ascend' }]
      }));

    const store = TestBed.inject(SchoolClassDropdownStore);
    store.load({
      searchTerm: null,
      filters: [],
      sort: [{ key: 'name', value: 'ascend' as const }],
      cursor: null,
      pageSize: 10
    });
    store.loadMore();

    expect(http.getAll).toHaveBeenNthCalledWith(2, expect.objectContaining({
      cursor: 'next-page'
    }));
    expect(store.schoolClasses()).toEqual([firstClass, secondClass]);
    expect(store.hasNextPage()).toBe(false);
  });

  it('keeps an empty collection and exposes the problem detail after a failed load', () => {
    const problem = {
      status: 503,
      title: 'School classes unavailable'
    };
    http.getAll.mockReturnValue(throwError(() => problem));

    const store = TestBed.inject(SchoolClassDropdownStore);
    store.load({
      searchTerm: 'Clasa',
      filters: [],
      sort: [{ key: 'name', value: 'ascend' as const }],
      cursor: null,
      pageSize: 10
    });

    expect(store.schoolClasses()).toEqual([]);
    expect(store.schoolClassesProblemDetail()).toEqual(problem);
    expect(store.schoolClassesLoading()).toBe(false);
  });
});
