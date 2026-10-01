import { Component, input, output } from '@angular/core';
import { BaseList } from '@ske/shared/tables';
import {
  CATEGORY_IMPORT_BATCH_STATUS_COLORS,
  CATEGORY_IMPORT_BATCH_STATUS_LABELS,
  CategoryImportBatchFileDto,
  CategoryImportBatchListItemDto,
  CategoryImportBatchStatus,
  GetAllCategoryImportBatchesRequest
} from '@ske/models';
import { CdkFixedSizeVirtualScroll, CdkVirtualForOf, CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { TimeAgoPipe } from '@ske/shared/pipes';

@Component({
  imports: [
    CdkFixedSizeVirtualScroll,
    CdkVirtualForOf,
    CdkVirtualScrollViewport,
    NzButtonComponent,
    NzDropdownDirective,
    NzDropdownMenuComponent,
    NzIconDirective,
    NzCardComponent,
    NzEmptyComponent,
    NzSkeletonComponent,
    NzSpinComponent,
    NzTypographyComponent,
    NzMenuDirective,
    NzMenuItemComponent,
    NzTagComponent,
    TimeAgoPipe
  ],
  selector: 'ske-category-import-list-small',
  templateUrl: './category-import-list-small.html',
  host: {
    class: 'absolute inset-0 flex flex-col',
  },
})
export class CategoryImportListSmall extends BaseList<
  CategoryImportBatchListItemDto,
  GetAllCategoryImportBatchesRequest
> {
  public readonly loading = input.required<boolean>();
  public readonly downloadFile = output<CategoryImportBatchFileDto>();
  public readonly review = output<CategoryImportBatchListItemDto>();

  protected readonly cardRowHeight = 128;
  protected readonly skeletonPlaceholders = [0, 1, 2, 3];

  protected readonly trackById = (_: number, item: CategoryImportBatchListItemDto) => item.id;

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
