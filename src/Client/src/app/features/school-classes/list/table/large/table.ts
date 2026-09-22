import { Component, computed, input, output } from '@angular/core';
import { BaseTableWithFilter } from '@ske/shared/tables';
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
import { StopClick } from '@ske/shared/directives';
import { NzProgressComponent } from 'ng-zorro-antd/progress';
import { NzBadgeComponent } from 'ng-zorro-antd/badge';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { TimeAgoPipe } from '@ske/shared/pipes';

@Component({
  imports: [DatePipe, NzButtonComponent, NzIconDirective, NzTableModule, StopClick, NzProgressComponent, NzBadgeComponent, NzTypographyComponent, TimeAgoPipe],
  selector: 'ske-school-class-table',
  styles: ``,
  templateUrl: './table.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class Table extends BaseTableWithFilter<SchoolClassDto, GetAllSchoolClassesRequest> {
  public readonly loading = input.required<boolean>();

  public readonly onEdit = output<SchoolClassDto>();
  public readonly onView = output<SchoolClassDto>();
  public readonly columns = SCHOOL_CLASS_TABLE_COLUMNS;

  public readonly progressMap = computed(() => {
    const items = this.items();
    const progressMap = new Map<string, number>();

    // calculate progress for each class based on end date, start date and current date
    const now = new Date();
    for (const item of items) {
      const startDate = new Date(item.startDate);
      const endDate = new Date(item.endDate);
      let progress = 0;
      if (now < startDate) {
        progress = 0;
      } else if (now > endDate) {
        progress = 100;
      } else {
        const totalDuration = endDate.getTime() - startDate.getTime();
        const elapsedDuration = now.getTime() - startDate.getTime();
        progress = Math.round((elapsedDuration / totalDuration) * 100);
      }
      progressMap.set(item.id, progress);
    }
    return progressMap;
  });

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
