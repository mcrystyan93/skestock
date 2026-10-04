import { Component, inject } from '@angular/core';
import { ConfigurationStore } from './services/configuration.store';
import { ConfigurationHeader } from './header/configuration-header';
import { DepartmentCardListContainer } from './departments/department-card-list-container';
import { NzModalService } from 'ng-zorro-antd/modal';
import { InvitationsSection } from './invitations/invitations-section';
import { RoomsSection } from './rooms/rooms-section';

@Component({
  imports: [ConfigurationHeader, DepartmentCardListContainer, InvitationsSection, RoomsSection],
  selector: 'ske-class-configuration-page',
  templateUrl: './configuration.page.html',
  host: {
    class: 'flex flex-col grow gap-4',
  },
  providers: [ConfigurationStore, NzModalService],
})
export class ConfigurationPage {
  public readonly store = inject(ConfigurationStore);

  constructor() {
    this.store.loadInvitations();
    this.store.loadDepartments();
    this.store.loadRooms();
  }
}
