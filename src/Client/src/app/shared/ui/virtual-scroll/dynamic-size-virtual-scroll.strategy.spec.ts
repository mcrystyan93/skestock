import { ListRange } from '@angular/cdk/collections';
import { CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import { DynamicSizeVirtualScrollStrategy } from './dynamic-size-virtual-scroll.strategy';

class FakeViewport {
  public dataLength = 0;
  public viewportSize = 250;
  public scrollOffset = 0;
  public totalContentSize = 0;
  public renderedRange: ListRange = { start: 0, end: 0 };
  public renderedContentOffset = 0;
  public scrolledTo: number[] = [];

  public getDataLength(): number {
    return this.dataLength;
  }
  public getViewportSize(): number {
    return this.viewportSize;
  }
  public measureScrollOffset(): number {
    return this.scrollOffset;
  }
  public setTotalContentSize(size: number): void {
    this.totalContentSize = size;
  }
  public setRenderedRange(range: ListRange): void {
    this.renderedRange = range;
  }
  public getRenderedRange(): ListRange {
    return this.renderedRange;
  }
  public setRenderedContentOffset(offset: number): void {
    this.renderedContentOffset = offset;
  }
  public scrollToOffset(offset: number): void {
    this.scrolledTo.push(offset);
    this.scrollOffset = offset;
  }
}

function attach(viewport: FakeViewport, estimate = 100, minBuffer = 0, maxBuffer = 0): DynamicSizeVirtualScrollStrategy {
  const strategy = new DynamicSizeVirtualScrollStrategy(estimate, minBuffer, maxBuffer);
  strategy.attach(viewport as unknown as CdkVirtualScrollViewport);
  return strategy;
}

describe('DynamicSizeVirtualScrollStrategy', () => {
  it('seeds total size from the estimate and renders enough items to fill the viewport', () => {
    const viewport = new FakeViewport();
    viewport.dataLength = 5;

    attach(viewport);

    expect(viewport.totalContentSize).toBe(500);
    expect(viewport.renderedRange).toEqual({ start: 0, end: 3 });
    expect(viewport.renderedContentOffset).toBe(0);
  });

  it('uses measured heights to recompute the total content size', () => {
    const viewport = new FakeViewport();
    viewport.dataLength = 5;
    const strategy = attach(viewport);

    strategy.updateItemSize(0, 200);
    strategy.updateItemSize(1, 300);

    // 200 + 300 + 100 + 100 + 100
    expect(viewport.totalContentSize).toBe(800);
  });

  it('offsets the rendered window when scrolled past measured items', () => {
    const viewport = new FakeViewport();
    viewport.dataLength = 5;
    const strategy = attach(viewport);
    strategy.updateItemSize(0, 200);
    strategy.updateItemSize(1, 300);

    viewport.scrollOffset = 550;
    strategy.onContentScrolled();

    // index 0 (200) + index 1 (300) = 500 <= 550, so item 2 is the first rendered.
    expect(viewport.renderedRange.start).toBe(2);
    expect(viewport.renderedContentOffset).toBe(500);
  });

  it('compensates the scroll offset when an item above the viewport grows', () => {
    const viewport = new FakeViewport();
    viewport.dataLength = 10;
    const strategy = attach(viewport);

    // Scroll down so the first items are above the fold.
    viewport.scrollOffset = 500;
    strategy.onContentScrolled();

    // Item 0 starts at offset 0 (< 500) and grows by 150 -> offset must shift to avoid a jump.
    strategy.updateItemSize(0, 250);

    expect(viewport.scrolledTo).toContain(650);
  });

  it('does not compensate when a visible/below item changes', () => {
    const viewport = new FakeViewport();
    viewport.dataLength = 10;
    const strategy = attach(viewport);

    viewport.scrollOffset = 0;
    strategy.onContentScrolled();
    viewport.scrolledTo = [];

    strategy.updateItemSize(2, 260);

    expect(viewport.scrolledTo).toEqual([]);
  });

  it('scrollToIndex targets the accumulated offset of the index', () => {
    const viewport = new FakeViewport();
    viewport.dataLength = 5;
    const strategy = attach(viewport);
    strategy.updateItemSize(0, 200);
    strategy.updateItemSize(1, 300);
    viewport.scrolledTo = [];

    strategy.scrollToIndex(2, 'auto');

    expect(viewport.scrolledTo).toContain(500);
  });
});
