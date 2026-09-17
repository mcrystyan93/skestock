import { DestroyRef, Directive, ElementRef, effect, inject, input } from '@angular/core';
import { VIRTUAL_SCROLL_STRATEGY } from '@angular/cdk/scrolling';
import { DynamicSizeVirtualScrollStrategy } from './dynamic-size-virtual-scroll.strategy';

/**
 * Measures the host element of a `*cdkVirtualFor` item and reports its height to the active
 * {@link DynamicSizeVirtualScrollStrategy}.
 *
 * The data index is bound through the `skeVirtualItem` input (e.g. `[skeVirtualItem]="index"`).
 * Because CDK recycles item views, the height is re-reported both when the element resizes
 * (via {@link ResizeObserver}) and when the bound index changes.
 */
@Directive({
  selector: '[skeVirtualItem]',
})
export class DynamicVirtualScrollItem {
  public readonly index = input.required<number>({ alias: 'skeVirtualItem' });

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private readonly strategy = inject(VIRTUAL_SCROLL_STRATEGY, { optional: true });
  private readonly observer = new ResizeObserver(() => this.report(this.index()));

  constructor() {
    this.observer.observe(this.host);
    effect(() => this.report(this.index()));
    inject(DestroyRef).onDestroy(() => this.observer.disconnect());
  }

  private report(index: number): void {
    if (!(this.strategy instanceof DynamicSizeVirtualScrollStrategy)) {
      return;
    }

    const height = this.host.offsetHeight;
    if (height > 0) {
      this.strategy.updateItemSize(index, height);
    }
  }
}
