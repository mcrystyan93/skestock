import { Directive, Input, OnChanges, forwardRef } from '@angular/core';
import { coerceNumberProperty, NumberInput } from '@angular/cdk/coercion';
import { ListRange } from '@angular/cdk/collections';
import { CdkVirtualScrollViewport, VIRTUAL_SCROLL_STRATEGY, VirtualScrollStrategy } from '@angular/cdk/scrolling';
import { Observable, Subject } from 'rxjs';
import { distinctUntilChanged } from 'rxjs/operators';

const SIZE_EPSILON = 0.5;

/**
 * A {@link VirtualScrollStrategy} that supports items of unknown, variable height.
 *
 * Each item's height starts from {@link estimatedItemSize} and is refined once the item is
 * rendered and measured (see the companion `skeVirtualItem` directive). Measured heights are
 * cached per data index, so the total scrollable size and the rendered window converge to the
 * real layout without requiring a fixed `itemSize`.
 */
export class DynamicSizeVirtualScrollStrategy implements VirtualScrollStrategy {
  private readonly _scrolledIndexChange = new Subject<number>();
  public readonly scrolledIndexChange: Observable<number> =
    this._scrolledIndexChange.pipe(distinctUntilChanged());

  private viewport: CdkVirtualScrollViewport | null = null;
  private sizes: number[] = [];
  private measured: boolean[] = [];
  private lastScrolledIndex = -1;

  constructor(
    private estimatedItemSize: number,
    private minBufferPx: number,
    private maxBufferPx: number,
  ) {}

  public attach(viewport: CdkVirtualScrollViewport): void {
    this.viewport = viewport;
    this.onDataLengthChanged();
  }

  public detach(): void {
    this._scrolledIndexChange.complete();
    this.viewport = null;
  }

  public updateBuffers(estimatedItemSize: number, minBufferPx: number, maxBufferPx: number): void {
    this.estimatedItemSize = estimatedItemSize;
    this.minBufferPx = minBufferPx;
    this.maxBufferPx = maxBufferPx;
    if (this.viewport) {
      this.viewport.setTotalContentSize(this.totalSize());
      this.updateRenderedRange();
    }
  }

  public onContentScrolled(): void {
    this.updateRenderedRange();
  }

  public onDataLengthChanged(): void {
    if (!this.viewport) {
      return;
    }

    const length = this.dataLength;

    if (this.sizes.length > length) {
      this.sizes.length = length;
      this.measured.length = length;
    } else {
      while (this.sizes.length < length) {
        this.sizes.push(this.estimatedItemSize);
        this.measured.push(false);
      }
    }

    this.viewport.setTotalContentSize(this.totalSize());
    this.updateRenderedRange();
  }

  public onContentRendered(): void {
    // Heights are reported by the item directive via updateItemSize; nothing to do here.
  }

  public onRenderedOffsetChanged(): void {
    // Offset is driven by updateRenderedRange; nothing to reconcile here.
  }

  public scrollToIndex(index: number, behavior: ScrollBehavior): void {
    if (!this.viewport) {
      return;
    }

    const clamped = Math.max(0, Math.min(index, this.dataLength));
    this.viewport.scrollToOffset(this.offsetAt(clamped), behavior);
  }

  /** Records the measured height of a rendered item and reflows if it changed. */
  public updateItemSize(index: number, size: number): void {
    if (!this.viewport || index < 0 || index >= this.sizes.length || size <= 0) {
      return;
    }

    const previous = this.sizes[index];
    const delta = size - previous;

    if (this.measured[index] && Math.abs(delta) < SIZE_EPSILON) {
      return;
    }

    this.sizes[index] = size;
    this.measured[index] = true;

    if (Math.abs(delta) < SIZE_EPSILON) {
      return;
    }

    this.viewport.setTotalContentSize(this.totalSize());

    // Compensate for size changes that occur above the current scroll position so the content
    // the user is looking at does not visibly jump.
    const scrollOffset = this.viewport.measureScrollOffset();
    if (this.offsetAt(index) < scrollOffset) {
      this.viewport.scrollToOffset(scrollOffset + delta);
    }

    this.updateRenderedRange();
  }

