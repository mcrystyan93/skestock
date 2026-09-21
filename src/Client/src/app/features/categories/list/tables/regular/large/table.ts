import { Component, input, output } from '@angular/core';
import { BaseTableWithFilter } from '@ske/shared/tables';
import { CATEGORY_TABLE_COLUMNS, CategoryDto, GetAllCategoriesRequest } from '@ske/models';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzAvatarComponent } from 'ng-zorro-antd/avatar';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { StopClick } from '@ske/shared/directives';
import { TimeAgoPipe } from '@ske/shared/pipes';

@Component({
  imports: [
    NzTableModule,
    NzAvatarComponent,
    NzTagComponent,
    NzTypographyComponent,
    NzButtonComponent,
    NzIconDirective,
    StopClick,
    TimeAgoPipe
  ],
  selector: 'ske-category-table',
  styles: ``,
  templateUrl: './table.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class Table extends BaseTableWithFilter<CategoryDto, GetAllCategoriesRequest> {
  public readonly loading = input.required<boolean>();
  public readonly deletingCategoryId = input<string | null>(null);

  public readonly onEdit = output<CategoryDto>();
  public readonly onDelete = output<CategoryDto>();
  public readonly columns = CATEGORY_TABLE_COLUMNS;
  public readonly columnLayout = {
    name: { width: '300px' },
    itemCount: { width: '110px' },
    createdDate: { width: '150px' },
    createdByName: { width: '180px' },
    lastModifiedDate: { width: '160px' },
    lastModifiedByName: { width: '180px' },
    actions: { width: '70px' }
  } as const;

  constructor() {
    super();
  }
}
