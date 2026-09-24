import {Component, effect, inject, input, signal, untracked} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {form, FormField} from '@angular/forms/signals';
import {NzCardComponent} from 'ng-zorro-antd/card';
import {LocationDropdownValue} from '@ske/models';
import {ErrorAlert} from '@ske/shared/errors';
import {LocationDropdown} from '@ske/shared/locations';
import {ClassStatisticsStore} from '../../../services/class-statistics.store';
import {ClassStockByCategoryChart} from './class-stock-by-category-chart';
import {LoaderDirective} from '@ske/shared/loader';

type LocationFilterModel = {
  location: LocationDropdownValue;
};

@Component({
  imports: [
    FormsModule,
    FormField,
    NzCardComponent,
    LocationDropdown,
    ErrorAlert,
    ClassStockByCategoryChart,
    LoaderDirective
  ],
  selector: 'ske-selected-location-stock-card',
  template: `
    <nz-card class="min-h-110 flex-body has-chart"
             [nzExtra]="locationDropdownExtra"
             nzTitle="Stoc pe categorii în locația selectată">
      <ng-template #locationDropdownExtra>
        <ske-location-dropdown [formField]="locationForm.location"
                               [allowClear]="true"
                               [loadOnInit]="active()"
                               placeholder="Locație"
                               class="w-40 sm:w-48"/>
      </ng-template>

      <ng-container *skeLoader="store.locationLoading()">
        @if (store.locationProblemDetail() || store.locationValidationErrors()) {
          <ske-error-display [problemDetail]="store.locationProblemDetail()"
                             [validationErrors]="store.locationValidationErrors()"/>
        } @else {
          <ske-class-stock-by-category-chart [data]="store.locationChart()"
                                             [emptyMessage]="store.locationId()
                                             ? 'Nu există categorii cu stoc înregistrat pentru această clasă.'
                                             : 'Selectați o locație pentru a afișa stocul pe categorii.'"
                                             ariaLabel="Stoc pe categorii în locația selectată"/>
        }
      </ng-container>
    </nz-card>
  `,
  host: {
    class: 'block h-full min-w-0'
  }
})
export class SelectedLocationStockCard {
  public readonly classId = input.required<string | null>();
  public readonly active = input(false);
  public readonly store = inject(ClassStatisticsStore);

  private readonly _formModel = signal<LocationFilterModel>({location: null});
  public readonly locationForm = form(this._formModel);

  private readonly _loadLocationEffectRef = effect(() => {
    const classId = this.classId();
    const isActive = this.active();
    const locationId = this.locationForm.location().value()?.id ?? null;

    if (!isActive || !classId) {
      return;
    }

    untracked(() => this.store.loadLocation({classId, locationId}));
  });
}
