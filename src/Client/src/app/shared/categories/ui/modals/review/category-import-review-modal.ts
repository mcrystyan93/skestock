import { DatePipe } from '@angular/common';
import { Component, effect, inject, viewChild } from '@angular/core';
import {
  NZ_MODAL_DATA,
  NzModalFooterDirective,
  NzModalRef,
  NzModalTitleDirective
} from 'ng-zorro-antd/modal';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { ErrorAlert } from '@ske/shared/errors';
import { CategoryImportReviewState } from '../../../services/category-import-review.store';
import { CategoryImportReviewTable } from './category-import-review-table';
import {
  CATEGORY_IMPORT_BATCH_STATUS_COLORS,
  CATEGORY_IMPORT_BATCH_STATUS_LABELS
} from '@ske/models';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzTabComponent, NzTabsComponent } from 'ng-zorro-antd/tabs';
import { NzTimelineComponent, NzTimelineItemComponent } from 'ng-zorro-antd/timeline';

@Component({
  imports: [
    NzModalTitleDirective,
    NzModalFooterDirective,
    NzButtonComponent,
    NzSpaceComponent,
    NzSpaceItemDirective,
    ErrorAlert,
    CategoryImportReviewTable,
    NzTagComponent,
    NzTypographyComponent,
    NzIconDirective,
    NzTabComponent,
    NzTabsComponent,
    NzTimelineComponent,
    NzTimelineItemComponent,
    DatePipe
  ],
  selector: 'ske-category-import-review-modal',
  templateUrl: './category-import-review-modal.html',
  providers: [CategoryImportReviewState]
})
export class CategoryImportReviewModal {
  private readonly _modalRef = inject(NzModalRef);
  private readonly _importId = inject<string>(NZ_MODAL_DATA);
  private readonly _linesTable = viewChild(CategoryImportReviewTable);

  public readonly store = inject(CategoryImportReviewState);
  public readonly statusLabels = CATEGORY_IMPORT_BATCH_STATUS_LABELS;
  public readonly statusColors = CATEGORY_IMPORT_BATCH_STATUS_COLORS;
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

  public close() {
    this._modalRef.close();
  }

  public async save() {
    const linesTable = this._linesTable();

    if (!linesTable)
      return;

    const { isValid, lines } = await linesTable.submit();

    if (!isValid)
      return;

    this.store.confirm(lines);
  }
}
