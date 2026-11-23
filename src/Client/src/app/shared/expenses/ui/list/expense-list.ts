import { Component, effect, inject, input, untracked } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { NzModalService } from 'ng-zorro-antd/modal';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzBadgeComponent } from 'ng-zorro-antd/badge';
import { NzPopconfirmDirective } from 'ng-zorro-antd/popconfirm';
import { isNil } from 'lodash-es';
import {
  ColumnFilter,
  EXPENSE_SOURCE_LABELS,
  EXPENSE_STATUS_COLORS,
  EXPENSE_STATUS_LABELS,
  ExpenseDto
} from '@ske/models';
import { ErrorAlert } from '@ske/shared/errors';
import { FileStorageState } from '@ske/shared/storage';
import { injectClassReadOnly } from '@ske/shared/school-classes';
import { ExpenseListStore } from '../../services/expense-list.store';
import { ExpenseModal, ExpenseModalData } from '../modals/expense-modal';

@Component({
  imports: [
    NzTableModule,
    NzButtonComponent,
    NzBadgeComponent,
    NzPopconfirmDirective,
    DatePipe,
    DecimalPipe,
    ErrorAlert
  ],
  selector: 'ske-expense-list',
  templateUrl: './expense-list.html',
  providers: [ExpenseListStore, FileStorageState, NzModalService],
  host: {
    class: 'absolute block inset-0 overflow-auto pt-4'
  }
})
export class ExpenseList {
  public readonly classId = input.required<string | null>();

  public readonly store = inject(ExpenseListStore);
  private readonly _fileStorage = inject(FileStorageState);
  private readonly _modalService = inject(NzModalService);
  public readonly readOnly = injectClassReadOnly();

  private readonly _loadEffect = effect(() => {
    const classId = this.classId();

    if (isNil(classId))
      return;

    const classFilter: ColumnFilter = { value: classId, operator: 'equals', fieldType: 'string', field: 'classId' };
    untracked(() => this.store.load({ filters: [classFilter] }));
  });

  public statusLabel(expense: ExpenseDto): string {
    return EXPENSE_STATUS_LABELS[expense.status];
  }

  public statusColor(expense: ExpenseDto): string {
    return EXPENSE_STATUS_COLORS[expense.status];
  }

  public sourceLabel(expense: ExpenseDto): string {
    return EXPENSE_SOURCE_LABELS[expense.source];
  }

  public download(expense: ExpenseDto) {
    if (expense.fileMetadataId)
      this._fileStorage.downloadFile(expense.fileMetadataId);
  }

  public open(expense: ExpenseDto) {
    const classId = this.classId();

    if (isNil(classId))
      return;

    const modalRef = this._modalService.create({
      nzContent: ExpenseModal,
      nzData: <ExpenseModalData>{ classId, expenseId: expense.id, readOnly: this.readOnly() },
      nzWrapClassName: 'modal-w-90 modal-w-md-75 modal-w-xl-50',
      nzCentered: true,
      nzMaskClosable: false
    });

    modalRef.afterClose.subscribe((result) => {
      if (result)
        this.store.reload();
    });
  }
}
