import { Component, inject } from '@angular/core';
import {
  applyEach,
  FormField,
  form,
  maxLength,
  required,
  submit,
  validate,
} from '@angular/forms/signals';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { ConfigurationStore, MAX_DEPARTMENT_NAME_LENGTH } from '../services/configuration.store';

@Component({
  imports: [FormField, NzButtonComponent, NzCardComponent, NzIconDirective, NzTypographyComponent],
  selector: 'ske-configuration-departments-card',
  templateUrl: './departments-card.html',
})
export class DepartmentsCard {
  public readonly store = inject(ConfigurationStore);

  protected readonly departmentsForm = form(this.store.departmentDrafts, (schemaPath) => {
    applyEach(schemaPath, (department) => {
      required(department.name, { message: 'Numele departamentului este obligatoriu.' });
      maxLength(department.name, MAX_DEPARTMENT_NAME_LENGTH, {
        message: `Numele nu poate depăși ${MAX_DEPARTMENT_NAME_LENGTH} de caractere.`,
      });
      validate(department.name, ({ value }) =>
        value() && !value().trim()
          ? { kind: 'required', message: 'Numele departamentului este obligatoriu.' }
          : undefined,
      );
      validate(department.name, ({ value, valueOf }) => {
        const currentName = value().trim().toLowerCase();
        const duplicate =
          currentName &&
          valueOf(schemaPath).filter((item) => item.name.trim().toLowerCase() === currentName)
            .length > 1;

        return duplicate
          ? { kind: 'duplicate', message: 'Numele departamentului trebuie să fie unic.' }
          : undefined;
      });

      required(department.responsibilities, { message: 'Responsabilitățile sunt obligatorii.' });
      validate(department.responsibilities, ({ value }) =>
        value() && !value().trim()
          ? { kind: 'required', message: 'Responsabilitățile sunt obligatorii.' }
          : undefined,
      );
    });
  });

  public async validate(): Promise<boolean> {
    return submit(this.departmentsForm, async () => undefined);
  }

  protected clearFeedback(): void {
    this.store.clearFeedback();
  }

  protected removeDepartment(index: number): void {
    this.store.removeDepartment(index);
  }
}
