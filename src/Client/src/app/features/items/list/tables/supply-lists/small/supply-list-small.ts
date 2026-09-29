import { Component, input, output } from '@angular/core';
import { BaseList } from '@ske/shared/tables';
import { formatSupplyListFrequency, GetAllSupplyListsRequest, SupplyListListItemDto } from '@ske/models';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzIconDirective } from 'ng-zorro-antd/icon';
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
    NzButtonComponent,
    NzCardComponent,
    NzEmptyComponent,
    NzIconDirective,
    NzSkeletonComponent,
    NzSpinComponent,
    NzTagComponent,
    NzTypographyComponent,
    CdkFixedSizeVirtualScroll,
    CdkVirtualForOf,
    CdkVirtualScrollViewport,
  ],
  selector: 'ske-supply-list-small',
  templateUrl: './supply-list-small.html',
  host: { class: 'absolute inset-0 flex flex-col' },
})
export class SupplyListSmall extends BaseList<SupplyListListItemDto, GetAllSupplyListsRequest> {
  public readonly loading = input.required<boolean>();
  public readonly togglingId = input<string | null>(null);
  public readonly open = output<SupplyListListItemDto>();
  public readonly toggleActive = output<SupplyListListItemDto>();

  protected readonly cardRowHeight = 128;
  protected readonly skeletonPlaceholders = [0, 1, 2, 3];

  protected readonly trackById = (_: number, item: SupplyListListItemDto) => item.id;

  protected subtitle(supplyList: SupplyListListItemDto): string {
    const lines = `${supplyList.lineCount} ${supplyList.lineCount === 1 ? 'articol' : 'articole'}`;
    return `${formatSupplyListFrequency(supplyList.frequency, supplyList.intervalWeeks)} · ${lines}`;
  }
}
