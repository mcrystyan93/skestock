import { Component, computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { NzProgressStatusType } from 'ng-zorro-antd/progress';
import { NzUploadFile } from 'ng-zorro-antd/upload';
import { Events } from '@ngrx/signals/events';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { tap } from 'rxjs';
import { ErrorAlert } from '@ske/shared/errors';
import { fileStorageApiEvents, FileStorageState } from '@ske/shared/storage';
import { ExpenseUploadStore } from '../../services/expense-upload.store';
import { ExpenseUploadForm } from './expense-upload-form';

export type AddExpenseModalData = {
  classId: string;
};

/** Close result when the user chooses to type the expense in instead of uploading a document. */
export const ADD_EXPENSE_MANUALLY = 'manual';

@Component({
  imports: [
    NzModalTitleDirective,
    NzModalFooterDirective,
    NzButtonComponent,
    NzSpaceComponent,
    NzSpaceItemDirective,
    ExpenseUploadForm,
    ErrorAlert
  ],
  selector: 'ske-add-expense-modal',
  templateUrl: './add-expense-modal.html',
  providers: [FileStorageState, ExpenseUploadStore]
})
export class AddExpenseModal {
  private readonly _modalData = inject<AddExpenseModalData>(NZ_MODAL_DATA);
  public readonly fileStorage = inject(FileStorageState);
  public readonly store = inject(ExpenseUploadStore);

  private readonly _nzModalRef = inject(NzModalRef);
  private readonly _events = inject(Events);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _nzMessageService = inject(NzMessageService);

  public readonly progressStatus = computed<NzProgressStatusType>(() => {
    if (this.fileStorage.hasUploadFailures())
      return 'exception';

    if (this.fileStorage.totalCount() > 0 && this.fileStorage.completedCount() === this.fileStorage.totalCount())
      return 'success';

    return 'active';
  });

  public readonly progressLabel = computed(() =>
    `${this.fileStorage.completedCount()} din ${this.fileStorage.totalCount()}`);

  public readonly showProgress = computed(() =>
    this.fileStorage.uploadLoading() || this.fileStorage.hasUploadFailures());

  public readonly failedFileNames = computed(() => this.fileStorage.failedFiles().map((file) => file.name));

  private readonly _uploadSuccessRef = this._events.on(fileStorageApiEvents.uploadSuccess)
    .pipe(
      takeUntilDestroyed(this._destroyRef),
      tap(({ payload }) => {
        if (this.fileStorage.hasUploadFailures() || payload.length === 0)
          return;

        this.store.createFromDocuments({ files: payload, classId: this._modalData.classId });
      })
    )
    .subscribe();

  private readonly _closeAfterCreate = effect(() => {
    const expenses = this.store.expenses();

    if (expenses.length === 0)
      return;

    if (this.store.failedCount() > 0)
      this._nzMessageService.warning(this.store.failedCount() === 1
        ? 'Un fișier nu a putut fi transformat în cheltuială.'
        : `${this.store.failedCount()} fișiere nu au putut fi transformate în cheltuieli.`);

    this._nzModalRef.close(expenses);
  });

  private readonly _fileList = signal<NzUploadFile[]>([]);

  public readonly hasFiles = computed(() => this._fileList().length > 0);

  public close() {
    this._nzModalRef.close();
  }

  public addManually() {
    this._nzModalRef.close(ADD_EXPENSE_MANUALLY);
  }

  public loadFiles() {
    const files = this._fileList();

    if (files.length === 0)
      return;

    this.fileStorage.uploadFiles(files);
  }

  public onFileListChange(fileList: NzUploadFile[]) {
    this._fileList.set(fileList);
  }
}
