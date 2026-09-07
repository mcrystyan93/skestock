import { Component, DestroyRef, inject } from '@angular/core';
import { Table } from './table';
import { GoodsReceiptImportListStore } from '../../services/goods-receipt-import-list.store';
import { GoodsReceiptImportListItemDto } from '@ske/models';
import { FileStorageState } from '@ske/shared/storage';
import { NzModalService } from 'ng-zorro-antd/modal';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReviewModal, ReviewModalData } from '../modals/review-modal';

@Component({
  imports: [
    Table
  ],
  selector: 'ske-goods-receipt-imports-table-container',
  styles: ``,
  templateUrl: './table-container.html',
  host: {
    class: 'absolute block inset-0'
  },
  providers: [FileStorageState, NzModalService]
})
export class TableContainer {
  public readonly store = inject(GoodsReceiptImportListStore);
  public readonly fileStorageState = inject(FileStorageState);

  private readonly _nzModalService = inject(NzModalService);
  private readonly _destroyRef = inject(DestroyRef);

  public downloadFile(importDto: GoodsReceiptImportListItemDto) {
    this.fileStorageState.downloadFile(importDto.fileMetadataId);
  }

  public review(importDto: GoodsReceiptImportListItemDto) {
    const modalRef = this._nzModalService.create({
      nzContent: ReviewModal,
      nzData: <ReviewModalData>{ importId: importDto.id },
      nzWidth: '90vw',
      nzCentered: true,
      nzMaskClosable: false
    });

    modalRef.afterClose
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe((result) => {
        if (!result?.receipt)
          return;

        this.store.reload();
      });
  }
}

