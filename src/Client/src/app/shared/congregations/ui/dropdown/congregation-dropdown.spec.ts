import { TestBed } from '@angular/core/testing';
import { type CongregationDto } from '@ske/models';
import { of, Subject, throwError } from 'rxjs';
import { CongregationDropdown } from './congregation-dropdown';
// noinspection ES6PreferShortImport
import { CongregationsHttp } from '../../services/congregations.http';

describe('CongregationDropdown', () => {
  const congregation: CongregationDto = {
    id: 'congregation-1',
    name: 'Central',
    email: 'central@example.com',
    canReceiveStudents: true,
    isSmallCongregation: false,
    createdDate: '2026-01-01T00:00:00Z',
    lastModifiedDate: '2026-01-01T00:00:00Z',
  };
  const page = { data: [congregation], hasNextPage: false, nextCursor: null, sort: [] };


  let http: {
    getAll: ReturnType<typeof vi.fn>;
    getById: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    http = {
      getAll: vi.fn(),
      getById: vi.fn(),
    };

    TestBed.configureTestingModule({
      imports: [CongregationDropdown],
      providers: [{ provide: CongregationsHttp, useValue: http }],
    });
  });

  it('shows a loading label before resolving an Id-only value', () => {
    const pendingCongregation = new Subject<CongregationDto>();
    http.getById.mockReturnValue(pendingCongregation);

    const fixture = TestBed.createComponent(CongregationDropdown);
    const dropdown = fixture.componentInstance;
    fixture.componentRef.setInput('allowEdit', false);
    fixture.componentRef.setInput('allowCreate', false);
    dropdown.value.set({ id: congregation.id });
    fixture.detectChanges();

    expect(dropdown.selectedCongregationLabel()).toBe('Se încarcă congregația…');

    pendingCongregation.next(congregation);
    pendingCongregation.complete();
    fixture.detectChanges();

    expect(dropdown.selectedCongregationLabel()).toBe(congregation.name);
    fixture.destroy();
  });

  it('shows an explicit fallback when the Id cannot be resolved', () => {
    http.getById.mockReturnValue(throwError(() => ({ status: 404 })));

    const fixture = TestBed.createComponent(CongregationDropdown);
    const dropdown = fixture.componentInstance;
    fixture.componentRef.setInput('allowEdit', false);
    fixture.componentRef.setInput('allowCreate', false);
    dropdown.value.set({ id: congregation.id });
    fixture.detectChanges();

    expect(dropdown.selectedCongregationLabel()).toBe(`Congregație indisponibilă`);
    fixture.destroy();
  });

  it('uses a supplied name without making a detail request', () => {
    http.getById.mockReturnValue(of(congregation));

    const fixture = TestBed.createComponent(CongregationDropdown);
    const dropdown = fixture.componentInstance;
    fixture.componentRef.setInput('allowEdit', false);
    fixture.componentRef.setInput('allowCreate', false);
    dropdown.value.set(congregation);
    fixture.detectChanges();

    expect(dropdown.selectedCongregationLabel()).toBe(congregation.name);
    expect(http.getById).not.toHaveBeenCalled();
    fixture.destroy();
  });

  describe('canReceiveStudents filter', () => {
    const canReceiveFilter = { field: 'canReceiveStudents', operator: 'equals', value: true, fieldType: 'boolean' };

    function create(canReceiveStudents: boolean) {
      http.getAll.mockReturnValue(of(page));
      const fixture = TestBed.createComponent(CongregationDropdown);
      fixture.componentRef.setInput('allowEdit', false);
      fixture.componentRef.setInput('allowCreate', false);
      fixture.componentRef.setInput('canReceiveStudents', canReceiveStudents);
      fixture.detectChanges();
      return fixture;
    }

    afterEach(() => vi.useRealTimers());

    it('sends the filter with searches when enabled', () => {
      vi.useFakeTimers();
      const fixture = create(true);

      fixture.componentInstance.onSearch('Cen');
      vi.advanceTimersByTime(300);

      expect(http.getAll).toHaveBeenCalledWith(
        expect.objectContaining({ searchTerm: 'Cen', filters: [canReceiveFilter] }),
      );
      fixture.destroy();
    });

    it('sends no filters when disabled', () => {
      vi.useFakeTimers();
      const fixture = create(false);

      fixture.componentInstance.onSearch('Cen');
      vi.advanceTimersByTime(300);

      expect(http.getAll).toHaveBeenCalledWith(expect.objectContaining({ filters: [] }));
      fixture.destroy();
    });

    it('refetches already loaded options when the input changes', () => {
      const fixture = create(false);
      fixture.componentInstance.store.load({ searchTerm: null, filters: [], sort: [], cursor: null, pageSize: 10 });
      http.getAll.mockClear();

      fixture.componentRef.setInput('canReceiveStudents', true);
      fixture.detectChanges();

      expect(http.getAll).toHaveBeenCalledWith(expect.objectContaining({ filters: [canReceiveFilter] }));
      fixture.destroy();
    });

    it('does not fetch on input change before anything was loaded', () => {
      const fixture = create(false);

      fixture.componentRef.setInput('canReceiveStudents', true);
      fixture.detectChanges();

      expect(http.getAll).not.toHaveBeenCalled();
      fixture.destroy();
    });
  });
});
