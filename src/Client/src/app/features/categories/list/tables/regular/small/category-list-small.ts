import { Component, input, output } from '@angular/core';
import { CATEGORY_TABLE_COLUMNS, CategoryDto, GetAllCategoriesRequest } from '@ske/models';
import { BaseList } from '@ske/shared/tables';
import {
  NzListComponent,
  NzListEmptyComponent,
  NzListItemActionComponent,
  NzListItemActionsComponent,
  NzListItemComponent,
  NzListItemMetaAvatarComponent,
  NzListItemMetaComponent,
  NzListItemMetaDescriptionComponent,
  NzListItemMetaTitleComponent
} from 'ng-zorro-antd/list';
import { NzAvatarComponent } from 'ng-zorro-antd/avatar';
import { NzDividerComponent } from 'ng-zorro-antd/divider';
import { DatePipe } from '@angular/common';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { CdkFixedSizeVirtualScroll, CdkVirtualForOf, CdkVirtualScrollViewport } from '@angular/cdk/scrolling';

@Component({
  imports: [
    NzListComponent,
    NzListItemComponent,
    NzListItemMetaComponent,
    NzListItemMetaAvatarComponent,
    NzAvatarComponent,
    NzListItemMetaTitleComponent,
    NzListItemMetaDescriptionComponent,
    NzDividerComponent,
    DatePipe,
    NzListItemActionsComponent,
    NzListItemActionComponent,
    NzButtonComponent,
    NzIconDirective,
    CdkVirtualScrollViewport,
    CdkFixedSizeVirtualScroll,
    CdkVirtualForOf,
    NzListEmptyComponent
  ],
  selector: 'ske-category-list-small',
  styles: ``,
  templateUrl: './category-list-small.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class CategoryListSmall extends BaseList<CategoryDto, GetAllCategoriesRequest> {
  public readonly loading = input.required<boolean>();

  public readonly onEdit = output<CategoryDto>();

  public readonly columns = CATEGORY_TABLE_COLUMNS;
}
