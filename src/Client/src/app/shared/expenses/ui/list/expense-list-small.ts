import { Component, input, output } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { CdkFixedSizeVirtualScroll, CdkVirtualForOf, CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzPopconfirmDirective } from 'ng-zorro-antd/popconfirm';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { StopClick } from '@ske/shared/directives';
import { BaseList } from '@ske/shared/tables';
import {
  EXPENSE_SOURCE_LABELS,
  EXPENSE_STATUS_COLORS,
  EXPENSE_STATUS_LABELS,
  ExpenseDto,
  GetAllExpensesRequest
} from '@ske/models';

@Component({
  imports: [
    NzButtonComponent,
    NzCardComponent,
    NzEmptyComponent,
    NzPopconfirmDirective,
    NzSkeletonComponent,
    NzSpinComponent,
    NzTagComponent,
    NzTypographyComponent,
    CdkFixedSizeVirtualScroll,
    CdkVirtualForOf,
    CdkVirtualScrollViewport,
    DatePipe,
    DecimalPipe,
    StopClick
  ],
  selector: 'ske-expense-list-small',
  templateUrl: './expense-list-small.html',
  host: {
    class: 'absolute inset-0 flex flex-col'
  }
})
export class ExpenseListSmall extends BaseList<ExpenseDto, GetAllExpensesRequest> {
  public readonly loading = input.required<boolean>();
  public readonly readOnly = input(false);

  public readonly onOpen = output<ExpenseDto>();
  public readonly onDownload = output<ExpenseDto>();
  public readonly onRetry = output<ExpenseDto>();
  public readonly onRemove = output<ExpenseDto>();

  protected readonly cardRowHeight = 112;
  protected readonly skeletonPlaceholders = [0, 1, 2, 3];
  protected readonly statusLabels = EXPENSE_STATUS_LABELS;
  protected readonly statusColors = EXPENSE_STATUS_COLORS;

  protected readonly trackById = (_: number, expense: ExpenseDto) => expense.id;

  protected subtitle(expense: ExpenseDto): string {
    return [EXPENSE_SOURCE_LABELS[expense.source], expense.documentNumber].filter(Boolean).join(' · ');
  }
}
