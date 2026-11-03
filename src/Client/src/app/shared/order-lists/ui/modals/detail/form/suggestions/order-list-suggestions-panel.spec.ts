import { ComponentFixture, TestBed } from '@angular/core/testing';
import type { LowStockItemDto, OrderListLineDto, PurchaseSegmentsDto, SupplyListDto, SupplyListListItemDto } from '@ske/models';
import { ClassStatisticsHttp } from '@ske/shared/class-statistics';
import { StockHttp } from '@ske/shared/stock';
import { SupplyListsHttp } from '@ske/shared/supply-lists';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { Observable, of, throwError } from 'rxjs';
import { OrderListSuggestionsPanel } from './order-list-suggestions-panel';

const lowStockItem = (itemId: string, itemName: string, locationName: string): LowStockItemDto => ({
  itemId,
  itemName,
  sku: null,
  imageFileId: null,
  unit: 'buc',
  locationName
});

const segmentItem = (itemId: string, name: string, averageQuantity: number) => ({
  itemId,
  name,
  sku: null,
  imageFileId: null,
  unit: 'cut',
  averageQuantity,
  purchaseCount: 3,
  averageIntervalDays: 7
});

const segments = (partial: Partial<PurchaseSegmentsDto>): PurchaseSegmentsDto => ({
  weekly: [],
  everyTwoWeeks: [],
  monthly: [],
  oncePerClass: [],
  ...partial
});

const supplyList = (id: string, name: string, lines: [string, string, number][]): SupplyListDto => ({
  id,
  name,
  frequency: 'Weekly',
  isActive: true,
  lines: lines.map(([itemId, itemName, quantity]) => ({ itemId, itemName, quantity, unit: 'set' })),
  createdDate: '',
  lastModifiedDate: ''
}) as SupplyListDto;

function createIcon(name: string) {
  return { name, icon: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1 1"></svg>' };
}

describe('OrderListSuggestionsPanel', () => {
  let fixture: ComponentFixture<OrderListSuggestionsPanel>;
  let emitted: OrderListLineDto[];
  let getAllActive: ReturnType<typeof vi.fn>;

  async function create(
    lowStock: Observable<LowStockItemDto[]>,
    purchaseSegments: Observable<PurchaseSegmentsDto>,
    added: string[] = [],
    supplyLists: SupplyListDto[] = []
  ) {
    getAllActive = vi.fn();
    TestBed.configureTestingModule({
      imports: [OrderListSuggestionsPanel],
      providers: [
        provideNzIconsTesting(),
        provideNzIcons([createIcon('icons:plus'), createIcon('icons:circle-check'), createIcon('icons:box')]),
        { provide: StockHttp, useValue: { getLowStockItems: vi.fn(() => lowStock) } },
        {
          provide: SupplyListsHttp,
          useValue: {
            getAllActive: getAllActive.mockReturnValue(of(supplyLists.map((list) => ({ id: list.id }) as SupplyListListItemDto))),
            getById: vi.fn((id: string) => of(supplyLists.find((list) => list.id === id)))
          }
        },
        { provide: ClassStatisticsHttp, useValue: { getPurchaseSegments: vi.fn(() => purchaseSegments) } }
      ]
    });

    fixture = TestBed.createComponent(OrderListSuggestionsPanel);
    emitted = [];
    fixture.componentRef.setInput('classId', 'class-1');
    fixture.componentRef.setInput('addedItemIds', new Set(added));
    fixture.componentInstance.add.subscribe((line) => emitted.push(line));
    await fixture.whenStable();
    fixture.detectChanges();
  }

  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';
  const buttons = () => Array.from((fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('li button'));

  async function openTab(index: number) {
    (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLElement>('.ant-tabs-tab')[index].click();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('lists low stock items once with their locations and adds quantity 1', async () => {
    await create(
      of([lowStockItem('a', 'Aţă', 'Dulap'), lowStockItem('a', 'Aţă', 'Raft')]),
      of(segments({}))
    );

    expect(text()).toContain('Aţă');
    expect(text()).toContain('Dulap, Raft');
    expect(buttons().length).toBe(1);

    buttons()[0].click();

    expect(emitted).toEqual([expect.objectContaining({ itemId: 'a', quantity: 1, unit: 'buc' })]);
  });

  it('groups the frequency tab by segment, hides empty groups and uses the average quantity', async () => {
    await create(
      of([]),
      of(segments({ weekly: [segmentItem('w', 'Hârtie', 4)], monthly: [segmentItem('m', 'Markere', 2)] }))
    );
    await openTab(1);

    expect(text()).toContain('Săptămânal');
    expect(text()).toContain('Lunar');
    expect(text()).not.toContain('La 2 săptămâni');
    expect(text()).toContain('4 cut');
  });

  it('disables and marks rows already in the list', async () => {
    await create(of([lowStockItem('a', 'Aţă', 'Dulap')]), of(segments({})), ['a']);

    expect(buttons()[0].disabled).toBe(true);
    expect(text()).toContain('Adăugat');
  });

  it('keeps low stock when the segments request fails', async () => {
    await create(of([lowStockItem('a', 'Aţă', 'Dulap')]), throwError(() => new Error('boom')));

    expect(text()).toContain('Aţă');

    await openTab(1);

    expect(text()).toContain('Nu am putut încărca produsele după frecvență.');
  });

  it('loads supply lists only when their tab is opened and groups them by list name', async () => {
    await create(of([]), of(segments({})), [], [
      supplyList('l1', 'Început de an', [['a', 'Caiet', 3], ['a', 'Caiet', 2], ['b', 'Pix', 10]]),
      supplyList('l2', 'Curățenie', [['c', 'Burete', 5]])
    ]);

    expect(getAllActive).not.toHaveBeenCalled();

    await openTab(2);

    expect(getAllActive).toHaveBeenCalledTimes(1);
    expect(text()).toContain('Început de an');
    expect(text()).toContain('Curățenie');
    expect(text()).toContain('5 set');
    expect(text()).toContain('10 set');

    buttons()[1].click();

    expect(emitted).toEqual([expect.objectContaining({ itemId: 'b', quantity: 10, unit: 'set' })]);
  });

  it('shows an empty state when there are no active supply lists', async () => {
    await create(of([]), of(segments({})));
    await openTab(2);

    expect(text()).toContain('Nu există liste predefinite active.');
  });

  it('skips a supply list that fails to load and still shows the others', async () => {
    await create(of([]), of(segments({})), [], [supplyList('l1', 'Început de an', [['a', 'Caiet', 3]]), supplyList('l2', 'Ștearsă', [])]);
    const http = TestBed.inject(SupplyListsHttp) as unknown as { getById: ReturnType<typeof vi.fn> };
    http.getById.mockImplementation((id: string) =>
      id === 'l2' ? throwError(() => new Error('404')) : of(supplyList('l1', 'Început de an', [['a', 'Caiet', 3]])));

    await openTab(2);

    expect(text()).toContain('Început de an');
    expect(text()).not.toContain('Nu am putut încărca listele predefinite.');
  });
});
