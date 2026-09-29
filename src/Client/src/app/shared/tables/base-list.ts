import { Component, effect, input, output, viewChild } from '@angular/core';
import { CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import { BasePaginationFilter } from '@ske/models';

@Component({
  template: ''
})
export class BaseList<T, K extends BasePaginationFilter> {
  public readonly items = input.required<Array<T>>();
  public readonly filter = input.required<K>();

  public readonly isReady = input(false);
  public readonly hasNextPage = input.required<boolean>();
  public readonly isLoadingMore = input.required<boolean>();

  public readonly onLoadMore = output<void>();

  private readonly _viewport = viewChild(CdkVirtualScrollViewport);

  private readonly _resizeEffect = effect((onCleanup) => {
    const viewport = this._viewport();
    if (!viewport || typeof ResizeObserver === 'undefined') return;
    const observer = new ResizeObserver(() => viewport.checkViewportSize());
    observer.observe(viewport.elementRef.nativeElement);
    onCleanup(() => observer.disconnect());
  });

  private readonly _fillViewportEffect = effect((onCleanup) => {
    const viewport = this._viewport();
    this.items();
    if (!viewport || !this.hasNextPage() || this.isLoadingMore()) return;

    const timer = setTimeout(() => {
      if (viewport.measureScrollOffset('bottom') <= SCROLL_THRESHOLD_PX && this.hasNextPage() && !this.isLoadingMore()) {
        this.onLoadMore.emit();
      }
    }, 50);
    onCleanup(() => clearTimeout(timer));
  });
  public readonly onFilterChange = output<K>();

  public onScroll(event: Event): void {
    const element = event.currentTarget as HTMLElement | null;
    if (!element) return;

    const distanceToBottom = element.scrollHeight - element.scrollTop - element.clientHeight;

    if (this.shouldLoadMore(distanceToBottom)) {
      this.onLoadMore.emit();
    }
  }

  protected shouldLoadMore(distanceToBottom: number): boolean {
    return distanceToBottom <= SCROLL_THRESHOLD_PX && this.hasNextPage() && !this.isLoadingMore();
  }
}

export const SCROLL_THRESHOLD_PX = 200;
