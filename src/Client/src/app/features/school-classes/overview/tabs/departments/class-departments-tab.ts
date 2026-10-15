import { Component, inject } from '@angular/core';
import { SchoolClassOverviewStore } from '../../../services/school-class-overview.store';
import { ErrorAlert } from '@ske/shared/errors';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { ClassConfigurationInitialization } from '../configuration-initialization/class-configuration-initialization';
import { ClassDepartmentList } from './list/class-department-list';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';

@Component({
  selector: 'ske-school-class-departments-tab',
  imports: [ErrorAlert, NzAlertComponent, ClassConfigurationInitialization, ClassDepartmentList, NzTypographyComponent],
  templateUrl: './class-departments-tab.html',
  host: { class: 'block min-w-0 p-4 md:p-6' }
})
export class ClassDepartmentsTab {
  protected readonly store = inject(SchoolClassOverviewStore);
}
