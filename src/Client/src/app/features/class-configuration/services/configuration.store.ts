import { signalStore } from '@ngrx/signals';
import { withInvitations } from './invitations.feature';
import { withDepartments } from './departments.feature';
import { withRooms } from './rooms.feature';
import { withInvitationSplit } from './invitation-split.feature';

export const ConfigurationStore = signalStore(
  withInvitations(),
  withDepartments(),
  withRooms(),
  withInvitationSplit(),
);
