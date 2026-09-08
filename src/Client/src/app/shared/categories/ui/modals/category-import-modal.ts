import { Component, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Events } from '@ngrx/signals/events';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzProgressComponent } from 'ng-zorro-antd/progress';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { NzUploadFile } from 'ng-zorro-antd/upload';
import { tap } from 'rxjs';
import { ErrorAlert } from '@ske/shared/errors';
import {
  fileStorageApiEvents,
  FileStorageState,
  FileUpload
} from '@ske/shared/storage';
import { CategoryImportState } from '../../services/category-import.store';

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
  selector: 'ske-category-import-modal',
  templateUrl: './category-import-modal.html',
  providers: [
    FileStorageState,
    CategoryImportState
  ]
})
export class CategoryImportModal {
  private readonly _modalRef = inject(NzModalRef);
  private readonly _events = inject(Events);
  private readonly _destroyRef = inject(DestroyRef);

  public readonly fileStorage = inject(FileStorageState);
  public readonly store = inject(CategoryImportState);
  private readonly _fileList = signal<Array<NzUploadFile>>([]);

  public readonly hasFiles = () => this._fileList().length > 0;

  private readonly _uploadSuccessRef = this._events.on(fileStorageApiEvents.uploadSuccess)
    .pipe(
      takeUntilDestroyed(this._destroyRef),
      tap(({ payload }) => {
        const [file] = payload;

        if (file)
          this.store.createImport({ fileMetadataId: file.id });
      })
    )
    .subscribe();

  private readonly _closeAfterCreate = effect(() => {
    const categoryImport = this.store.categoryImport();

    if (categoryImport)
      this._modalRef.close(categoryImport);
  });

  public close() {
    this._modalRef.close();
  }

  public loadFile() {
    const [file] = this._fileList();

    if (file)
      this.fileStorage.uploadFiles([file]);
  }

  public onFileListChange(fileList: NzUploadFile[]) {
    this._fileList.set(fileList);
  }
}
