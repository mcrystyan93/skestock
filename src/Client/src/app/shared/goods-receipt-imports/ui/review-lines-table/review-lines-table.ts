import { Component, computed, input, linkedSignal, output } from '@angular/core';
import { applyEach, disabled, form, FormField, required, schema, submit, validate } from '@angular/forms/signals';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzInputNumberComponent } from 'ng-zorro-antd/input-number';
import { NzDatePickerComponent } from 'ng-zorro-antd/date-picker';
import { ItemDropdown } from '@ske/shared/items';
import { LocationDropdown } from '@ske/shared/locations';
import { ItemDto } from '@ske/models';
import { LineMutationEvent, ReviewEditableLine } from '../../services/review.store';
import { NzFormControlComponent, NzFormItemComponent } from 'ng-zorro-antd/form';
import { NzInputAddonBeforeDirective, NzInputPrefixDirective } from 'ng-zorro-antd/input';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';

const ITEM_DROPDOWN_PLACEHOLDER = 'Creaza sau alege un produs';

@Component({
  imports: [
    NzTableModule,
    NzTagComponent,
    NzIconDirective,
    NzButtonComponent,
    NzInputNumberComponent,
    NzDatePickerComponent,
    FormField,
    ItemDropdown,
    LocationDropdown,
    NzFormItemComponent,
    NzFormControlComponent,
    NzTooltipDirective,
    NzInputAddonBeforeDirective
  ],
  selector: 'ske-goods-receipt-import-review-lines-table',
  styles: ``,
  templateUrl: './review-lines-table.html'
})
export class ReviewLinesTable {
  public readonly itemPlaceholder = ITEM_DROPDOWN_PLACEHOLDER;

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

  public readonly isQuantityOver = computed(() => {
    const totals = this.groupTotals();

    // a new map where the key is the sourceLineIndex and the value is a boolean indicating if the total quantity exceeds the original quantity
    const overMap = new Map<number, boolean>();

    for (const [sourceLineIndex, { quantity, originalQuantity }] of totals.entries()) {
      overMap.set(sourceLineIndex, quantity > originalQuantity);
    }

    return overMap;
  });

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

  public canRemove(line: ReviewEditableLine): boolean {
    return this._linesModel().lines.filter((l) => l.sourceLineIndex === line.sourceLineIndex).length > 1;
  }

  public createPrefill(line: ReviewEditableLine): Partial<ItemDto> {
    return {
      name: line.extractedName ?? '',
      sku: line.extractedSku ?? '',
      unit: line.extractedUnit ?? ''
    };
  }

  public onSplit(rowId: string) {
    this.splitLine.emit({ rowId });
  }

  public onRemove(rowId: string) {
    this.removeLine.emit({ rowId });
  }

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
