import { signalStore } from '@ngrx/signals';
import { withInvitations } from './invitations.feature';
import { withDepartments } from './departments.feature';
import { withRooms } from './rooms.feature';

export const ConfigurationStore = signalStore(withInvitations(), withDepartments(), withRooms());
