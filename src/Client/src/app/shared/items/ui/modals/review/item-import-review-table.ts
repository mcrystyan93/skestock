// noinspection ES6PreferShortImport

import { Component, input, linkedSignal } from '@angular/core';
import { ItemImportReviewEditableLine } from '../../../services/item-import-review.store';
import { NzTableModule } from 'ng-zorro-antd/table';
import { applyEach, form, required, schema, submit } from '@angular/forms/signals';
import { ItemImportReviewLineRow } from './item-import-review-line-row';

@Component({
  imports: [NzTableModule, ItemImportReviewLineRow],
  selector: 'ske-item-import-review-table',
  templateUrl: './item-import-review-table.html'
})
export class ItemImportReviewTable {
  public readonly lines = input.required<ItemImportReviewEditableLine[]>();
  public readonly loading = input.required<boolean>();

  private readonly _linesModel = linkedSignal({
    source: () => this.lines(),
    computation: (lines) => (<ReviewLinesFormModel>{ lines: lines.map((line) => ({ ...line })) })
  });

  private readonly _lineSchema = schema<ItemImportReviewEditableLine>((schemaPath) => {
    required(schemaPath.item, { message: 'Selectați un produs.' });
  });

  public readonly linesForm = form(this._linesModel, (schemaPath) => {
    applyEach(schemaPath.lines, this._lineSchema);
  });

  public async submit(): Promise<ReviewLinesSubmitResult> {
    let isValid = false;
    let lines: ItemImportReviewEditableLine[] = [];

    isValid = await submit(this.linesForm, async (_) => {
      lines = this.linesForm().value().lines;
    });

    return { isValid, lines };
  }
}

export type ReviewLinesFormModel = {
  lines: ItemImportReviewEditableLine[];
}
export type ReviewLinesSubmitResult = {
  isValid: boolean;
  lines: ItemImportReviewEditableLine[];
}
