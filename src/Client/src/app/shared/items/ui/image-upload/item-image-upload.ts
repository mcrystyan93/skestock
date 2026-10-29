import { Component, computed, effect, inject, input, model, signal } from '@angular/core';
import type { FormValueControl } from '@angular/forms/signals';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzUploadComponent, NzUploadFile } from 'ng-zorro-antd/upload';
import { FileStorageState } from '@ske/shared/storage';

const MAX_IMAGE_BYTES = 5 * 1024 * 1024;
const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];

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
    if (!ALLOWED_TYPES.includes(file.type ?? '')) {
      this._message.error('Format neacceptat');
      return false;
    }

    if ((file.size ?? 0) > MAX_IMAGE_BYTES) {
      this._message.error('Imaginea depășește 5 MB');
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

    // The File itself must reach the upload: HttpClient would serialize a plain object as JSON.
    if (file)
      this.beforeUpload(
        Object.assign(file, { uid: file.name, originFileObj: file }) as unknown as NzUploadFile,
      );
  };

  protected readonly remove = () => {
    this._localFile.set(null);
    this.storage.resetUpload();
    this.value.set(null);

    return true;
  };
}
