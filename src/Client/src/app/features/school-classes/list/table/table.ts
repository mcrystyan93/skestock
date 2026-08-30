import { Component, input, output } from '@angular/core';
import { BaseTable } from '@ske/shared/tables';
import {
  CLASS_STATUS_COLORS,
  CLASS_STATUS_LABELS,
  ClassStatus,
  GetAllSchoolClassesRequest,
  SCHOOL_CLASS_TABLE_COLUMNS,
  SchoolClassDto
} from '@ske/models';
import { NzTableModule } from 'ng-zorro-antd/table';
import { DatePipe } from '@angular/common';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzSpinComponent } from 'ng-zorro-antd/spin';

@Component({
  imports: [
    DatePipe,
    NzButtonComponent,
    NzIconDirective,
    NzTagComponent,
    NzTableModule,
    NzSpinComponent
  ],
  selector: 'ske-table',
  styles: ``,
  templateUrl: './table.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class Table extends BaseTable<SchoolClassDto, GetAllSchoolClassesRequest> {
  public readonly loading = input.required<boolean>();

  public readonly onEdit = output<SchoolClassDto>();
  public readonly columns = SCHOOL_CLASS_TABLE_COLUMNS;

  constructor() {
    super();
  }

  public statusLabel(status: ClassStatus): string {
    return CLASS_STATUS_LABELS[status];
  }

  public statusColor(status: ClassStatus): string {
    return CLASS_STATUS_COLORS[status];
  }
}
