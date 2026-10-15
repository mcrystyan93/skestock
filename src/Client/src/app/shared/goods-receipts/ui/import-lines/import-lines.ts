import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { GoodsReceiptImportLineDto } from '@ske/models';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';

@Component({
  selector: 'ske-goods-receipt-import-lines',
  imports: [CurrencyPipe, DecimalPipe, NzEmptyComponent, NzSkeletonComponent, NzTableModule, NzTypographyComponent],
  templateUrl: './import-lines.html',
  host: { class: 'block h-full min-h-0 overflow-auto' },
  styles: `
    .desktop-lines { display: none; }
    @media (min-width: 992px) {
      .desktop-lines { display: block; }
      .mobile-lines { display: none; }
    }
  `
})
export class ImportLines {
  readonly lines = input.required<GoodsReceiptImportLineDto[]>();
  readonly loading = input(false);
}
