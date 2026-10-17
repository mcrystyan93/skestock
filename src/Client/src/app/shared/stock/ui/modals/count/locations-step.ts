import { Component, computed, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdjustmentReason, ADJUSTMENT_REASON_OPTIONS } from '@ske/models';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { NzCheckboxComponent } from 'ng-zorro-antd/checkbox';
import { NzFormControlComponent, NzFormItemComponent, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { NzOptionComponent, NzSelectComponent } from 'ng-zorro-antd/select';

export type StockCountLocation = {
  id: string;
  name: string;
};

/** Step 1: adjustment reason for the whole count and the locations to walk through. */
@Component({
  imports: [
    FormsModule,
    NzAlertComponent,
    NzCheckboxComponent,
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

  protected readonly reasonOptions = ADJUSTMENT_REASON_OPTIONS;

  protected readonly allSelected = computed(
    () => this.locations().length > 0 && this.selectedLocationIds().length === this.locations().length
  );
  protected readonly someSelected = computed(() => this.selectedLocationIds().length > 0 && !this.allSelected());

  protected toggleAll(checked: boolean) {
    this.selectedLocationIdsChange.emit(checked ? this.locations().map((l) => l.id) : []);
  }

  protected toggleLocation(id: string, checked: boolean) {
    const without = this.selectedLocationIds().filter((x) => x !== id);
    this.selectedLocationIdsChange.emit(checked ? [...without, id] : without);
  }
}
