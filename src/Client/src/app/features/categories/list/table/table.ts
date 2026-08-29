import { Component, input, output } from '@angular/core';
import { BaseTable, VirtualData } from '@ske/shared/tables';
import { CATEGORY_TABLE_COLUMNS, CategoryDto, GetAllCategoriesRequest } from '@ske/models';
import {
  NzTableComponent,
  NzTableVirtualScrollDirective,
  NzTheadComponent,
  NzThMeasureDirective
} from 'ng-zorro-antd/table';
import { DatePipe } from '@angular/common';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';

@Component({
  imports: [
    NzTableComponent,
    NzTheadComponent,
    NzThMeasureDirective,
    NzTableVirtualScrollDirective,
    DatePipe,
    NzButtonComponent,
    NzIconDirective
  ],
  selector: 'ske-table',
  styles: ``,
  templateUrl: './table.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class Table extends BaseTable<CategoryDto, GetAllCategoriesRequest> {
  public readonly loading = input.required<boolean>();
  public readonly deletingCategoryId = input<number | null>(null);

  public readonly onEdit = output<CategoryDto>();
  public readonly onDelete = output<CategoryDto>();
  public readonly columns = CATEGORY_TABLE_COLUMNS;

  constructor() {
    super();
  }
}