  private get dataLength(): number {
    return this.viewport?.getDataLength() ?? 0;
  }

  private sizeAt(index: number): number {
    return this.sizes[index] ?? this.estimatedItemSize;
  }

  private offsetAt(index: number): number {
    let offset = 0;
    const clamped = Math.min(index, this.sizes.length);
    for (let i = 0; i < clamped; i++) {
      offset += this.sizeAt(i);
    }
    return offset;
  }

  private totalSize(): number {
    return this.offsetAt(this.dataLength);
  }

  private updateRenderedRange(): void {
    if (!this.viewport) {
      return;
    }

    const length = this.dataLength;
    if (length === 0) {
      this.setRange({ start: 0, end: 0 }, 0);
      return;
    }

    const viewportSize = this.viewport.getViewportSize();
    const scrollOffset = this.viewport.measureScrollOffset();
    const renderStartOffset = Math.max(0, scrollOffset - this.minBufferPx);
    const renderEndOffset = scrollOffset + viewportSize + this.maxBufferPx;

    let offset = 0;
    let start = 0;
    while (start < length && offset + this.sizeAt(start) <= renderStartOffset) {
      offset += this.sizeAt(start);
      start++;
    }

    const startOffset = offset;
    let end = start;
    let visibleOffset = offset;
    while (end < length && visibleOffset < renderEndOffset) {
      visibleOffset += this.sizeAt(end);
      end++;
    }

    this.setRange({ start, end }, startOffset);
  }

  private setRange(range: ListRange, offset: number): void {
    if (!this.viewport) {
      return;
    }

    this.viewport.setRenderedRange(range);
    this.viewport.setRenderedContentOffset(offset);

    if (range.start !== this.lastScrolledIndex) {
      this.lastScrolledIndex = range.start;
      this._scrolledIndexChange.next(range.start);
    }
  }
}

/**
 * Attaches a {@link DynamicSizeVirtualScrollStrategy} to a `cdk-virtual-scroll-viewport`.
 *
 * Uses classic `@Input()` setters (mirroring CDK's own `CdkFixedSizeVirtualScroll`) because the
 * strategy instance must exist before Angular binds the inputs so it can be provided through the
 * `VIRTUAL_SCROLL_STRATEGY` token.
 */
@Directive({
  selector: 'cdk-virtual-scroll-viewport[dynamicItemSize]',
  providers: [
    {
      provide: VIRTUAL_SCROLL_STRATEGY,
      useFactory: (directive: CdkDynamicSizeVirtualScroll) => directive.scrollStrategy,
      deps: [forwardRef(() => CdkDynamicSizeVirtualScroll)],
    },
  ],
})
export class CdkDynamicSizeVirtualScroll implements OnChanges {
  @Input()
  public get estimatedItemSize(): number {
    return this._estimatedItemSize;
  }
  public set estimatedItemSize(value: NumberInput) {
    this._estimatedItemSize = coerceNumberProperty(value, this._estimatedItemSize);
  }
  private _estimatedItemSize = 200;

  @Input()
  public get minBufferPx(): number {
    return this._minBufferPx;
  }
  public set minBufferPx(value: NumberInput) {
    this._minBufferPx = coerceNumberProperty(value, this._minBufferPx);
  }
  private _minBufferPx = 200;

  @Input()
  public get maxBufferPx(): number {
    return this._maxBufferPx;
  }
  public set maxBufferPx(value: NumberInput) {
    this._maxBufferPx = coerceNumberProperty(value, this._maxBufferPx);
  }
  private _maxBufferPx = 400;

  public readonly scrollStrategy = new DynamicSizeVirtualScrollStrategy(
    this._estimatedItemSize,
    this._minBufferPx,
    this._maxBufferPx,
  );

  public ngOnChanges(): void {
    this.scrollStrategy.updateBuffers(this._estimatedItemSize, this._minBufferPx, this._maxBufferPx);
  }
}
