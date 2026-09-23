// noinspection ES6PreferShortImport

import { Component, effect, input, linkedSignal, output } from '@angular/core';
import { ItemImportReviewEditableLine } from '../../../services/item-import-review.store';
import { NzTableModule } from 'ng-zorro-antd/table';
import { applyEach, form, required, schema, submit } from '@angular/forms/signals';
import { ItemImportReviewLineRow } from './item-import-review-line-row';
import { BaseTable } from '@ske/shared/tables';

@Component({
  imports: [NzTableModule, ItemImportReviewLineRow],
  selector: 'ske-item-import-review-table',
  templateUrl: './item-import-review-table.html',
  host: { class: 'absolute block inset-0' }
})
export class ItemImportReviewTable extends BaseTable {
  public readonly lines = input.required<ItemImportReviewEditableLine[]>();
  public readonly loading = input.required<boolean>();

  public readonly formChanges = output<ItemImportReviewEditableLine[]>();

  private _initialFormChange = true;

  private readonly _formChangeEffect = effect(() => {
    const linesForm = this.linesForm();
    const lines = linesForm.value().lines;

    if (this._initialFormChange) {
      this._initialFormChange = false;
      return;
    }

    this.formChanges.emit(lines);
  });

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

  constructor() {
    super();
  }

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
