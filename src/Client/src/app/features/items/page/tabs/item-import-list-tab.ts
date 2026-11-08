import { Component, input, output } from '@angular/core';
import { ErrorAlert } from '@ske/shared/errors';
import { LayoutBreakpoint } from '@ske/shared/directives';
import {
  GetAllItemImportBatchesRequest,
  ItemImportBatchFileDto,
  ItemImportBatchListItemDto,
  ProblemDetails,
  ValidationProblemDetails
} from '@ske/models';
import { ItemImportFilterForm } from '../../import/filter/item-import-filter-form';
import { ItemImportTable } from '../../import/tables/large/table';
import { ItemImportListSmall } from '../../import/tables/small/item-import-list-small';

@Component({
  imports: [ErrorAlert, ItemImportFilterForm, ItemImportTable, ItemImportListSmall, LayoutBreakpoint],
  selector: 'ske-item-import-list-tab',
  templateUrl: './item-import-list-tab.html',
  host: { class: 'flex grow flex-col gap-2 absolute inset-0' }
})
export class ItemImportListTab {
  public readonly items = input.required<ItemImportBatchListItemDto[]>();
  public readonly filter = input.required<GetAllItemImportBatchesRequest>();
  public readonly loading = input.required<boolean>();
  public readonly hasNextPage = input.required<boolean>();
  public readonly isLoadingMore = input.required<boolean>();
  public readonly problemDetail = input<ProblemDetails | null>();
  public readonly validationErrors = input<ValidationProblemDetails | null>();

  public readonly onFilterChange = output<GetAllItemImportBatchesRequest>();
  public readonly onLoadMore = output<void>();
  public readonly downloadFile = output<ItemImportBatchFileDto>();
  public readonly review = output<ItemImportBatchListItemDto>();
}
