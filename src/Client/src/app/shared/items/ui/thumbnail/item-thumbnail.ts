import { Component, computed, effect, inject, input, output } from '@angular/core';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzImageService } from 'ng-zorro-antd/image';
import { NzMessageService } from 'ng-zorro-antd/message';
import { FileStorageState } from '@ske/shared/storage';
import { IMAGE_ACCEPT, imageFileError, toUploadFile } from '../image-upload/item-image-file';

@Component({
  imports: [NzIconDirective],
  // Not providedIn root; only NzImageModule provides it.
  // Own upload state so each row can upload without touching the global uploadSuccess event.
  providers: [NzImageService, FileStorageState],
  selector: 'ske-item-thumbnail',
  templateUrl: './item-thumbnail.html',
  host: {
    class:
      'relative z-10 inline-flex shrink-0 items-center justify-center overflow-hidden rounded bg-slate-100 text-slate-400 dark:bg-slate-800',
    '[class.h-10]': 'size() === "thumb"',
    '[class.w-10]': 'size() === "thumb"',
    '[class.h-40]': 'size() === "full"',
    '[class.w-40]': 'size() === "full"',
  },
})
export class ItemThumbnail {
  public readonly itemId = input.required<string>();
  public readonly imageFileId = input<string | null | undefined>();
  public readonly size = input<'thumb' | 'full'>('thumb');
  // Without an image, the placeholder becomes a button that uploads one (rows only; stock tables stay read-only).
  public readonly uploadable = input(false);
  public readonly imageUploaded = output<string>();

  protected readonly storage = inject(FileStorageState);
  protected readonly accept = IMAGE_ACCEPT;

  private readonly _imageService = inject(NzImageService);
  private readonly _message = inject(NzMessageService);

  protected readonly url = computed(() => this.buildUrl(this.size()));

  private readonly _notifyFailureRef = effect(() => {
    if (this.storage.hasUploadFailures()) this._message.error('Încărcarea imaginii a eșuat');
  });

  private readonly _emitUploadedRef = effect(() => {
    const [uploaded] = this.storage.uploadedFiles();

    if (uploaded) this.imageUploaded.emit(uploaded.fileId);
  });

  protected open(event: Event): void {
    // Rows and cards open the edit modal on click; the preview must not trigger it.
    event.stopPropagation();

    const src = this.buildUrl('full');
    if (src) {
      this._imageService.preview([{ src, alt: 'Imagine articol' }]);
    }
  }

  protected pick(event: Event, picker: HTMLInputElement): void {
    event.stopPropagation();
    picker.click();
  }

  protected upload(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    input.value = '';

    if (!file) return;

    const error = imageFileError(file);

    if (error) {
      this._message.error(error);
      return;
    }

    this.storage.uploadFiles([toUploadFile(file)]);
  }

  private buildUrl(size: 'thumb' | 'full'): string | null {
    const fileId = this.imageFileId();

    return fileId ? `/api/Items/${this.itemId()}/image?size=${size}&v=${fileId}` : null;
  }
}
