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
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { NzBadgeComponent } from 'ng-zorro-antd/badge';

@Component({
  imports: [
    DatePipe,
    CdkFixedSizeVirtualScroll,
    CdkVirtualForOf,
    CdkVirtualScrollViewport,
    NzButtonComponent,
    NzIconDirective,
    NzCardComponent,
    NzEmptyComponent,
    NzSkeletonComponent,
    NzSpinComponent,
    NzTooltipDirective,
    NzTypographyComponent,
    NzBadgeComponent
  ],
  selector: 'ske-school-class-list-small',
  templateUrl: './school-class-list-small.html',
  host: {
    class: 'absolute inset-0 flex flex-col',
  },
})
export class SchoolClassListSmall extends BaseList<SchoolClassDto, GetAllSchoolClassesRequest> {
  public readonly loading = input.required<boolean>();
  public readonly onEdit = output<SchoolClassDto>();
  public readonly onView = output<SchoolClassDto>();

  protected readonly cardRowHeight = 112;
  protected readonly skeletonPlaceholders = [0, 1, 2, 3];

  protected readonly trackById = (_: number, item: SchoolClassDto) => item.id;

  public statusLabel(status: ClassStatus): string {
    return CLASS_STATUS_LABELS[status];
  }

  public statusColor(status: ClassStatus): string {
    return CLASS_STATUS_COLORS[status];
  }
}
