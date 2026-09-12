import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { SchoolClassDropdown } from './school-class-dropdown';
import { SchoolClassesHttp } from '../../services/school-classes.http';

describe('SchoolClassDropdown', () => {
  let http: { getAll: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    vi.useFakeTimers();
    http = {
      getAll: vi.fn().mockReturnValue(of({
        data: [],
        hasNextPage: false,
        nextCursor: null,
        sort: [{ key: 'name', value: 'ascend' }]
      }))
    };

    TestBed.configureTestingModule({
      imports: [SchoolClassDropdown],
      providers: [{ provide: SchoolClassesHttp, useValue: http }]
    });
  });

  afterEach(() => {
    vi.useRealTimers();
    TestBed.resetTestingModule();
  });

  it('waits for a search before loading school classes', () => {
    const fixture = TestBed.createComponent(SchoolClassDropdown);
    const dropdown = fixture.componentInstance;

    expect(http.getAll).not.toHaveBeenCalled();

    dropdown.onSearch('Clasa');
    vi.advanceTimersByTime(299);
    expect(http.getAll).not.toHaveBeenCalled();

    vi.advanceTimersByTime(1);
    expect(http.getAll).toHaveBeenCalledWith(expect.objectContaining({
      searchTerm: 'Clasa',
      filters: [],
      sort: [{ key: 'name', value: 'ascend' }]
    }));

    fixture.destroy();
  });

  it('delegates pagination to the dropdown store', () => {
    const fixture = TestBed.createComponent(SchoolClassDropdown);
    const dropdown = fixture.componentInstance;
    const loadMore = vi.spyOn(dropdown.store, 'loadMore');

    dropdown.loadMore();

    expect(loadMore).toHaveBeenCalledOnce();
    fixture.destroy();
  });

  it('supports a nullable full-object form value', () => {
    const fixture = TestBed.createComponent(SchoolClassDropdown);
    const dropdown = fixture.componentInstance;
    const schoolClass = {
      id: 'class-1',
      name: 'Clasa I'
    };

    dropdown.value.set(schoolClass);

    expect(dropdown.value()).toEqual(schoolClass);
    fixture.destroy();
  });
});
