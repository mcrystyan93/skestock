import { Component, effect, inject, input, untracked } from '@angular/core';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { ErrorAlert } from '@ske/shared/errors';
import { GoodsReceiptImportLinesStore } from '../../services/goods-receipt-import-lines.store';
import { ImportLines } from './import-lines';

@Component({
  selector: 'ske-goods-receipt-import-lines-container',
  imports: [ImportLines, ErrorAlert, NzButtonComponent],
  providers: [GoodsReceiptImportLinesStore],
  templateUrl: './import-lines-container.html',
  host: { class: 'absolute inset-0 block overflow-auto' }
})
export class ImportLinesContainer {
  readonly goodsReceiptId = input.required<string>();
  readonly store = inject(GoodsReceiptImportLinesStore);

  constructor() {
    effect(() => {
      const receiptId = this.goodsReceiptId();
      untracked(() => this.store.load(receiptId));
    });
  }
}
