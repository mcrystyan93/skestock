import { Component, effect, inject, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { ItemImportReviewState } from '../../../services/item-import-review.store';
import { ItemImportReviewTable } from './item-import-review-table';
import { ErrorAlert } from '@ske/shared/errors';
import { isNil } from 'lodash-es';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzTabComponent, NzTabsComponent } from 'ng-zorro-antd/tabs';
import { NzTimelineComponent, NzTimelineItemComponent } from 'ng-zorro-antd/timeline';
import { DatePipe } from '@angular/common';
import {
  ITEM_IMPORT_BATCH_STATUS_COLORS,
  ITEM_IMPORT_BATCH_STATUS_LABELS
} from '@ske/models';

@Component({
  imports: [FormsModule, NzButtonComponent, NzModalFooterDirective,
    NzModalTitleDirective, NzSpaceComponent, NzSpaceItemDirective, ItemImportReviewTable, ErrorAlert, NzTagComponent, NzTypographyComponent, NzIconDirective, NzTabComponent, NzTabsComponent, NzTimelineComponent, NzTimelineItemComponent, DatePipe],
  selector: 'ske-item-import-review-modal',
  templateUrl: './item-import-review-modal.html',
  providers: [ItemImportReviewState]
})
export class ItemImportReviewModal {
  private readonly _modalRef = inject(NzModalRef);
  private readonly _importId = inject<string>(NZ_MODAL_DATA);
  private readonly _linesTable = viewChild(ItemImportReviewTable);

  public readonly store = inject(ItemImportReviewState);
  public readonly statusLabels = ITEM_IMPORT_BATCH_STATUS_LABELS;
  public readonly statusColors = ITEM_IMPORT_BATCH_STATUS_COLORS;
  public readonly historyStatusLabels: Record<string, string> = {
    created: 'Creat',
    processing: 'Se proceseaza',
    completed: 'Procesat',
    failed: 'Esuat',
    confirmed: 'Confirmat'
  };

  private readonly _closeEffect = effect(() => {
    if (this.store.confirmation())
      this._modalRef.close(this.store.confirmation());
  });

  constructor() {
    this.store.load(this._importId);
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
