import {Component, DestroyRef, inject, input, model} from '@angular/core';
import {SchoolClassOverviewStore} from '../../services/school-class-overview.store';
import {Header} from './header';
import {NzModalService} from 'ng-zorro-antd/modal';
import {AddGoodsReceiptModal} from '@ske/shared/goods-receipts';
import {ReviewModal, ReviewModalData} from '@ske/shared/goods-receipt-imports';
import {GoodsReceiptImportDto} from '@ske/models';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {Router} from '@angular/router';

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
    const modalRef = this._nzModalService.create({
      nzContent: AddGoodsReceiptModal,
      nzData: {
        classId: this.classId()
      },
      nzCentered: true,
      nzClosable: false
    });

    modalRef.afterClose.pipe(takeUntilDestroyed(this._destroyRef)).subscribe((result: unknown) => {
      if (!Array.isArray(result))
        return;

      result
        .filter((item): item is GoodsReceiptImportDto => this.isGoodsReceiptImportResult(item))
        .forEach((item) => this.openGoodsReceiptImportReview(item.id));
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

  private isGoodsReceiptImportResult(result: unknown): result is GoodsReceiptImportDto {
    return typeof result === 'object'
      && result !== null
      && 'id' in result
      && typeof result.id === 'string';
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
