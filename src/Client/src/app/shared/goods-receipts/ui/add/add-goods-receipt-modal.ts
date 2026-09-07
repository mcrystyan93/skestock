import { Component, DestroyRef, inject } from '@angular/core';
import { NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
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

  private readonly _uploadSuccessRef = this._events.on(fileStorageApiEvents.uploadSuccess)
    .pipe(
      takeUntilDestroyed(this._destroyRef),
      tap(payload => this._nzModalRef.close({ fileMetadata: payload.payload }))
    )
    .subscribe();

  private fileList: Array<NzUploadFile> = [];

  public close() {
    this._nzModalRef.close();
  }

  public loadFile() {
    const file = this.fileList[0];

    this.store.uploadFile(file);
  }

  public onFileListChange(fileList: NzUploadFile[]) {
    this.fileList = fileList;
  }
}
