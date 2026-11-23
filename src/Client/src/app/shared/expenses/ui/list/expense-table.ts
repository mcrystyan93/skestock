import { Component, input, output } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzBadgeComponent } from 'ng-zorro-antd/badge';
import { NzPopconfirmDirective } from 'ng-zorro-antd/popconfirm';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { BaseTableWithFilter } from '@ske/shared/tables';
import {
  EXPENSE_SOURCE_LABELS,
  EXPENSE_STATUS_COLORS,
  EXPENSE_STATUS_LABELS,
  ExpenseDto,
  GetAllExpensesRequest
} from '@ske/models';

@Component({
  imports: [
    NzTableModule,
    NzButtonComponent,
    NzBadgeComponent,
    NzPopconfirmDirective,
    NzTypographyComponent,
    DatePipe,
    DecimalPipe
  ],
  selector: 'ske-expense-table',
  templateUrl: './expense-table.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class ExpenseTable extends BaseTableWithFilter<ExpenseDto, GetAllExpensesRequest> {
  public readonly loading = input.required<boolean>();
  public readonly readOnly = input(false);

  public readonly onOpen = output<ExpenseDto>();
  public readonly onDownload = output<ExpenseDto>();
  public readonly onRetry = output<ExpenseDto>();
  public readonly onRemove = output<ExpenseDto>();

  // The virtual-scroll template context is untyped, so lookups go through typed helpers.
  protected statusLabel(expense: ExpenseDto): string {
    return EXPENSE_STATUS_LABELS[expense.status];
  }

  protected statusColor(expense: ExpenseDto): string {
    return EXPENSE_STATUS_COLORS[expense.status];
  }

  protected sourceLabel(expense: ExpenseDto): string {
    return EXPENSE_SOURCE_LABELS[expense.source];
  }
}
