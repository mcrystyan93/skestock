import { Component, input, output } from '@angular/core';
import { BaseList } from '@ske/shared/tables';
import { GetAllItemsRequest, ItemDto } from '@ske/models';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import {
  CdkFixedSizeVirtualScroll,
  CdkVirtualForOf,
  CdkVirtualScrollViewport,
} from '@angular/cdk/scrolling';

@Component({
  imports: [
    NzCardComponent,
    NzEmptyComponent,
    NzSkeletonComponent,
    NzSpinComponent,
    NzTagComponent,
    NzTypographyComponent,
    CdkFixedSizeVirtualScroll,
    CdkVirtualForOf,
    CdkVirtualScrollViewport,
  ],
  selector: 'ske-item-list-small',
  templateUrl: './item-list-small.html',
  host: {
    class: 'absolute inset-0 flex flex-col',
  },
})
export class ItemListSmall extends BaseList<ItemDto, GetAllItemsRequest> {
  public readonly loading = input.required<boolean>();
  public readonly onEdit = output<ItemDto>();

  protected readonly cardRowHeight = 84;
  protected readonly skeletonPlaceholders = [0, 1, 2, 3];

  protected readonly trackById = (_: number, item: ItemDto) => item.id;

  protected subtitle(item: ItemDto): string {
    return [item.sku, item.categoryName || '—'].filter(Boolean).join(' · ');
  }

  protected onSpace(event: Event, item: ItemDto): void {
    event.preventDefault();
    this.onEdit.emit(item);
  }
}
