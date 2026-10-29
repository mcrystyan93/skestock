import { Component, computed, effect, inject, input, model, signal } from '@angular/core';
import type { FormValueControl } from '@angular/forms/signals';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzUploadComponent, NzUploadFile } from 'ng-zorro-antd/upload';
import { FileStorageState } from '@ske/shared/storage';
import { imageFileError, toUploadFile } from './item-image-file';

@Component({
  imports: [NzUploadComponent, NzIconDirective, NzButtonComponent],
  selector: 'ske-item-image-upload',
  templateUrl: './item-image-upload.html',
  // Own state so the global uploadSuccess event (used by import modals) is never relied on.
  providers: [FileStorageState],
})
export class ItemImageUpload implements FormValueControl<string | null> {
  public readonly value = model<string | null>(null);
  public readonly itemId = input<string | null>(null);
  public readonly uploading = model(false);

  protected readonly storage = inject(FileStorageState);

  private readonly _message = inject(NzMessageService);
  private readonly _localFile = signal<NzUploadFile | null>(null);

  protected readonly fileList = computed<NzUploadFile[]>(() => {
    const local = this._localFile();

    if (local)
      return [
        {
          // Spreading a File drops its name, size and type (prototype getters), so copy them explicitly.
          ...local,
          name: local.name,
          size: local.size,
          type: local.type,
          status: this.storage.uploadLoading()
            ? 'uploading'
            : this.storage.hasUploadFailures()
              ? 'error'
              : 'done',
        },
      ];

    const fileId = this.value();
    const itemId = this.itemId();

    return fileId && itemId
      ? [
          {
            uid: fileId,
            name: 'Imagine articol',
            status: 'done',
            url: `/api/Items/${itemId}/image?size=full&v=${fileId}`,
          },
        ]
      : [];
  });

  private readonly _syncUploadingRef = effect(() =>
    this.uploading.set(this.storage.uploadLoading()),
  );

  private readonly _notifyFailureRef = effect(() => {
    if (this.storage.hasUploadFailures()) this._message.error('Încărcarea imaginii a eșuat');
  });

  private readonly _emitUploadedRef = effect(() => {
    const [uploaded] = this.storage.uploadedFiles();

    if (uploaded) this.value.set(uploaded.fileId);
  });

  protected readonly beforeUpload = (file: NzUploadFile) => {
    const error = imageFileError(file);

    if (error) {
      this._message.error(error);
      return false;
    }

    this._localFile.set(file);
    this.storage.uploadFiles([file]);

    return false;
  };

  protected readonly replace = (event: Event) => {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    input.value = '';

    if (file) this.beforeUpload(toUploadFile(file));
  };

  protected readonly remove = () => {
    this._localFile.set(null);
    this.storage.resetUpload();
    this.value.set(null);

    return true;
  };
}
