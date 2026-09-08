import { signalStore, withState } from "@ngrx/signals";
import { withCategoryCollection } from "./category-collection.feature";

type CategoryDropdownState = {};
const initialState: CategoryDropdownState = {};
export const CategoryDropdownStore = signalStore(
    withState(initialState),
    withCategoryCollection()
);