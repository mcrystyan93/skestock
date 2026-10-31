import { Component, input } from '@angular/core';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { ItemThumbnail } from '../thumbnail/item-thumbnail';

/** Large item image (or placeholder icon) with name and context; stacked on mobile, side by side from `sm`. */
@Component({
  imports: [ItemThumbnail, NzTypographyComponent],
  selector: 'ske-item-hero',
  templateUrl: './item-hero.html',
  host: { class: 'flex min-w-0 flex-col items-center gap-3 text-center sm:flex-row sm:gap-4 sm:text-left' }
})
export class ItemHero {
  public readonly itemId = input.required<string>();
  public readonly imageFileId = input<string | null | undefined>();
  public readonly name = input.required<string>();
  public readonly subtitle = input<string>();
}
