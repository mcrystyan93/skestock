import { Component, computed, input, linkedSignal } from '@angular/core';
import { applyEach, form, required, schema, submit } from '@angular/forms/signals';
import { NzTableModule } from 'ng-zorro-antd/table';
// noinspection ES6PreferShortImport
import { CategoryImportReviewEditableLine } from '../../../services/category-import-review.store';
import { CategoryImportReviewLineRow } from './category-import-review-line-row';
import { NzAlertComponent, NzAlertType } from 'ng-zorro-antd/alert';
import { isEmpty, isNil } from 'lodash-es';

@Component({
  imports: [NzTableModule, CategoryImportReviewLineRow, NzAlertComponent],
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

  public readonly alertMessage = computed<{ text: string, type: NzAlertType }>(() => {
    const lines = this.linesForm().value()?.lines;

    if (isNil(lines) || isEmpty(lines))
      return { text: 'Nu există linii de revizuit.', type: 'warning' };

    const nrOfMatchedLines = lines.filter(l => !!l.category?.id).length;

    if (nrOfMatchedLines === lines.length)
      return { text: 'Toate categoriile au fost potrivite', type: 'success' };

    return {
      text: `${nrOfMatchedLines} din ${lines.length} categorii au fost potrivite. ${lines.length - nrOfMatchedLines} necesita atenție.`,
      type: 'warning'
    };
  });
}

type CategoryReviewLinesFormModel = {
  lines: CategoryImportReviewEditableLine[];
};

export type CategoryReviewLinesSubmitResult = {
  isValid: boolean;
  lines: CategoryImportReviewEditableLine[];
};
