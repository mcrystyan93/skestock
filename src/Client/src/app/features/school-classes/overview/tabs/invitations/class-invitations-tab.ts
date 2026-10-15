import { Component, inject } from '@angular/core';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { SchoolClassOverviewStore } from '../../../services/school-class-overview.store';
import { ErrorAlert } from '@ske/shared/errors';
import { ClassConfigurationInitialization } from '../configuration-initialization/class-configuration-initialization';
import { ClassInvitationCount } from './count/class-invitation-count';

@Component({
  selector: 'ske-school-class-invitations-tab',
  imports: [NzTypographyComponent, ErrorAlert, ClassConfigurationInitialization, ClassInvitationCount],
  templateUrl: './class-invitations-tab.html',
  host: { class: 'block min-w-0 p-4 md:p-6' }
})
export class ClassInvitationsTab {
  protected readonly store = inject(SchoolClassOverviewStore);
}
