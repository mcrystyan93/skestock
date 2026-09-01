import { signalStore, withState } from "@ngrx/signals";
import { withLocationCollection } from "../store-features/location-collection.feature";

type LocationDropdownState = {};
const initialState: LocationDropdownState = {};
export const LocationDropdownStore = signalStore(
    withState(initialState),
    withLocationCollection()
);
