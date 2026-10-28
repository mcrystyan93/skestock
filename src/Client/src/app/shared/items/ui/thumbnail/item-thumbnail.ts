import { Component, computed, input } from '@angular/core';
import { NzIconDirective } from 'ng-zorro-antd/icon';

@Component({
  imports: [NzIconDirective],
  selector: 'ske-item-thumbnail',
  templateUrl: './item-thumbnail.html',
  host: {
    class:
      'inline-flex shrink-0 items-center justify-center overflow-hidden rounded bg-slate-100 text-slate-400 dark:bg-slate-800',
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

  protected readonly url = computed(() => {
    const fileId = this.imageFileId();

    return fileId ? `/api/Items/${this.itemId()}/image?size=${this.size()}&v=${fileId}` : null;
  });
}
