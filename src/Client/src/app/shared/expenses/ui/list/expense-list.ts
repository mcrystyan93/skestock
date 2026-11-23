import { Component, effect, inject, input, untracked } from '@angular/core';
import { NzModalService } from 'ng-zorro-antd/modal';
import { isNil } from 'lodash-es';
import { ColumnFilter, ExpenseDto } from '@ske/models';
import { ErrorAlert } from '@ske/shared/errors';
import { FileStorageState } from '@ske/shared/storage';
import { injectClassReadOnly } from '@ske/shared/school-classes';
import { LayoutBreakpoint } from '@ske/shared/directives';
import { ExpenseListStore } from '../../services/expense-list.store';
import { ExpenseModal, ExpenseModalData } from '../modals/expense-modal';
import { ExpenseTable } from './expense-table';
import { ExpenseListSmall } from './expense-list-small';

@Component({
  imports: [ErrorAlert, LayoutBreakpoint, ExpenseTable, ExpenseListSmall],
  selector: 'ske-expense-list',
  templateUrl: './expense-list.html',
  providers: [ExpenseListStore, FileStorageState, NzModalService],
  host: {
    class: 'absolute flex flex-col inset-0 pt-4'
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
