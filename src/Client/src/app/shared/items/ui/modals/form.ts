import { Component, input, linkedSignal } from '@angular/core';
import { CategoryDropdownValue, ItemDto } from '@ske/models';
import { form, FormField, maxLength, min, required, submit } from '@angular/forms/signals';
import { NzFormControlComponent, NzFormDirective, NzFormItemComponent, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { SkeletonInputLoaderDirective } from '@ske/shared/loader';
import { NzInputDirective, NzInputWrapperComponent, NzTextareaCountComponent } from 'ng-zorro-antd/input';
import { NzInputNumberComponent } from 'ng-zorro-antd/input-number';
import { NzCheckboxComponent } from 'ng-zorro-antd/checkbox';
import { CategoryDropdown } from '@ske/shared/categories';
import { NzSwitchComponent } from 'ng-zorro-antd/switch';
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
    NzInputNumberComponent,
    CategoryDropdown,
    NzSwitchComponent,
    NzColDirective,
    NzRowDirective,
    NzTextareaCountComponent
  ],
  selector: 'ske-item-form',
  styles: ``,
  templateUrl: './form.html'
})
export class Form {
  public readonly loading = input.required<boolean>();
  public readonly item = input.required<Partial<ItemDto>>();

  private readonly _formModel = linkedSignal({
    source: () => this.item(),
    computation: (item) => (<ItemFormModel>{
      id: item.id ?? null,
      sku: item.sku ?? '',
      name: item.name ?? '',
      description: item.description ?? '',
      unit: item.unit ?? '',
      minThreshold: item.minThreshold ?? 0,
      isPerishable: item.isPerishable ?? false,
      category: !item.categoryId ? null : { id: item.categoryId, name: item.categoryName ?? '' }
    })
  });

  public readonly itemForm = form(this._formModel, (schemaPath) => {
    maxLength(schemaPath.sku, 50, {
      message: 'Codul produsului nu poate depăși 50 de caractere.'
    });
    required(schemaPath.name, {
      message: 'Numele produsului este obligatoriu.'
    });
    maxLength(schemaPath.name, 100, {
      message: 'Numele produsului nu poate depăși 100 de caractere.'
    });
    maxLength(schemaPath.description, 500, {
      message: 'Descrierea nu poate depăși 500 de caractere.'
    });
    required(schemaPath.unit, {
      message: 'Unitatea de măsură este obligatorie.'
    });
    maxLength(schemaPath.unit, 20, {
      message: 'Unitatea de măsură nu poate depăși 20 de caractere.'
    });
    required(schemaPath.minThreshold, {
      message: 'Pragul minim este obligatoriu.'
    });
    min(schemaPath.minThreshold, 0, {
      message: 'Pragul minim nu poate fi negativ.'
    });
    required(schemaPath.category, {
      message: 'Categoria este obligatorie.'
    });
  });

  public async submit():Promise<ItemFormSubmit> {
    let formData: ItemFormModel | null = null;
    const isValid = await submit(this.itemForm, async (_) => {
      formData = this.itemForm().value();
    });

    return { isValid, formData };
  }
}

export type ItemFormModel = {
  id: number | null;
  sku: string;
  name: string;
  description: string;
  unit: string;
  minThreshold: number;
  isPerishable: boolean;
  category: CategoryDropdownValue;
};
export type ItemFormSubmit = {
  isValid: boolean;
  formData: ItemFormModel | null;
}
