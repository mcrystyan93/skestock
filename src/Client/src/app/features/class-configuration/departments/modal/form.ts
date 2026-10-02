import { Component, input, linkedSignal } from '@angular/core';
import { form, FormField, maxLength, required, submit, validate } from '@angular/forms/signals';
import { DepartmentTemplateDto, SaveDepartmentRequest } from '@ske/models';
import { MAX_DEPARTMENT_NAME_LENGTH } from '../../services/configuration.constants';
import { NzFormControlComponent, NzFormDirective, NzFormItemComponent, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { NzInputDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { SkeletonInputLoaderDirective } from '@ske/shared/loader';

@Component({
  imports: [
    NzFormDirective,
    NzFormItemComponent,
    NzFormLabelComponent,
    NzFormControlComponent,
    NzInputWrapperComponent,
    NzInputDirective,
    FormField,
    SkeletonInputLoaderDirective
  ],
  selector: 'ske-configuration-department-form',
  templateUrl: './form.html'
})
export class DepartmentForm {
  public readonly loading = input.required<boolean>();
  public readonly department = input.required<DepartmentTemplateDto | null>();


  private readonly _formModel = linkedSignal<SaveDepartmentRequest>(() => {
    const department = this.department();
    return department ? { ...department } : { id: null, name: '', responsibilities: '' };
  });

  public readonly departmentForm = form(this._formModel, (path) => {
    required(path.name, { message: 'Numele departamentului este obligatoriu.' });
    maxLength(path.name, MAX_DEPARTMENT_NAME_LENGTH, {
      message: `Numele nu poate depăși ${MAX_DEPARTMENT_NAME_LENGTH} de caractere.`
    });
    validate(path.name, ({ value }) =>
      value() && !value().trim()
        ? { kind: 'required', message: 'Numele departamentului este obligatoriu.' }
        : undefined
    );
    required(path.responsibilities, { message: 'Responsabilitățile sunt obligatorii.' });
    validate(path.responsibilities, ({ value }) =>
      value() && !value().trim()
        ? { kind: 'required', message: 'Responsabilitățile sunt obligatorii.' }
        : undefined
    );
  });

  public async submit(): Promise<DepartmentFormSubmit> {
    let formData: SaveDepartmentRequest | null = null;
    const isValid = await submit(this.departmentForm, async () => {
      const department = this.departmentForm().value();
      formData = {
        ...department,
        name: department.name.trim(),
        responsibilities: department.responsibilities.trim()
      };
    });
    return { isValid, formData };
  }
}

export type DepartmentFormSubmit = {
  isValid: boolean;
  formData: SaveDepartmentRequest | null;
};
