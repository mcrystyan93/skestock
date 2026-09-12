import {Component, effect, linkedSignal, model, output, signal, untracked} from '@angular/core';
import {ClassAnalysisFilter, ClassAnalysisFilterFormData} from '@ske/models';
import {NzCardComponent} from 'ng-zorro-antd/card';
import {SchoolClassDropdown} from '@ske/shared/school-classes';
import {NzColDirective, NzRowDirective} from 'ng-zorro-antd/grid';
import {form, FormField} from '@angular/forms/signals';
import {NzFormControlComponent, NzFormDirective, NzFormItemComponent, NzFormLabelComponent} from 'ng-zorro-antd/form';
import {LocationDropdown} from '@ske/shared/locations';
import {NzDatePickerComponent} from 'ng-zorro-antd/date-picker';

@Component({
  selector: 'ske-class-analytics-filters',
  template: `
    <nz-card>
      <form nz-form
            nzNoColon
            nzLayout="vertical">
        <nz-row [nzGutter]="[16, 16]">
          <nz-col nzSpan="6">
            <nz-form-item nzLayout="vertical">
              <nz-form-label>Clasa</nz-form-label>
              <nz-form-control>
                <ske-school-class-dropdown [formField]="schoolAnalysisForm.schoolClass" />
              </nz-form-control>
            </nz-form-item>
          </nz-col>
          <nz-col nzSpan="6">
            <nz-form-item>
              <nz-form-label>Locație</nz-form-label>
              <nz-form-control>
                <ske-location-dropdown [formField]="schoolAnalysisForm.location" />
              </nz-form-control>
            </nz-form-item>
          </nz-col>
          <nz-col nzSpan="6">
            <nz-form-item>
              <nz-form-label>Data început costuri</nz-form-label>
              <nz-form-control>
                <nz-date-picker [formField]="schoolAnalysisForm.startDate"
                                class="w-full" />
              </nz-form-control>
            </nz-form-item>
          </nz-col>
          <nz-col nzSpan="6">
            <nz-form-item>
              <nz-form-label>Data sfârșit costuri</nz-form-label>
              <nz-form-control>
                <nz-date-picker [formField]="schoolAnalysisForm.endDate"
                                class="w-full" />
              </nz-form-control>
            </nz-form-item>
          </nz-col>
        </nz-row>
      </form>
    </nz-card>
  `,
  imports: [NzCardComponent, NzRowDirective, NzColDirective, SchoolClassDropdown, FormField,
    NzFormDirective, LocationDropdown, NzDatePickerComponent, NzFormItemComponent, NzFormLabelComponent,
    NzFormControlComponent]
})
export class ClassAnalyticsFilters {
  public readonly filter = model.required<ClassAnalysisFilter>();

  private _initialFormChangesDone = false;

  private readonly _formModel = linkedSignal({
    source: () => this.filter(),
    computation: (filter) => (<ClassAnalysisFilterFormData>{
      schoolClass: filter.schoolClass,
      location: filter.location,
      startDate: filter.startDate,
      endDate: filter.endDate
    })
  });

  public readonly schoolAnalysisForm = form(this._formModel);

  private readonly _formChangesEffect = effect(() => {
    const formValue = this.schoolAnalysisForm().value();
    const currentFilter = untracked(() => this.filter());

    if (!this._initialFormChangesDone) {
      this._initialFormChangesDone = true;
      return;
    }

    if (sameFilter(currentFilter, formValue))
      return;

    untracked(() => this.filter.set(formValue));
  });
}

function sameFilter(filter1: ClassAnalysisFilter, formData: ClassAnalysisFilterFormData): boolean {
  return filter1.schoolClass === formData.schoolClass &&
    filter1.location === formData.location &&
    filter1.startDate === formData.startDate &&
    filter1.endDate === formData.endDate;
}

