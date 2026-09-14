import { Component, input, linkedSignal } from '@angular/core';
import { applyEach, form, required, schema, submit } from '@angular/forms/signals';
import { NzTableModule } from 'ng-zorro-antd/table';
import { CategoryImportReviewEditableLine } from '../../../services/category-import-review.store';
import { CategoryImportReviewLineRow } from './category-import-review-line-row';

@Component({
  imports: [NzTableModule, CategoryImportReviewLineRow],
  selector: 'ske-category-import-review-table',
  templateUrl: './category-import-review-table.html'
})
export class CategoryImportReviewTable {
  public readonly lines = input.required<CategoryImportReviewEditableLine[]>();
  public readonly loading = input.required<boolean>();

  private readonly _linesModel = linkedSignal({
    source: () => this.lines(),
    computation: (lines) => (<CategoryReviewLinesFormModel>{ lines: lines.map((line) => ({ ...line })) })
  });

  private readonly _lineSchema = schema<CategoryImportReviewEditableLine>((schemaPath) => {
    required(schemaPath.category, { message: 'Selectați o categorie.' });
  });

  public readonly linesForm = form(this._linesModel, (schemaPath) => {
    applyEach(schemaPath.lines, this._lineSchema);
  });

  public async submit(): Promise<CategoryReviewLinesSubmitResult> {
    let isValid = false;
    let lines: CategoryImportReviewEditableLine[] = [];

    isValid = await submit(this.linesForm, async () => {
      lines = this.linesForm().value().lines;
    });

    return { isValid, lines };
  }
}

type CategoryReviewLinesFormModel = {
  lines: CategoryImportReviewEditableLine[];
};

export type CategoryReviewLinesSubmitResult = {
  isValid: boolean;
  lines: CategoryImportReviewEditableLine[];
};
