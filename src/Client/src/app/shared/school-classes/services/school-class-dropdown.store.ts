import { signalStore, withState } from '@ngrx/signals';
import { withSchoolClassCollection } from './school-class-collection.feature';

type SchoolClassDropdownState = {};

const initialState: SchoolClassDropdownState = {};

export const SchoolClassDropdownStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withSchoolClassCollection()
);
