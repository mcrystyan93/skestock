import { signalStore, withState } from '@ngrx/signals';
import { withItemCollection } from '../store-features/item-collection.feature';

type ItemDropdownState = {};
const initialState: ItemDropdownState = {};
export const ItemDropdownStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withItemCollection()
);
