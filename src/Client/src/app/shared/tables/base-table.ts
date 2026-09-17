import { AfterViewInit, Component, DestroyRef, effect, ElementRef, inject, signal, viewChild } from '@angular/core';
import { NzTableComponent } from 'ng-zorro-antd/table';
import { isNil } from 'lodash-es';
import { TableDimensions } from './table-dimensions';

@Component({
  template: ''
})
export class BaseTable implements AfterViewInit {
  protected readonly _afterViewInit = signal(false);
  protected readonly _table = viewChild<NzTableComponent<unknown>>('table');

  protected readonly _elementRef: ElementRef<HTMLElement> = inject(ElementRef<HTMLElement>);
  protected readonly _destroyRef = inject(DestroyRef);

  public readonly tableDimensions = signal<TableDimensions>(initialTableDimensions);

  private readonly measureEffect = effect(() => {
    if (!this._afterViewInit()) return;

    this.measureHost();
  });

  public ngAfterViewInit(): void {
    this._afterViewInit.set(true);
  }

  private measureHost() {
    if (isNil(this._elementRef)) return;
    const element = this._elementRef.nativeElement as HTMLElement;
    const updateSize = () => {
      const tableHeaderContainer = element.querySelector('.ant-table-header') as HTMLElement | null;
      const headerHeight = Math.round(tableHeaderContainer?.getBoundingClientRect().height ?? 0);
      const availableHeight = Math.max(element.clientHeight - headerHeight, 0);

      this.tableDimensions.set({
        width: `${element.clientWidth - 30}px`,
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

const initialTableDimensions: TableDimensions = { width: '0px', height: '0px', isLoaded: false };
