import { Component, computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Events } from '@ngrx/signals/events';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzProgressComponent, NzProgressStatusType } from 'ng-zorro-antd/progress';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { NzUploadFile } from 'ng-zorro-antd/upload';
import { tap } from 'rxjs';
import { ErrorAlert } from '@ske/shared/errors';
import { fileStorageApiEvents, FileStorageState, FileUpload } from '@ske/shared/storage';
import { CreateItemImportRequest } from '@ske/models';
import { ItemImportState } from '../../../services/item-import.store';

@Component({
  imports: [
    NzModalTitleDirective,
    NzModalFooterDirective,
    NzButtonComponent,
    NzSpaceComponent,
    NzSpaceItemDirective,
    NzProgressComponent,
    ErrorAlert,
    FileUpload
  ],
  selector: 'ske-item-import-modal',
  templateUrl: './item-import-modal.html',
  providers: [FileStorageState, ItemImportState]
})
export class ItemImportModal {
  private readonly _modalRef = inject(NzModalRef);
  private readonly _events = inject(Events);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _fileList = signal<NzUploadFile[]>([]);

  public readonly fileStorage = inject(FileStorageState);
  public readonly store = inject(ItemImportState);
  public readonly hasFiles = () => this._fileList().length > 0;

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
      tap(({ payload }) => {
        const requests: CreateItemImportRequest[] = payload.map((file) => ({ fileMetadataId: file.id }));
        if (requests.length > 0)
          this.store.createImports(requests);
      })
    )
    .subscribe();

  private readonly _closeAfterCreate = effect(() => {
    if (this.store.itemImports().length > 0)
      this._modalRef.close(this.store.itemImports());
  });

  public close() {
    this._modalRef.close();
  }

  public loadFiles() {
    const files = this._fileList();
    if (files.length > 0)
      this.fileStorage.uploadFiles(files);
  }

  public onFileListChange(fileList: NzUploadFile[]) {
    this._fileList.set(fileList);
  }
}
