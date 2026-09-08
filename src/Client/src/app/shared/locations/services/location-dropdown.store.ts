import { signalStore, withState } from '@ngrx/signals';
import { withLocationCollection } from './location-collection.feature';

type LocationDropdownState = {};
const initialState: LocationDropdownState = {};
export const LocationDropdownStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withLocationCollection()
);
