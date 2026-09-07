import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { NzProgressComponent, NzProgressStatusType } from 'ng-zorro-antd/progress';
import { Upload } from './upload';
import { NzUploadFile } from 'ng-zorro-antd/upload';
import { fileStorageApiEvents, FileStorageState } from '@ske/shared/storage';
import { Events } from '@ngrx/signals/events';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { tap } from 'rxjs';

@Component({
  imports: [
    NzModalTitleDirective,
    NzModalFooterDirective,
    NzButtonComponent,
    NzSpaceComponent,
    NzSpaceItemDirective,
    NzProgressComponent,
    Upload
  ],
  selector: 'ske-add-goods-receipt-modal',
  styles: ``,
  templateUrl: './add-goods-receipt-modal.html',
  providers: [FileStorageState]
})
export class AddGoodsReceiptModal {
  private readonly _nzModalRef = inject(NzModalRef);

  public readonly store = inject(FileStorageState);

  private readonly _events = inject(Events);
  private readonly _destroyRef = inject(DestroyRef);

  public readonly progressStatus = computed<NzProgressStatusType>(() => {
    if (this.store.hasUploadFailures())
      return 'exception';

    if (this.store.totalCount() > 0 && this.store.completedCount() === this.store.totalCount())
      return 'success';

    return 'active';
  });

  public readonly progressFormat = (): string =>
    `${this.store.completedCount()} din ${this.store.totalCount()}`;

  private readonly _uploadSuccessRef = this._events.on(fileStorageApiEvents.uploadSuccess)
    .pipe(
      takeUntilDestroyed(this._destroyRef),
      tap(payload => this._nzModalRef.close({ files: payload.payload }))
    )
    .subscribe();

  private readonly _fileList = signal<Array<NzUploadFile>>([]);

  public readonly hasFiles = computed(() => this._fileList().length > 0);

  public close() {
    this._nzModalRef.close();
  }

  public loadFiles() {
    const files = this._fileList();

    if (files.length === 0)
      return;

    this.store.uploadFiles(files);
  }

  public onFileListChange(fileList: NzUploadFile[]) {
    this._fileList.set(fileList);
  }
}
