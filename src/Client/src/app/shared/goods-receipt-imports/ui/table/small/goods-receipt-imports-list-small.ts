import { Component, input, output } from '@angular/core';
import {
  GetAllGoodsReceiptImportsRequest,
  GOODS_RECEIPT_IMPORT_STATUS_COLORS,
  GOODS_RECEIPT_IMPORT_STATUS_LABELS,
  GoodsReceiptImportListItemDto,
  GoodsReceiptImportStatus
} from '@ske/models';
import { BaseList } from '@ske/shared/tables';
import { TimeAgoPipe } from '@ske/shared/pipes';
import { StopClick } from '@ske/shared/directives';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';

@Component({
  imports: [
    NzButtonComponent,
    NzCardComponent,
    NzEmptyComponent,
    NzIconDirective,
    NzSkeletonComponent,
    NzSpinComponent,
    NzTagComponent,
    NzTooltipDirective,
    NzTypographyComponent,
    StopClick,
    TimeAgoPipe
  ],
  selector: 'ske-goods-receipt-imports-list-small',
  templateUrl: './goods-receipt-imports-list-small.html',
  host: {
    class: 'absolute inset-0 flex flex-col overflow-hidden'
  }
})
export class GoodsReceiptImportsListSmall extends BaseList<
  GoodsReceiptImportListItemDto,
  GetAllGoodsReceiptImportsRequest
> {
  public readonly loading = input.required<boolean>();
  public readonly downloadFile = output<GoodsReceiptImportListItemDto>();
  public readonly review = output<GoodsReceiptImportListItemDto>();

  protected readonly skeletonPlaceholders = [0, 1, 2, 3];

  public statusLabel(status: GoodsReceiptImportStatus): string {
    return GOODS_RECEIPT_IMPORT_STATUS_LABELS[status];
  }

  public statusColor(status: GoodsReceiptImportStatus): string {
    return GOODS_RECEIPT_IMPORT_STATUS_COLORS[status];
  }

  public fileName(importDto: GoodsReceiptImportListItemDto): string {
    const path = importDto.blobPath.replaceAll('\\', '/');
    const name = path.split('/').filter(Boolean).at(-1);

    return name || 'Fișier importat';
  }
}
