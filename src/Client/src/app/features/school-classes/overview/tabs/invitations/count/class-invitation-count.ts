import { Component, input } from '@angular/core';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzStatisticComponent } from 'ng-zorro-antd/statistic';

@Component({
  selector: 'ske-class-invitation-count',
  imports: [NzCardComponent, NzStatisticComponent],
  templateUrl: './class-invitation-count.html',
  host: { class: 'block min-w-0' }
})
export class ClassInvitationCount {
  public readonly count = input.required<number>();
}
