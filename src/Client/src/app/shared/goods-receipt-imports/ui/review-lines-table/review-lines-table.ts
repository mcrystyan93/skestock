import { Component, computed, input, linkedSignal, output } from '@angular/core';
import { applyEach, disabled, form, required, schema, submit, validate } from '@angular/forms/signals';
import { NzTableModule } from 'ng-zorro-antd/table';
import { LineMutationEvent, ReviewEditableLine } from '../../services/review.store';
import { ReviewLine } from './review-line/review-line';


@Component({
  imports: [
    NzTableModule,
    ReviewLine
  ],
  selector: 'ske-goods-receipt-import-review-lines-table',
  styles: ``,
  templateUrl: './review-lines-table.html'
})
export class ReviewLinesTable {

  public readonly lines = input.required<ReviewEditableLine[]>();
  public readonly loading = input<boolean>();

  /** Structural mutations are owned by the store; emit the current edited working copy with them. */
  public readonly splitLine = output<LineMutationEvent>();
  public readonly removeLine = output<LineMutationEvent>();

  private readonly _linesModel = linkedSignal<ReviewEditableLine[], ReviewLinesFormModel>({
    source: () => this.lines(),
    computation: (lines, previous) => {
      const previousLines = previous?.value.lines;

      // A row count change means a structural mutation (split/remove) happened in the store, which
      // rebuilds from `store.lines` and would drop the in-progress edits held only in this form.
      // Carry the previous edited row over by `rowId` (keeping the store-recomputed `quantity`) so
      // surviving rows preserve their edits; split-remainder rows fall back to the store row.
      const structurallyChanged = !!previousLines && previousLines.length !== lines.length;

      if (!structurallyChanged)
        return { lines: lines.map((line) => ({ ...line })) };

      const previousByRowId = new Map(previousLines!.map((line) => [line.rowId, line]));

      return {
        lines: lines.map((line) => {
          const previousLine = previousByRowId.get(line.rowId);

          return previousLine ? { ...previousLine, quantity: line.quantity } : { ...line };
        })
      };
    }
  });

  private readonly _linesSchema = schema<ReviewLinesFormModel>((schemaPath) => {
    applyEach(schemaPath.lines, (line) => {
      required(line.item, {
        message: 'Selectati sau creati un produs.'
      });
      required(line.location, {
        message: 'Locatia este obligatorie.'
      });
      validate(line.quantity, ({ valueOf }) =>
        valueOf(line.quantity) <= 0 ? {
          kind: 'min',
          message: 'Cantitatea trebuie sa fie mai mare ca 0.'
        } : undefined
      );
      disabled(line.expiryDate, { when: ({ valueOf }) => !valueOf(line.item)?.isPerishable });
      required(line.expiryDate, {
        when: ({ valueOf }) => !!valueOf(line.item)?.isPerishable,
        message: 'Data expirarii este obligatorie pentru produse perisabile.'
      });
    });
  });

  public readonly linesForm = form(this._linesModel, this._linesSchema);

  public groupTotals = computed(() => {
    const totals = new Map<number, { quantity: number, originalQuantity: number }>();
    const lines = this._linesModel().lines;

    for (const line of lines) {
      const currentTotal = totals.get(line.sourceLineIndex) ?? { quantity: 0, originalQuantity: line.originalQuantity };
      totals.set(line.sourceLineIndex, {
        quantity: currentTotal.quantity + line.quantity,
        originalQuantity: currentTotal.originalQuantity
      });
    }

    return totals;
  });

  public canRemoveMap = computed(() => {
    const removeMap = new Map<number, boolean>();
    const lines = this._linesModel().lines;

    for (const line of lines) {
      const count = lines.filter((l) => l.sourceLineIndex === line.sourceLineIndex).length;
      removeMap.set(line.sourceLineIndex, count > 1);
    }

    return removeMap;
  });

  public async submit(): Promise<ReviewLinesSubmit> {
    let isValid = false;
    let lines: ReviewEditableLine[] = [];
    isValid = await submit(this.linesForm, async (_) => {
      lines = this.linesForm.lines().value();
    });

    return { isValid, lines };
  }
}

export type ReviewLinesSubmit = {
  isValid: boolean;
  lines: ReviewEditableLine[];
};

export type ReviewLinesFormModel = {
  lines: ReviewEditableLine[];
}
