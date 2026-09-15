import { DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { BaseList } from '@ske/shared/tables';
import {
  CATEGORY_IMPORT_BATCH_STATUS_COLORS,
  CATEGORY_IMPORT_BATCH_STATUS_LABELS,
  CategoryImportBatchFileDto,
  CategoryImportBatchListItemDto,
  CategoryImportBatchStatus,
  GetAllCategoryImportBatchesRequest,
} from '@ske/models';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import {
  NzListComponent,
  NzListEmptyComponent,
  NzListItemActionComponent,
  NzListItemActionsComponent,
  NzListItemComponent,
  NzListItemMetaComponent,
  NzListItemMetaDescriptionComponent,
  NzListItemMetaTitleComponent,
} from 'ng-zorro-antd/list';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzDividerComponent } from 'ng-zorro-antd/divider';
import {
  CdkFixedSizeVirtualScroll,
  CdkVirtualForOf,
  CdkVirtualScrollViewport,
} from '@angular/cdk/scrolling';

@Component({
  imports: [
    DatePipe,
    NzButtonComponent,
    NzDropdownDirective,
    NzDropdownMenuComponent,
    NzIconDirective,
    NzListComponent,
    NzListEmptyComponent,
    NzListItemActionComponent,
    NzListItemActionsComponent,
    NzListItemComponent,
    NzListItemMetaComponent,
    NzListItemMetaDescriptionComponent,
    NzListItemMetaTitleComponent,
    NzMenuDirective,
    NzMenuItemComponent,
    NzTagComponent,
    NzDividerComponent,
    CdkFixedSizeVirtualScroll,
    CdkVirtualForOf,
    CdkVirtualScrollViewport,
  ],
  selector: 'ske-category-import-list-small',
  templateUrl: './category-import-list-small.html',
  host: {
    class: 'absolute block inset-0',
  },
})
export class CategoryImportListSmall extends BaseList<
  CategoryImportBatchListItemDto,
  GetAllCategoryImportBatchesRequest
> {
  public readonly loading = input.required<boolean>();
  public readonly downloadFile = output<CategoryImportBatchFileDto>();
  public readonly review = output<CategoryImportBatchListItemDto>();

  public statusLabel(status: CategoryImportBatchStatus): string {
    return CATEGORY_IMPORT_BATCH_STATUS_LABELS[status];
  }

  public statusColor(status: CategoryImportBatchStatus): string {
    return CATEGORY_IMPORT_BATCH_STATUS_COLORS[status];
  }

  public fileNames(categoryImport: CategoryImportBatchListItemDto): string {
    return categoryImport.files.map((file) => file.originalName).join(', ');
  }
}
