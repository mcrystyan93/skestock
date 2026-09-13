import {Component, computed, DestroyRef, effect, inject, signal} from '@angular/core';
import {NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalTitleDirective} from 'ng-zorro-antd/modal';
import {NzButtonComponent} from 'ng-zorro-antd/button';
import {NzSpaceComponent, NzSpaceItemDirective} from 'ng-zorro-antd/space';
import {NzProgressComponent, NzProgressStatusType} from 'ng-zorro-antd/progress';
import {NzUploadFile} from 'ng-zorro-antd/upload';
import {ErrorAlert} from '@ske/shared/errors';
import {fileStorageApiEvents, FileStorageState, FileUpload} from '@ske/shared/storage';
import {
  GoodsReceiptImportModalData,
  GoodsReceiptImportState
} from '../../services/goods-receipt-import.store';
import {Events} from '@ngrx/signals/events';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {tap} from 'rxjs';

const NO_FILES_COUNT = 0;

@Component({
  imports: [
    NzModalTitleDirective,
    NzModalFooterDirective,
    NzButtonComponent,
    NzSpaceComponent,
    NzSpaceItemDirective,
    NzProgressComponent,
    FileUpload,
    ErrorAlert
  ],
  selector: 'ske-add-goods-receipt-modal',
  styles: ``,
  templateUrl: './add-goods-receipt-modal.html',
  providers: [FileStorageState, GoodsReceiptImportState]
})
export class AddGoodsReceiptModal {
  public readonly modalData = signal<GoodsReceiptImportModalData>(inject(NZ_MODAL_DATA));
  public readonly fileStorage = inject(FileStorageState);
  public readonly store = inject(GoodsReceiptImportState);

  private readonly _nzModalRef = inject(NzModalRef);
  private readonly _events = inject(Events);
  private readonly _destroyRef = inject(DestroyRef);

  public readonly progressStatus = computed<NzProgressStatusType>(() => {
    if (this.fileStorage.hasUploadFailures())
      return 'exception';

    if (this.fileStorage.totalCount() > 0 && this.fileStorage.completedCount() === this.fileStorage.totalCount())
      return 'success';

    return 'active';
  });

  public readonly progressFormat = (): string =>
    `${this.fileStorage.completedCount()} din ${this.fileStorage.totalCount()}`;

  private readonly _uploadSuccessRef = this._events.on(fileStorageApiEvents.uploadSuccess)
    .pipe(
      takeUntilDestroyed(this._destroyRef),
      tap(({payload}) => {
        const classId = this.modalData().classId;

        if (classId === null || this.fileStorage.hasUploadFailures() || payload.length === NO_FILES_COUNT)
          return;

        this.store.createImports({files: payload, classId});
      })
    )
    .subscribe();

  private readonly _closeAfterImport = effect(() => {
    const imports = this.store.goodsReceiptImports();

    if (imports.length > 0)
      this._nzModalRef.close(imports);
  });

  private readonly _fileList = signal<Array<NzUploadFile>>([]);

  public readonly hasFiles = computed(() => this._fileList().length > 0);

  public close() {
    this._nzModalRef.close();
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
