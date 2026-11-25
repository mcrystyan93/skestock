import { Component, inject, signal } from '@angular/core';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { StockItemDto, StockItemGroup } from '@ske/models';
import { CHECKABLE_CARD_CLASSES } from '../../../../ui/card-classes';

/** Asks which location of a multi-location item to work on; closes with the chosen stock line. */
@Component({
  imports: [
    NzModalTitleDirective,
    NzModalFooterDirective,
    NzButtonComponent,
    NzIconDirective,
    NzTagComponent,
    NzTypographyComponent
  ],
  selector: 'ske-stock-location-picker-modal',
  templateUrl: './stock-location-picker-modal.html'
})
export class StockLocationPickerModal {
  public readonly modalData = inject<StockLocationPickerModalData>(NZ_MODAL_DATA);

  protected readonly cardClasses = CHECKABLE_CARD_CLASSES;
  protected readonly selected = signal<StockItemDto | null>(null);

  private readonly _nzModalRef = inject(NzModalRef<StockLocationPickerModal, StockItemDto>);

  protected close(): void {
    this._nzModalRef.close();
  }

  protected confirm(): void {
    const selected = this.selected();
    if (selected) this._nzModalRef.close(selected);
  }
}

export type StockLocationPickerModalData = {
  group: StockItemGroup;
};
