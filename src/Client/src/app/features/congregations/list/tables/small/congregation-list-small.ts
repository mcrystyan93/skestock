import { Component, input, output } from '@angular/core';
import { CongregationDto, GetAllCongregationsRequest } from '@ske/models';
import { BaseList } from '@ske/shared/tables';
import { StopClick } from '@ske/shared/directives';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { CdkFixedSizeVirtualScroll, CdkVirtualForOf, CdkVirtualScrollViewport } from '@angular/cdk/scrolling';

@Component({
  imports: [
    NzButtonComponent,
    NzCardComponent,
    NzEmptyComponent,
    NzIconDirective,
    NzSkeletonComponent,
    NzSpinComponent,
    NzTagComponent,
    NzTypographyComponent,
    StopClick,
    CdkVirtualScrollViewport,
    CdkFixedSizeVirtualScroll,
    CdkVirtualForOf
  ],
  selector: 'ske-congregation-list-small',
  templateUrl: './congregation-list-small.html',
  host: {
    class: 'absolute inset-0 flex flex-col'
  }
})
export class CongregationListSmall extends BaseList<CongregationDto, GetAllCongregationsRequest> {
  public readonly loading = input.required<boolean>();
  public readonly deletingCongregationId = input<string | null>(null);

  public readonly onEdit = output<CongregationDto>();
  public readonly onDelete = output<CongregationDto>();

  protected readonly cardRowHeight = 96;
  protected readonly skeletonPlaceholders = [0, 1, 2, 3];

  protected readonly trackById = (_: number, item: CongregationDto) => item.id;

  protected onSpace(event: Event, item: CongregationDto): void {
    event.preventDefault();
    this.onEdit.emit(item);
  }
}
