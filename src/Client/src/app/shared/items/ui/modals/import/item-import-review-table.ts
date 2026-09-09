import { Component, input, output } from '@angular/core';
import { ItemImportReviewEditableLine } from '../../../services/item-import-review.store';
import { NzInputDirective } from 'ng-zorro-antd/input';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagComponent } from 'ng-zorro-antd/tag';

@Component({
  imports: [NzInputDirective, NzTableModule, NzTagComponent],
  selector: 'ske-item-import-review-table',
  templateUrl: './item-import-review-table.html'
})
export class ItemImportReviewTable {
  public readonly lines = input.required<ItemImportReviewEditableLine[]>();
  public readonly linesChange = output<ItemImportReviewEditableLine[]>();

  public update(rowId: string, property: keyof ItemImportReviewEditableLine, value: string | boolean) {
    this.linesChange.emit(this.lines().map((line) =>
      line.rowId === rowId ? { ...line, [property]: value } : line));
  }
}
