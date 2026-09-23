import { Component, computed, input } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { CategoryDropdown } from '../../dropdown/category-dropdown';
import { CategoryImportReviewEditableLine } from '../../../services/category-import-review.store';
import { CategoryDto } from '@ske/models';
import { NzFormControlComponent, NzFormItemComponent } from 'ng-zorro-antd/form';
import { NzTableCellDirective } from 'ng-zorro-antd/table';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { NzIconDirective } from 'ng-zorro-antd/icon';

const CATEGORY_DROPDOWN_PLACEHOLDER = 'Alege o categorie';

@Component({
  imports: [
    CategoryDropdown,
    FormField,
    NzFormControlComponent,
    NzFormItemComponent,
    NzTableCellDirective,
    NzTypographyComponent,
    NzIconDirective
  ],
  selector: 'tr[ske-category-import-review-line-row]',
  templateUrl: './category-import-review-line-row.html',
  host: {
    '[class]': 'rowClass()'
  }
})
export class CategoryImportReviewLineRow {
  public readonly lineForm = input.required<FieldTree<CategoryImportReviewEditableLine>>();
  public readonly categoryPlaceholder = CATEGORY_DROPDOWN_PLACEHOLDER;

  public readonly categoryPrefill = computed(() => {
    const lineForm = this.lineForm();
    const line = lineForm().value();

    return <Partial<CategoryDto>>{
      name: line.name
    };
  });

  public readonly rowClass = computed(() => {
    const lineForm = this.lineForm();
    const itemValue = lineForm.category().value();

    return !itemValue?.id ? '[&>td]:!bg-amber-500/10' : '';
  });
}
