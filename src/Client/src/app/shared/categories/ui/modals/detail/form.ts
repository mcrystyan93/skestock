import {Component, forwardRef, input, linkedSignal} from '@angular/core';
import {CategoryDropdownValue, CategoryDto} from '@ske/models';
import {form, FormField, maxLength, required, submit} from '@angular/forms/signals';
import {NzFormControlComponent, NzFormDirective, NzFormItemComponent, NzFormLabelComponent} from 'ng-zorro-antd/form';
import {SkeletonInputLoaderDirective} from '@ske/shared/loader';
import {NzInputDirective, NzInputWrapperComponent} from 'ng-zorro-antd/input';
import {CategoryDropdown} from '../../dropdown/category-dropdown';
import {IconPicker, type IconPickerValue} from '@ske/shared/icons';

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
    IconPicker,
    // The dropdown opens CategoryDetailModal, which contains this form: break the import cycle.
    forwardRef(() => CategoryDropdown)
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
      name: category.name ?? '',
      parent: category.parentId ? { id: category.parentId, name: category.parentName ?? '' } : null,
      icon: category.icon ?? null
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

  public async submit(): Promise<CategoryFormSubmit> {
    let formData: CategoryFormModel | null = null;
    const isValid = await submit(this.categoryForm, async (data) => {
      formData = this.categoryForm().value();
    });

    return {isValid, formData};
  }
}

export type CategoryFormModel = {
  id: string | null;
  name: string;
  parent: CategoryDropdownValue;
  icon: IconPickerValue | null;
};
export type CategoryFormSubmit = {
  isValid: boolean;
  formData: CategoryFormModel | null;
}
