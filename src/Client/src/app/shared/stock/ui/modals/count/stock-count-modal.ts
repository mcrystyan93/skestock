import { Component, computed, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Events } from '@ngrx/signals/events';
import { ErrorAlert } from '@ske/shared/errors';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalService, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { filter } from 'rxjs';
import { StockCountState, stockCountApiEvents, stockCountRowKey } from '../../../services/stock-count.store';
import { ItemStep } from './item-step/item-step';
import { LocationsStep } from './locations-step/locations-step';
import { StockCountFooter } from './stock-count-footer/stock-count-footer';
import { StockCountSteps } from './stock-count-steps/stock-count-steps';
import { SummaryStep } from './summary-step/summary-step';

export type StockCountModalData = {
  classId: string;
};

/**
 * Container of the three-step stock count wizard (Locații -> Articole -> Rezumat): owns the store, the
 * modal ref and the close/confirm flow, and closes with `true` once saved. Rendering lives in the
 * presentational step/footer components.
 */
@Component({
  imports: [
    NzModalTitleDirective,
    NzModalFooterDirective,
    NzSpinComponent,
    NzTypographyComponent,
    ErrorAlert,
    StockCountSteps,
    LocationsStep,
    ItemStep,
    SummaryStep,
    StockCountFooter
  ],
  selector: 'ske-stock-count-modal',
  templateUrl: './stock-count-modal.html',
  providers: [StockCountState]
})
export class StockCountModal {
  public readonly store = inject(StockCountState);

  private readonly _modalData = inject<StockCountModalData>(NZ_MODAL_DATA);
  private readonly _nzModalRef = inject(NzModalRef);
  private readonly _nzModalService = inject(NzModalService);
  private readonly _nzMessageService = inject(NzMessageService);

  protected readonly currentCount = computed(() => {
    const row = this.store.currentRow();
    return row ? (this.store.counts()[stockCountRowKey(row)] ?? row.quantity) : 0;
  });

  constructor() {
    this.store.load(this._modalData.classId);

    inject(Events)
      .on(stockCountApiEvents.countSuccess)
      .pipe(
        filter(() => this.store.completed()),
        takeUntilDestroyed(inject(DestroyRef))
      )
      .subscribe(() => {
        this._nzMessageService.success('Inventarul de stoc a fost salvat cu succes!');
        this._nzModalRef.close(true);
      });
  }

  public close() {
    if (this.store.stockCountLoading()) return;

    if (Object.keys(this.store.counts()).length === 0) {
      this._nzModalRef.close();
      return;
    }

    this._nzModalService.confirm({
      nzTitle: 'Renunțați la modificări?',
      nzContent: 'Modificările nesalvate vor fi pierdute.',
      nzOkText: 'Renunță',
      nzCancelText: 'Continuă editarea',
      nzOnOk: () => this._nzModalRef.close()
    });
  }
}
