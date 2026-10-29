import { Component, computed, inject, input } from '@angular/core';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzImageService } from 'ng-zorro-antd/image';

@Component({
  imports: [NzIconDirective],
  // Not providedIn root; only NzImageModule provides it.
  providers: [NzImageService],
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

  private readonly _imageService = inject(NzImageService);

  protected readonly url = computed(() => this.buildUrl(this.size()));

  protected open(event: Event): void {
    // Rows and cards open the edit modal on click; the preview must not trigger it.
    event.stopPropagation();

    const src = this.buildUrl('full');
    if (src) {
      this._imageService.preview([{ src, alt: 'Imagine articol' }]);
    }
  }

  private buildUrl(size: 'thumb' | 'full'): string | null {
    const fileId = this.imageFileId();

    return fileId ? `/api/Items/${this.itemId()}/image?size=${size}&v=${fileId}` : null;
  }
}
