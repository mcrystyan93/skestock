# Spec: Patch the item list in place instead of reloading

## Objective
Today every `ItemUpdated`/`ItemEnabled`/`ItemDisabled` realtime event calls `ItemListState.reload()`, which clears `items`, shows the loader, and refetches page 1. For the user that means: list flicker, loaded-more pages and scroll position lost, and (server) the just-evicted `items` cache tag is rebuilt by every connected client at once.

Goal: an update to one item replaces that one row in `ItemListState.items`, with no list reload, no flicker, no lost pages.

Success: edit/enable/disable an item (own tab or another user's) → only that row changes; scroll position and loaded pages stay; no `GET /api/Items` list request is issued for the event.

## Findings the design rests on
- `setItemImage` and `toggleActive` already patch the row from the HTTP response, but the realtime event for the same change then triggers `reload()` anyway (the broadcast goes to the whole `ItemsList` group, including the sender; `RealtimeEnvelope` carries no origin). So the existing local patches are currently invisible.
- The item edit modal saves through `ItemDetailState.editItem`, which does not touch the list at all; the list only updates via the realtime reload.
- Events carry only `{ itemId }`. `GET /api/Items/{id}` exists (`ItemsHttp.getById`) and its server cache entry is per item.
- Server list cache invalidation stays as is (see analysis: patching server cache entries is not feasible with HybridCache).

## Design (recommended)
On `itemUpdated`, `itemEnabled`, `itemDisabled` with `{ itemId }`:
1. If the item is **not in the loaded `items`**, do nothing (it may be on an unloaded page; the next load/loadMore fetches fresh data).
2. If it is, `ItemsHttp.getById(itemId)` and replace that row in `items` by id. Same code path for own and foreign changes, so no origin tracking is needed and the existing `toggleActive`/`setItemImage` local patches become consistent with it.
3. `itemCreated` and `itemImportBatchConfirmed` keep calling `reload()` (a new row's position depends on sort/filter, so a full reload is correct).
4. Fetch failure → fall back to `reload()` (never leave a stale row silently).
5. Concurrency: fetches are grouped per item id with `switchMap` inside each group, so a newer event for a row cancels its older fetch (a late response cannot overwrite fresher data) while different rows never cancel each other.

### Known limitation (accepted)
A patched row stays in its current position even if the edit changes its sort key (`name`, `sku`, `unit`, `lastModifiedDate`) or makes it no longer match the active filter/search (e.g. active filter + disable, category filter + category change). It is corrected on the next `load()` (filter/sort/search change, page revisit). This is the price of not reloading.

Optional follow-up, not in scope unless you ask: when the active sort/filter touches a changed field, drop the patch and `reload()` instead.

## Commands
```
cd src/Client && npm test -- --watch=false
cd src/Client && npm run build
cd src/Client && npx prettier --write <files>
```

## Project Structure
- `src/Client/src/app/features/items/services/item-list.store.ts` - event handler change + new `patchItem` method.
- `src/Client/src/app/features/items/services/item-list.store.spec.ts` - new specs (colocated).
- No backend change. No new dependency, no new pattern (`rxMethod` + `mapResponse`, as the existing methods).

## Code Style
Match `item-list.store.ts`: `rxMethod<string>` piped through `mergeMap` + `mapResponse`, `patchState(store, { items: store.items().map(i => i.id === updated.id ? updated : i) })`, split the single `.on(...)` into two subscriptions (row events vs. list events).

## Testing Strategy
Vitest, colocated, same harness as the existing `item-list.store.spec.ts`:
- row event for a loaded item → `getById` called once, only that row replaced, `getAll` not called.
- row event for an item not in the list → no request.
- two events for different items in flight → both applied.
- two events for the same item, older fetch resolving last → newest state wins.
- `getById` fails → `reload()` called (`getAll` issued).
- `itemCreated` still reloads.

## Boundaries
- Always: Romanian UI copy untouched; keep `togglingItemId` behaviour; prettier on touched files.
- Ask first: sending an origin/connection id in the realtime envelope; sending the full `ItemDto` in the event payload instead of fetching; any server cache change.
- Never: change the server `items` tag invalidation, remove the fallback `reload()`.

## Success Criteria
- [ ] Specs above pass; `npm run build` and `npm test -- --watch=false` green.
- [ ] Manual (AppHost): edit an item's description in tab A with tab B on page 2 of the list → B's row updates, B stays on page 2, no list request in the network tab.
- [ ] Manual: disable an item under the "active" filter → row updates in place (limitation above, expected).

## Open Questions
1. **Fetch by id vs. event carries the DTO.** I recommend fetch by id: no backend change, and the by-id cache is warm after the first client. Costs one small request per client per event. OK?
2. **Accept the stale-position limitation**, or do you want the "reload when sort/filter touches the changed field" follow-up now?
3. **Scope:** also patch on `itemDisabled`/`itemEnabled` (included above), and leave `itemCreated` as a reload. OK?
