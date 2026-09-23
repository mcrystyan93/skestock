import { DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { BaseList } from '@ske/shared/tables';
import {
  CLASS_STATUS_COLORS,
  CLASS_STATUS_LABELS,
  ClassStatus,
  GetAllSchoolClassesRequest,
  SchoolClassDto
} from '@ske/models';
import { CdkFixedSizeVirtualScroll, CdkVirtualForOf, CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDividerComponent } from 'ng-zorro-antd/divider';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import {
  NzListComponent,
  NzListEmptyComponent,
  NzListItemActionComponent,
  NzListItemActionsComponent,
  NzListItemComponent,
  NzListItemMetaComponent,
  NzListItemMetaDescriptionComponent,
  NzListItemMetaTitleComponent
} from 'ng-zorro-antd/list';
import { NzBadgeComponent } from 'ng-zorro-antd/badge';

@Component({
  imports: [
    DatePipe,
    CdkFixedSizeVirtualScroll,
    CdkVirtualForOf,
    CdkVirtualScrollViewport,
    NzButtonComponent,
    NzDividerComponent,
    NzIconDirective,
    NzListComponent,
    NzListEmptyComponent,
    NzListItemActionComponent,
    NzListItemActionsComponent,
    NzListItemComponent,
    NzListItemMetaComponent,
    NzListItemMetaDescriptionComponent,
    NzListItemMetaTitleComponent,
    NzBadgeComponent
  ],
  selector: 'ske-school-class-list-small',
  templateUrl: './school-class-list-small.html',
  host: {
    class: 'absolute block inset-0',
  },
})
export class SchoolClassListSmall extends BaseList<SchoolClassDto, GetAllSchoolClassesRequest> {
  public readonly loading = input.required<boolean>();
  public readonly onEdit = output<SchoolClassDto>();
  public readonly onView = output<SchoolClassDto>();

  public statusLabel(status: ClassStatus): string {
    return CLASS_STATUS_LABELS[status];
  }

  public statusColor(status: ClassStatus): string {
    return CLASS_STATUS_COLORS[status];
  }
}
