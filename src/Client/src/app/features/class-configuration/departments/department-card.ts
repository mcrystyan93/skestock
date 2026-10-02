import { Component, input, output } from '@angular/core';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { LoaderDirective } from '@ske/shared/loader';
import { DepartmentTemplateDto } from '@ske/models';

@Component({
  imports: [NzButtonComponent, NzCardComponent, NzIconDirective, LoaderDirective],
  selector: 'ske-configuration-department-card',
  templateUrl: './department-card.html',
})
export class DepartmentCard {
  public readonly department = input.required<DepartmentTemplateDto>();
  public readonly loading = input(false);
  public readonly onEdit = output<void>();
  public readonly onRemove = output<void>();
}
