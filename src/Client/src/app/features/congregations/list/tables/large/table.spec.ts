import { TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { CongregationDto } from '@ske/models';
import { Table } from './table';

describe('Congregation Table', () => {
  const congregation: CongregationDto = {
    id: 'c1',
    name: 'Central',
    email: 'central@example.com',
    canReceiveStudents: true,
    isSmallCongregation: false,
    createdDate: '2026-01-01T00:00:00Z',
    lastModifiedDate: '2026-01-01T00:00:00Z'
  };

  beforeEach(() => {
    // jsdom has no ResizeObserver, which the base table uses to size the virtual scroll area.
    vi.stubGlobal('ResizeObserver', class {
      observe() {}
      unobserve() {}
      disconnect() {}
    });
    TestBed.configureTestingModule({
      imports: [Table],
      providers: [
        provideNzIconsTesting(),
        provideNzIcons([
          { name: 'icons:pen-to-square', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:trash-can', icon: '<svg viewBox="0 0 24 24"></svg>' }
        ])
      ]
    });
  });

  afterEach(() => vi.unstubAllGlobals());

  function createFixture() {
    const fixture = TestBed.createComponent(Table);
    fixture.componentRef.setInput('items', [congregation]);
    fixture.componentRef.setInput('filter', { filters: [], pageSize: 50, sort: [] });
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('hasNextPage', false);
    fixture.componentRef.setInput('isLoadingMore', false);
    fixture.detectChanges();
    return fixture;
  }

  it('renders one header per column', () => {
    const fixture = createFixture();

    const headers = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('th')).map((th) =>
      th.textContent?.trim()
    );

    expect(headers).toEqual([
      'Nume',
      'Email',
      'Coordonator',
      'Poate primi cursanți',
      'Congregație mică',
      'Ultima modificare',
      'Acțiuni'
    ]);
    fixture.destroy();
  });
});
