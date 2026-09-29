import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { GetAllStockBatchesRequest, StockBatchListItemDto } from '@ske/models';
import { BaseList } from '@ske/shared/tables';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';

@Component({
  imports: [
    CurrencyPipe,
    DecimalPipe,
    NzEmptyComponent,
    NzSkeletonComponent,
    NzSpinComponent,
    NzTypographyComponent
  ],
  selector: 'ske-stock-batches-list-small',
  templateUrl: './stock-batches-list-small.html',
  host: {
    class: 'absolute inset-0 flex flex-col overflow-hidden'
  }
})
export class StockBatchesListSmall extends BaseList<StockBatchListItemDto, GetAllStockBatchesRequest> {
  public readonly loading = input.required<boolean>();

  protected readonly skeletonPlaceholders = [0, 1, 2];

  protected readonly trackById = (_: number, batch: StockBatchListItemDto) => batch.id;
}
