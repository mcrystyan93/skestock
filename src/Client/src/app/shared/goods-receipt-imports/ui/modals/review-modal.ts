import { Component, effect, inject, viewChild } from '@angular/core';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { isNil } from 'lodash-es';
import { buildConfirmRequest, ReviewStore } from '../../services/review.store';
import { ReviewLinesTable } from '../review-lines-table/review-lines-table';
import { ReviewInfo } from '../review-lines-table/review-info/review-info';
import { NzDividerComponent } from 'ng-zorro-antd/divider';
import { ErrorAlert } from '@ske/shared/errors';

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
    ReviewLinesTable,
    ReviewInfo,
    NzDividerComponent,
    ErrorAlert
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
  private readonly _reviewInfo = viewChild(ReviewInfo);

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
    const reviewInfo = this._reviewInfo();

    if (isNil(linesTable) || isNil(reviewInfo))
      return;

    const { isValid, lines } = await linesTable.submit();
    const { isValid: isReviewInfoValid, model: reviewInfoModel } = await reviewInfo.submit();

    if (!isValid || !isReviewInfoValid || isNil(reviewInfoModel))
      return;

    this.store.confirm(buildConfirmRequest(reviewInfoModel.supplierReference, reviewInfoModel.note, lines));
  }
}
