import { Component, input, output } from '@angular/core';
import { BaseTableWithFilter } from '@ske/shared/tables';
import { CONGREGATION_TABLE_COLUMNS, CongregationDto, GetAllCongregationsRequest } from '@ske/models';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { StopClick } from '@ske/shared/directives';
import { TimeAgoPipe } from '@ske/shared/pipes';

@Component({
  imports: [
    NzTableModule,
    NzTagComponent,
    NzTypographyComponent,
    NzButtonComponent,
    NzIconDirective,
    StopClick,
    TimeAgoPipe
  ],
  selector: 'ske-congregation-table',
  styles: ``,
  templateUrl: './table.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class Table extends BaseTableWithFilter<CongregationDto, GetAllCongregationsRequest> {
  public readonly loading = input.required<boolean>();
  public readonly deletingCongregationId = input<string | null>(null);

  public readonly onEdit = output<CongregationDto>();
  public readonly onDelete = output<CongregationDto>();
  public readonly columns = CONGREGATION_TABLE_COLUMNS;
  public readonly columnLayout = {
    name: { width: '200px' },
    email: { width: '180px' },
    coordinator: { width: '140px' },
    canReceiveStudents: { width: '150px' },
    isSmallCongregation: { width: '140px' },
    lastModifiedDate: { width: '160px' },
    actions: { width: '90px' }
  } as const;

  constructor() {
    super();
  }
}
