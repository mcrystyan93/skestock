import { Component, inject } from '@angular/core';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { SchoolClassOverviewStore } from '../../../../services/school-class-overview.store';
import { ClassDepartmentCard } from '../department/class-department-card';

@Component({
  selector: 'ske-class-department-list',
  imports: [NzEmptyComponent, ClassDepartmentCard],
  templateUrl: './class-department-list.html',
  host: { class: 'block min-w-0' }
})
export class ClassDepartmentList {
  protected readonly store = inject(SchoolClassOverviewStore);
}
