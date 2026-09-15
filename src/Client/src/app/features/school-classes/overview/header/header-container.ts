import { Component, DestroyRef, inject, input, model } from '@angular/core';
import { SchoolClassOverviewStore } from '../../services/school-class-overview.store';
import { Header } from './header';
import { NzModalService } from 'ng-zorro-antd/modal';
import { AddGoodsReceiptModal } from '@ske/shared/goods-receipts';
import { ReviewModal, ReviewModalData } from '@ske/shared/goods-receipt-imports';
import { Router } from '@angular/router';

@Component({
  imports: [
    Header
  ],
  selector: 'ske-school-class-overview-header-container',
  styles: ``,
  templateUrl: './header-container.html',
  providers: [NzModalService]
})
export class HeaderContainer {
  public readonly selectedTabIndex = model<number>(0);
  public readonly classId = input.required<string | null>();
  public readonly store = inject(SchoolClassOverviewStore);

  private readonly _router = inject(Router);
  private readonly _nzModalService = inject(NzModalService);
  private readonly _destroyRef = inject(DestroyRef);

  public addGoodsReceipt() {
    this._nzModalService.create({
      nzContent: AddGoodsReceiptModal,
      nzData: {
        classId: this.classId()
      },
      nzCentered: true,
      nzClosable: false
    });
  }

  private openGoodsReceiptImportReview(importId: string) {
    this._nzModalService.create({
      nzContent: ReviewModal,
      nzData: <ReviewModalData>{importId},
      nzWidth: '90vw',
      nzCentered: true,
      nzMaskClosable: false
    });
  }

  public openAnalytics() {
    const classId = this.classId();
    if (classId) {
      void this._router.navigate(['/class-analytics'], { queryParams: { classId } });
    }
  }

  public close() {
    this._router.navigate(['school-classes']);
  }
}
