import { Component, input, linkedSignal, output } from '@angular/core';
import { LocationDropdownValue } from '@ske/models';
import { LocationDropdown } from '@ske/shared/locations';

@Component({
  imports: [LocationDropdown],
  selector: 'ske-item-location-cell',
  template: `
    <ske-location-dropdown [allowClear]="true"
                           placeholder="Fără locație"
                           [value]="value()"
                           (valueChange)="select($event)" />
  `
})
export class ItemLocationCell {
  public readonly locationId = input<string | null | undefined>();
  public readonly locationName = input<string | null | undefined>();

  public readonly locationChange = output<string | null>();

  // Stable reference per item state: the dropdown re-resolves its selection whenever the value reference changes.
  protected readonly value = linkedSignal<LocationDropdownValue>(() => {
    const id = this.locationId();

    return id ? { id, name: this.locationName() ?? undefined } : null;
  });

  protected select(location: LocationDropdownValue) {
    this.value.set(location);

    const id = location?.id ?? null;
    if (id !== (this.locationId() ?? null))
      this.locationChange.emit(id);
  }
}
