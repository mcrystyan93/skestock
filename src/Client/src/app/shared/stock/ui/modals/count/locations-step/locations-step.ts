import { Component, computed, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdjustmentReason, ADJUSTMENT_REASON_OPTIONS } from '@ske/models';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { NzFormControlComponent, NzFormItemComponent, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzOptionComponent, NzSelectComponent } from 'ng-zorro-antd/select';
import { CHECKABLE_CARD_CLASSES } from '../../../../../order-lists/ui/modals/wizard/steps/card-classes';

export type StockCountLocation = {
  id: string;
  name: string;
  /** Rows with stock at this location. */
  itemCount: number;
};

/** Step 1: adjustment reason for the whole count and the locations to walk through. */
@Component({
  imports: [
    FormsModule,
    NzAlertComponent,
    NzButtonComponent,
    NzIconDirective,
    NzFormItemComponent,
    NzFormLabelComponent,
    NzFormControlComponent,
    NzSelectComponent,
    NzOptionComponent
  ],
  selector: 'ske-stock-count-locations-step',
  templateUrl: './locations-step.html',
  host: { class: 'block' }
})
export class LocationsStep {
  public readonly locations = input.required<StockCountLocation[]>();
  public readonly selectedLocationIds = input.required<string[]>();
  public readonly reason = input.required<AdjustmentReason>();
  public readonly loading = input(false);

  public readonly reasonChange = output<AdjustmentReason>();
  public readonly selectedLocationIdsChange = output<string[]>();

  // A count only reconciles totals, so the damage/expiry/found reasons would mislabel its lines.
  protected readonly reasonOptions = ADJUSTMENT_REASON_OPTIONS.filter(
    (o) => o.value === AdjustmentReason.Adjustment || o.value === AdjustmentReason.Miscount
  );
  protected readonly cardClasses = CHECKABLE_CARD_CLASSES;

  protected readonly allSelected = computed(
    () => this.locations().length > 0 && this.selectedLocationIds().length === this.locations().length
  );

  protected toggleAll() {
    this.selectedLocationIdsChange.emit(this.allSelected() ? [] : this.locations().map((l) => l.id));
  }

  protected toggleLocation(id: string, checked: boolean) {
    const without = this.selectedLocationIds().filter((x) => x !== id);
    this.selectedLocationIdsChange.emit(checked ? [...without, id] : without);
  }
}
