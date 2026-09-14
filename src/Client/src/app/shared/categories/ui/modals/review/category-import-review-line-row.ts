import { Component, computed, input } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { CategoryDropdown } from '../../dropdown/category-dropdown';
import { CategoryImportReviewEditableLine } from '../../../services/category-import-review.store';
import { CategoryDto } from '@ske/models';
import { NzFormControlComponent, NzFormItemComponent } from 'ng-zorro-antd/form';
import { NzTableCellDirective } from 'ng-zorro-antd/table';
import { NzTagComponent } from 'ng-zorro-antd/tag';

const CATEGORY_DROPDOWN_PLACEHOLDER = 'Alege o categorie';

@Component({
  imports: [
    CategoryDropdown,
    FormField,
    NzFormControlComponent,
    NzFormItemComponent,
    NzTableCellDirective,
    NzTagComponent
  ],
  selector: 'tr[ske-category-import-review-line-row]',
  templateUrl: './category-import-review-line-row.html'
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
}
