import { DatePipe } from '@angular/common';
import { Component, inject, input, output } from '@angular/core';
import { CategoryDto, GetAllCategoriesRequest } from '@ske/models';
import { BaseList } from '@ske/shared/tables';
import { NzAvatarComponent } from 'ng-zorro-antd/avatar';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { CdkFixedSizeVirtualScroll, CdkVirtualForOf, CdkVirtualScrollViewport } from '@angular/cdk/scrolling';

@Component({
  imports: [
    NzAvatarComponent,
    NzCardComponent,
    NzEmptyComponent,
    NzSkeletonComponent,
    NzSpinComponent,
    NzTypographyComponent,
    CdkVirtualScrollViewport,
    CdkFixedSizeVirtualScroll,
    CdkVirtualForOf
  ],
  providers: [DatePipe],
  selector: 'ske-category-list-small',
  templateUrl: './category-list-small.html',
  host: {
    class: 'absolute inset-0 flex flex-col'
  }
})
export class CategoryListSmall extends BaseList<CategoryDto, GetAllCategoriesRequest> {
  public readonly loading = input.required<boolean>();
  public readonly onEdit = output<CategoryDto>();

  protected readonly cardRowHeight = 84;
  protected readonly skeletonPlaceholders = [0, 1, 2, 3];

  private readonly _datePipe = inject(DatePipe);

  protected readonly trackById = (_: number, item: CategoryDto) => item.id;

  protected subtitle(item: CategoryDto): string {
    const count = `${item.itemCount} ${item.itemCount === 1 ? 'articol' : 'articole'}`;
    const date = this._datePipe.transform(item.lastModifiedDate, 'short');
    return date ? `${count} · ${date}` : count;
  }

  protected onSpace(event: Event, item: CategoryDto): void {
    event.preventDefault();
    this.onEdit.emit(item);
  }
}
