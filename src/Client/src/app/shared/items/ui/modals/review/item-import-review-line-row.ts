import { Component, computed, input } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { ItemDropdown } from '../../dropdown/item-dropdown';
import { NzTableCellDirective } from 'ng-zorro-antd/table';
import { NzFormControlComponent, NzFormItemComponent } from 'ng-zorro-antd/form';
import { ItemDto } from '@ske/models';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { ItemImportReviewEditableLine } from '../../../services/item-import-review.store';
import { NzIconDirective } from 'ng-zorro-antd/icon';

const ITEM_DROPDOWN_PLACEHOLDER = 'Alege un produs';
const CATEGORY_PLACEHOLDER = 'Selectează un articol';

@Component({
  imports: [
    ItemDropdown,
    NzTableCellDirective,
    NzFormControlComponent,
    NzFormItemComponent,
    FormField,
    NzTypographyComponent,
    NzIconDirective
  ],
  selector: 'tr[ske-item-import-review-line-row]',
  styles: ``,
  templateUrl: './item-import-review-line-row.html',
  host: {
    '[class]': 'rowClass()'
  }
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

  public readonly rowClass = computed(() => {
    const lineForm = this.lineForm();
    const itemValue = lineForm.item().value();

    return !itemValue?.id ? '[&>td]:!bg-amber-500/10' : '';
  });
}
