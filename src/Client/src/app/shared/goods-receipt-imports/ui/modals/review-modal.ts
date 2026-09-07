import { Component, effect, inject, viewChild } from '@angular/core';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { isNil } from 'lodash-es';
import { ReviewStore } from '../../services/review.store';
import { ReviewLinesTable } from '../review-lines-table/review-lines-table';

export type ReviewModalData = {
  importId: string;
};

@Component({
  imports: [
    NzModalTitleDirective,
    NzModalFooterDirective,
    NzButtonComponent,
    NzSpaceComponent,
    NzSpaceItemDirective,
    NzAlertComponent,
    ReviewLinesTable
  ],
  selector: 'ske-goods-receipt-import-review-modal',
  styles: ``,
  templateUrl: './review-modal.html',
  providers: [ReviewStore]
})
export class ReviewModal {
  private readonly _modalData = inject<ReviewModalData>(NZ_MODAL_DATA);
  private readonly _nzModalRef = inject(NzModalRef);

  public readonly store = inject(ReviewStore);

  private readonly _linesTable = viewChild(ReviewLinesTable);

  private readonly _confirmedEffectRef = effect(() => {
    const receipt = this.store.confirmedReceipt();

    if (!receipt)
      return;

    this._nzModalRef.close({ receipt });
  });

  constructor() {
    this.store.load(this._modalData.importId);
  }

  public close() {
    this._nzModalRef.close();
  }

  public async save() {
    const linesTable = this._linesTable();

    if (isNil(linesTable))
      return;

    const { isValid, lines } = await linesTable.submit();

    if (!isValid)
      return;

    this.store.confirm(lines);
  }
}
