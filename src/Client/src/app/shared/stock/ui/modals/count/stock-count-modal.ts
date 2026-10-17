import { Component, computed, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Events } from '@ngrx/signals/events';
import { ADJUSTMENT_REASON_OPTIONS } from '@ske/models';
import { ErrorAlert } from '@ske/shared/errors';
import { LayoutBreakpoint } from '@ske/shared/directives';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzCheckboxComponent } from 'ng-zorro-antd/checkbox';
import { NzFormControlComponent, NzFormItemComponent, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { NzInputNumberComponent } from 'ng-zorro-antd/input-number';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalService, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzOptionComponent, NzSelectComponent } from 'ng-zorro-antd/select';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzStepComponent, NzStepsComponent } from 'ng-zorro-antd/steps';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { filter } from 'rxjs';
import { StockCountState, stockCountApiEvents, stockCountRowKey } from '../../../services/stock-count.store';

export type StockCountModalData = {
  classId: string;
};

/** Three-step stock count wizard (Locații -> Articole -> Rezumat); closes with `true` once saved. */
@Component({
  imports: [
    FormsModule,
    DecimalPipe,
    NzModalTitleDirective,
    NzModalFooterDirective,
    NzButtonComponent,
    NzAlertComponent,
    NzCheckboxComponent,
    NzFormItemComponent,
    NzFormLabelComponent,
    NzFormControlComponent,
    NzInputNumberComponent,
    NzSelectComponent,
    NzOptionComponent,
    NzSpinComponent,
    NzStepsComponent,
    NzStepComponent,
    NzTableModule,
    NzTypographyComponent,
    ErrorAlert,
    LayoutBreakpoint
  ],
  selector: 'ske-stock-count-modal',
  templateUrl: './stock-count-modal.html',
  providers: [StockCountState]
})
export class StockCountModal {
  public readonly store = inject(StockCountState);
  protected readonly reasonOptions = ADJUSTMENT_REASON_OPTIONS;

  private readonly _modalData = inject<StockCountModalData>(NZ_MODAL_DATA);
  private readonly _nzModalRef = inject(NzModalRef);
  private readonly _nzModalService = inject(NzModalService);
  private readonly _nzMessageService = inject(NzMessageService);

  protected readonly stepIndex = computed(() => ['locations', 'items', 'summary'].indexOf(this.store.step()));
  protected readonly currentCount = computed(() => {
    const row = this.store.currentRow();
    return row ? (this.store.counts()[stockCountRowKey(row)] ?? row.quantity) : 0;
  });
  protected readonly allSelected = computed(
    () => this.store.locations().length > 0 && this.store.selectedLocationIds().length === this.store.locations().length
  );
  protected readonly someSelected = computed(() => this.store.selectedLocationIds().length > 0 && !this.allSelected());

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

  protected toggleAll(checked: boolean) {
    this.store.setSelectedLocationIds(checked ? this.store.locations().map((l) => l.id) : []);
  }

  protected toggleLocation(id: string, checked: boolean) {
    const without = this.store.selectedLocationIds().filter((x) => x !== id);
    this.store.setSelectedLocationIds(checked ? [...without, id] : without);
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
