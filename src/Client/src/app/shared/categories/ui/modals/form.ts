import { Component, input, linkedSignal } from '@angular/core';
import { CategoryDto } from '@ske/models';
import { form, FormField, maxLength, required, submit } from '@angular/forms/signals';
import { NzFormControlComponent, NzFormDirective, NzFormItemComponent, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { SkeletonInputLoaderDirective } from '@ske/shared/loader';
import { NzInputDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';

@Component({
  imports: [
    NzFormDirective,
    SkeletonInputLoaderDirective,
    NzFormItemComponent,
    NzFormLabelComponent,
    NzFormControlComponent,
    NzInputWrapperComponent,
    FormField,
    NzInputDirective
  ],
  selector: 'ske-form',
  styles: ``,
  templateUrl: './form.html'
})
export class Form {
  public readonly loading = input.required<boolean>();
  public readonly category = input.required<Partial<CategoryDto>>();

  private readonly _formModel = linkedSignal({
    source: () => this.category(),
    computation: (category) => (<CategoryFormModel>{
      id: category.id ?? null,
      name: category.name ?? ''
    })
  });

  public readonly categoryForm = form(this._formModel, (schemaPath) => {
    required(schemaPath.name, {
      message: 'Numele categoriei este obligatoriu.'
    });
    maxLength(schemaPath.name, 100, {
      message: 'Numele categoriei nu poate depăși 100 de caractere.'
    });
  });

  public async submit():Promise<CategoryFormSubmit> {
    let formData: CategoryFormModel | null = null;
    const isValid = await submit(this.categoryForm, async (data) => {
      formData = this.categoryForm().value();
    });

    return { isValid, formData };
  }
}

export type CategoryFormModel = {
  id: string | null;
  name: string;
};
export type CategoryFormSubmit = {
  isValid: boolean;
  formData: CategoryFormModel | null;
}
