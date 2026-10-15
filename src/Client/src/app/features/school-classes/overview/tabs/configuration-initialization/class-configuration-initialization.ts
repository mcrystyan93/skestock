import { Component, input, output } from '@angular/core';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzCardComponent } from 'ng-zorro-antd/card';

@Component({
  selector: 'ske-class-configuration-initialization',
  imports: [NzButtonComponent, NzCardComponent],
  templateUrl: './class-configuration-initialization.html',
  host: { class: 'block min-w-0' }
})
export class ClassConfigurationInitialization {
  public readonly loading = input(false);
  public readonly initialize = output<void>();
}
