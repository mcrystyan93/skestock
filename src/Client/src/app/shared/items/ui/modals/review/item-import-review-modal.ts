import { Component, effect, inject, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { ItemImportReviewState } from '../../../services/item-import-review.store';
import { ItemImportReviewTable } from './item-import-review-table';
import { ErrorAlert } from '@ske/shared/errors';
import { isNil } from 'lodash-es';

@Component({
  imports: [FormsModule, NzButtonComponent, NzModalFooterDirective,
    NzModalTitleDirective, NzSpaceComponent, NzSpaceItemDirective, ItemImportReviewTable, ErrorAlert],
  selector: 'ske-item-import-review-modal',
  templateUrl: './item-import-review-modal.html',
  providers: [ItemImportReviewState]
})
export class ItemImportReviewModal {
  private readonly _modalRef = inject(NzModalRef);
  private readonly _data = inject<{ importId: string }>(NZ_MODAL_DATA);
  private readonly _linesTable = viewChild(ItemImportReviewTable);

  public readonly store = inject(ItemImportReviewState);
  private readonly _closeEffect = effect(() => {
    if (this.store.confirmation())
      this._modalRef.close(this.store.confirmation());
  });

  constructor() {
    this.store.load(this._data.importId);
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

  public close() {
    this._modalRef.close();
  }
}
