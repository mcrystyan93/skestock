import { Component, input, linkedSignal } from '@angular/core';
import { ClassStatus, CLASS_STATUS_OPTIONS, SchoolClassDto } from '@ske/models';
import { form, FormField, maxLength, required, submit, validate } from '@angular/forms/signals';
import { NzFormControlComponent, NzFormDirective, NzFormItemComponent, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { SkeletonInputLoaderDirective } from '@ske/shared/loader';
import { NzInputDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { NzDatePickerComponent } from 'ng-zorro-antd/date-picker';
import { NzOptionComponent, NzSelectComponent } from 'ng-zorro-antd/select';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';

@Component({
  imports: [
    NzFormDirective,
    SkeletonInputLoaderDirective,
    NzFormItemComponent,
    NzFormLabelComponent,
    NzFormControlComponent,
    NzInputWrapperComponent,
    FormField,
    NzInputDirective,
    NzDatePickerComponent,
    NzSelectComponent,
    NzOptionComponent,
    NzColDirective,
    NzRowDirective
  ],
  selector: 'ske-school-class-form',
  styles: ``,
  templateUrl: './form.html'
})
export class Form {
  public readonly loading = input.required<boolean>();
  public readonly schoolClass = input.required<Partial<SchoolClassDto>>();

  public readonly statusOptions = CLASS_STATUS_OPTIONS;

  private readonly _formModel = linkedSignal({
    source: () => this.schoolClass(),
    computation: (schoolClass) => (<SchoolClassFormModel>{
      id: schoolClass.id ?? null,
      name: schoolClass.name ?? '',
      startDate: schoolClass.startDate ? new Date(schoolClass.startDate) : null,
      endDate: schoolClass.endDate ? new Date(schoolClass.endDate) : null,
      status: schoolClass.status ?? ClassStatus.Upcoming
    })
  });

  public readonly schoolClassForm = form(this._formModel, (schemaPath) => {
    required(schemaPath.name, {
      message: 'Numele clasei este obligatoriu.'
    });
    maxLength(schemaPath.name, 100, {
      message: 'Numele clasei nu poate depăși 100 de caractere.'
    });
    required(schemaPath.startDate, {
      message: 'Data de început este obligatorie.'
    });
    required(schemaPath.endDate, {
      message: 'Data de sfârșit este obligatorie.'
    });
    required(schemaPath.status, {
      message: 'Starea clasei este obligatorie.'
    });
    validate(schemaPath.endDate, (ctx) => {
      const startDate = ctx.valueOf(schemaPath.startDate);
      const endDate = ctx.value();

      if (!startDate || !endDate || endDate > startDate)
        return undefined;

      return {
        kind: 'invalidDateRange',
        message: 'Data de sfârșit trebuie să fie după data de început.'
      };
    });
  });

  public async submit(): Promise<SchoolClassFormSubmit> {
    let formData: SchoolClassFormModel | null = null;
    const isValid = await submit(this.schoolClassForm, async (_) => {
      formData = this.schoolClassForm().value();
    });

    return { isValid, formData };
  }
}

export type SchoolClassFormModel = {
  id: string | null;
  name: string;
  startDate: Date | null;
  endDate: Date | null;
  status: ClassStatus;
};
export type SchoolClassFormSubmit = {
  isValid: boolean;
  formData: SchoolClassFormModel | null;
}
