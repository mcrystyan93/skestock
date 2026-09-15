import { TestBed } from '@angular/core/testing';
import { CategoryImportListSmall } from './category-import-list-small';

describe('CategoryImportListSmall', () => {
  const filter = { filters: [], pageSize: 50, sort: [] };

  function createFixture() {
    const fixture = TestBed.createComponent(CategoryImportListSmall);
    fixture.componentRef.setInput('items', []);
    fixture.componentRef.setInput('filter', filter);
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('hasNextPage', true);
    fixture.componentRef.setInput('isLoadingMore', false);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CategoryImportListSmall],
    });
  });

  it('emits onLoadMore when scrolling within the bottom threshold', () => {
    const fixture = createFixture();
    const component = fixture.componentInstance;
    const emit = vi.spyOn(component.onLoadMore, 'emit');

    component.onScroll(createScrollEvent(1000, 700, 100));

    expect(emit).toHaveBeenCalledOnce();
    fixture.destroy();
  });

  it('joins file names for the list title', () => {
    const fixture = createFixture();
    const component = fixture.componentInstance;

    expect(
      component.fileNames({
        id: 'batch-id',
        status: 'processing',
        files: [
          {
            fileMetadataId: 'file-1',
            originalName: 'categorii.csv',
            blobPath: 'imports/categorii.csv',
            contentType: 'text/csv',
            sizeBytes: 10,
            status: 'completed',
            sortOrder: 0,
          },
          {
            fileMetadataId: 'file-2',
            originalName: 'categorii-2.csv',
            blobPath: 'imports/categorii-2.csv',
            contentType: 'text/csv',
            sizeBytes: 20,
            status: 'completed',
            sortOrder: 1,
          },
        ],
        uploadedAt: '2026-01-01T00:00:00Z',
        createdDate: '2026-01-01T00:00:00Z',
      }),
    ).toBe('categorii.csv, categorii-2.csv');

    fixture.destroy();
  });
});

function createScrollEvent(scrollHeight: number, scrollTop: number, clientHeight: number): Event {
  const element = { scrollHeight, scrollTop, clientHeight } as HTMLElement;
  const event = new Event('scroll');
  Object.defineProperty(event, 'currentTarget', { value: element });
  return event;
}
