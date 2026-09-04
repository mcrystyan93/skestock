import { Component, input } from '@angular/core';
import { GoodsReceiptImportsList } from '@ske/shared/goods-receipt-imports';

@Component({
  imports: [
    GoodsReceiptImportsList
  ],
  selector: 'ske-school-class-overview-goods-receipt-imports-tab',
  styles: ``,
  template: `
    <ske-goods-receipt-imports-list [classId]="classId()" />
  `
})
export class GoodsReceiptImportsTab {
  public readonly classId = input.required<string | null>();
}
