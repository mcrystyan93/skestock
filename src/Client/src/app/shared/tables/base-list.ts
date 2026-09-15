import { Component, input, output } from '@angular/core';
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
