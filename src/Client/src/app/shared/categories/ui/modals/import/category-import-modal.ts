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
import {
  fileStorageApiEvents,
  FileStorageState,
  FileUpload
} from '@ske/shared/storage';
import { CreateCategoryImportRequest } from '@ske/models';
import { CategoryImportState } from '../../../services/category-import.store';

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
        const requests: CreateCategoryImportRequest[] = payload
          .map((file) => ({ fileMetadataId: file.id }));

        if (requests.length > 0)
          this.store.createImports(requests);
      })
    )
    .subscribe();

  private readonly _closeAfterCreate = effect(() => {
    const categoryImports = this.store.categoryImports();

    if (categoryImports.length > 0)
      this._modalRef.close(categoryImports);
  });

  public close() {
    this._modalRef.close();
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
