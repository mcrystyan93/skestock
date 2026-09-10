import { Component, computed, input } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { ItemDropdown, ItemImportReviewEditableLine } from '@ske/shared/items';
import { NzTableCellDirective } from 'ng-zorro-antd/table';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzFormControlComponent, NzFormItemComponent } from 'ng-zorro-antd/form';
import { ItemDto } from '@ske/models';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';

const ITEM_DROPDOWN_PLACEHOLDER = 'Alege un produs';
const CATEGORY_PLACEHOLDER = 'Selectează un articol';

@Component({
  imports: [
    ItemDropdown,
    NzTableCellDirective,
    NzTagComponent,
    NzFormControlComponent,
    NzFormItemComponent,
    FormField,
    NzTypographyComponent
  ],
  selector: 'tr[ske-item-import-review-line-row]',
  styles: ``,
  templateUrl: './item-import-review-line-row.html'
})
export class ItemImportReviewLineRow {
  public readonly lineForm = input.required<FieldTree<ItemImportReviewEditableLine>>();
  public readonly itemPlaceholder = ITEM_DROPDOWN_PLACEHOLDER;
  public readonly categoryPlaceholder = CATEGORY_PLACEHOLDER;

  public readonly itemPrefill = computed(() => {
    const lineForm = this.lineForm();
    const lineValue = lineForm().value();

    return <ItemDto>{
      sku: lineValue.sku,
      name: lineValue.name,
      description: lineValue.description,
      unit: lineValue.unit,
      isPerishable: lineValue.isPerishable,
      categoryName: lineValue.category?.name,
      categoryId: lineValue.category?.id
    };
  });
}
