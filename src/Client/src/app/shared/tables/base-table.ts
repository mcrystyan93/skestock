import {
  AfterViewInit,
  Component,
  computed,
  DestroyRef,
  effect,
  ElementRef,
  inject,
  input,
  output,
  signal,
  viewChild
} from '@angular/core';
import { CdkScrollable, ScrollDispatcher } from '@angular/cdk/scrolling';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NzTableComponent, NzTableSortOrder } from 'ng-zorro-antd/table';
import { BasePaginationFilter, PaginationSort } from '@ske/models';
import { isNil } from 'lodash-es';

@Component({
  template: ''
})
export class BaseTable<T, K extends BasePaginationFilter> implements AfterViewInit {
  public readonly items = input.required<Array<T>>();
  public readonly filter = input.required<K>();

  public readonly isReady = input(false);
  public readonly hasNextPage = input.required<boolean>();
  public readonly isLoadingMore = input.required<boolean>();

  public readonly onLoadMore = output<void>();
  public readonly onFilterChange = output<K>();


  public readonly itemsVirtualData = computed<Array<VirtualData<T>>>(() =>
    this.items().map((item, index) => ({ ...item, index }))
  );
  public readonly sortByKey = computed(() => this.toSortMap(this.filter().sort));
  public readonly virtualItemSize = signal(49);
  public readonly tableDimensions = signal<TableDimensions>(initialTableDimensions);
  public readonly virtualMinBufferPx = computed(() => this.virtualItemSize() * 2);
  public readonly virtualMaxBufferPx = computed(() => this.virtualItemSize() * 4);

  protected readonly _elementRef: ElementRef<HTMLElement> = inject(ElementRef<HTMLElement>);
  protected readonly _destroyRef = inject(DestroyRef);
  protected readonly _scrollDispatcher = inject(ScrollDispatcher);
  protected readonly _afterViewInit = signal(false);
  protected readonly _table = viewChild<NzTableComponent<T>>('table');

  private readonly measureEffect = effect(() => {
    if (!this._afterViewInit()) return;

    this.measureHost();
  });

  private readonly refreshVirtualViewportEffect = effect(() => {
    if (!this._afterViewInit()) return;

    this.tableDimensions();
    this.virtualItemSize();

    this.refreshVirtualViewport();
  });

  public ngAfterViewInit(): void {
    this._afterViewInit.set(true);

    this.observeTableScroll();
  }

  public onSortChange(value: NzTableSortOrder, key: string): void {
    const nextSort = this.buildNextSort(this.filter().sort ?? [], key, value);

    this.onFilterChange.emit({
      ...this.filter(),
      sort: nextSort
    });
  }

  protected refreshVirtualViewport(): void {
    queueMicrotask(() => {
      this._table()?.cdkVirtualScrollViewport?.checkViewportSize();
    });
  }

  private observeTableScroll(): void {
    this._scrollDispatcher
      .scrolled()
      .pipe(
        takeUntilDestroyed(this._destroyRef),
        filter((scrollable): scrollable is CdkScrollable => !!scrollable),
        filter((scrollable) => {
          if (isNil(this._elementRef)) return false;
          return this._elementRef.nativeElement.contains(scrollable.getElementRef().nativeElement);
        })
      )
      .subscribe((scrollable) => {
        const element = scrollable.getElementRef()?.nativeElement ?? null;
        if (isNil(element)) return;
        const distanceToBottom = element.scrollHeight - element.scrollTop - element.clientHeight;

        if (this.shouldLoadMore(distanceToBottom)) {
          this.onLoadMore.emit();
        }
      });
  }

  private shouldLoadMore(distanceToBottom: number): boolean {
    return distanceToBottom <= SCROLL_THRESHOLD_PX && this.hasNextPage() && !this.isLoadingMore();
  }

  private toSortMap(sort: K['sort'] | undefined): Map<string, NzTableSortOrder> {
    const map = new Map<string, NzTableSortOrder>();

    for (const item of sort ?? []) {
      map.set(item.key, item.value);
    }

    return map;
  }

  private buildNextSort(
    currentSort: PaginationSort[],
    key: string,
    order: NzTableSortOrder
  ): PaginationSort[] {
    if (order === null) {
      return currentSort.filter((item) => item.key !== key);
    }

    const nextSort = [...currentSort];
    const existingIndex = nextSort.findIndex((item) => item.key === key);
    if (existingIndex >= 0) {
      nextSort[existingIndex] = { ...nextSort[existingIndex], value: order };
      return nextSort;
    }

    nextSort.push({ key, value: order });
    return nextSort;
  }

  private measureHost() {
    if (isNil(this._elementRef)) return;
    const element = this._elementRef.nativeElement as HTMLElement;
    const updateSize = () => {
      const tableHeaderContainer = element.querySelector('.ant-table-header') as HTMLElement | null;
      const headerHeight = Math.round(tableHeaderContainer?.getBoundingClientRect().height ?? 0);
      const availableHeight = Math.max(element.clientHeight - headerHeight, 0);

      this.tableDimensions.set({
        width: `${element.clientWidth - 20}px`,
        height: `${availableHeight}px`,
        isLoaded: true
      });
    };

    updateSize();

    const observer = new ResizeObserver(updateSize);
    observer.observe(element);

    const mutationObserver = new MutationObserver(updateSize);
    mutationObserver.observe(element, { childList: true, subtree: true });

    this._destroyRef.onDestroy(() => {
      observer.disconnect();
      mutationObserver.disconnect();
    });
  }
}

export type TableDimensions = {
  width: string;
  height: string;
  isLoaded: boolean;
};
const initialTableDimensions: TableDimensions = { width: '0px', height: '0px', isLoaded: false };
const SCROLL_THRESHOLD_PX = 200;

export type VirtualData<T> = T & {
  index: number;
};
