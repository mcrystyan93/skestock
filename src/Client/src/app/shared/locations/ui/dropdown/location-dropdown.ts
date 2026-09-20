import { Component, computed, effect, inject, input, linkedSignal, model, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, type FormValueControl } from '@angular/forms/signals';
import { GetAllLocationsRequest, type LocationDropdownValue, PAGINATION_PAGE_SIZE } from '@ske/models';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { LocationDropdownStore } from '../../services/location-dropdown.store';
import { NzOptionComponent, NzSelectComponent } from 'ng-zorro-antd/select';
import { NzSpinComponent } from 'ng-zorro-antd/spin';

@Component({
  selector: 'ske-location-dropdown',
  imports: [
    NzSelectComponent,
    FormField,
    NzSpinComponent,
    NzOptionComponent
  ],
  template: `
    <nz-select [formField]="locationForm.location"
               nzShowSearch
               nzShowArrow
               [nzLoading]="store.loading()"
               [nzAllowClear]="allowClear()"
               nzServerSearch
               class="w-full"
               [compareWith]="(a, b) => a && b ? a.id === b.id : a === b"
               (nzOnSearch)="onSearch($event)"
               [nzDropdownMatchSelectWidth]="false"
               [nzDropdownRender]="loadingMoreTemplate"
               (nzScrollToBottom)="loadMore()"
               [nzPlaceHolder]="placeholder()">
      @if (value(); as location) {
        @if (location.id !== excludedLocationId()) {
          <nz-option [nzValue]="location"
                     nzHide
                     [nzLabel]="selectedLocationLabel()"></nz-option>
        }
      }

      @for (location of availableLocations(); track location.id) {
        <nz-option [nzValue]="location"
                   [nzLabel]="location.name ?? ''"></nz-option>
      }
    </nz-select>

    <ng-template #loadingMoreTemplate>
      @if (store.isLoadingMore()) {
        <nz-spin></nz-spin>
      }
    </ng-template>
  `,
  providers: [LocationDropdownStore]
})
export class LocationDropdown implements FormValueControl<LocationDropdownValue> {
  public readonly value = model<LocationDropdownValue>(null);
  public readonly disabled = input<boolean>(false);
  public readonly allowClear = input<boolean>(false);
  public readonly placeholder = input<string>('Selectați o locație');
  public readonly prePopulateWithDefault = input<boolean>(false);
  public readonly excludedLocationId = input<string | null>(null);
  public readonly loadOnInit = input<boolean>(false);

  public readonly store = inject(LocationDropdownStore);
  private readonly _search$ = new Subject<string>();
  private readonly _defaultRequested = signal(false);
  private readonly _initialLoadRequested = signal(false);

  public readonly availableLocations = computed(() => {
    const excludedLocationId = this.excludedLocationId();

    return this.store.locations().filter(location => location.id !== excludedLocationId);
  });

  private readonly _formModel = linkedSignal({
    source: () => this.value(),
    computation: (value) => (<LocationDropdownFormModel>{ location: value })
  });

  public readonly locationForm = form(this._formModel);

  private readonly _formLocationChangeEffectRef = effect(() => {
    const location = this.locationForm.location().value();

    untracked(() => this.value.set(location));
  });

  private readonly _selectedValueEffectRef = effect(() => {
    const location = this.value();

    untracked(() => this.store.resolveSelectedLocation(location));
  });

  // When enabled, request the default location once (SignalStore owns the async fetch + state).
  private readonly _requestDefaultEffectRef = effect(() => {
    if (!this.prePopulateWithDefault() || untracked(this._defaultRequested)) {
      return;
    }

    this._defaultRequested.set(true);
    this.store.loadDefault();
  });

  private readonly _loadOnInitEffectRef = effect(() => {
    if (!this.loadOnInit() || this._initialLoadRequested()) {
      return;
    }

    this._initialLoadRequested.set(true);
    untracked(() => this.store.load(this.buildFilter({ searchTerm: null })));
  });

  // Apply the resolved default only while the user hasn't already picked/cleared a value.
  private readonly _applyDefaultEffectRef = effect(() => {
    const defaultLocation = this.store.defaultLocation();

    if (!this.prePopulateWithDefault() || !defaultLocation) {
      return;
    }

    untracked(() => {
      if (!this.value()) {
        this.value.set(defaultLocation);
      }
    });
  });

  private readonly _searchSub = this._search$
    .pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntilDestroyed()
    )
    .subscribe((searchTerm) => {
      this.store.load(this.buildFilter({ searchTerm }));
    });

  public readonly selectedLocationLabel = computed(() => {
    const location = this.value();

    if (!location?.id)
      return '';

    const resolvedLocation = this.store.selectedLocation();

    if (resolvedLocation?.id === location.id && resolvedLocation.name)
      return this.locationLabel(resolvedLocation);

    if (this.store.selectedLocationLoading())
      return 'Se încarcă locația…';

    if (this.store.selectedLocationUnavailable())
      return `Locație indisponibilă`;

    return location.name ? this.locationLabel(location) : 'Se încarcă locația…';
  });

  public loadMore() {
    this.store.loadMore();
  }

  public onSearch(searchTerm: string) {
    this._search$.next(searchTerm);
  }

  public locationLabel(location: LocationDropdownValue): string {
    return location?.name ?? 'Locație indisponibilă';
  }

  private buildFilter(
    partialFilter: Partial<GetAllLocationsRequest>
  ): GetAllLocationsRequest {
    return {
      ...partialFilter,
      pageSize: partialFilter.pageSize ?? PAGINATION_PAGE_SIZE,
      filters: partialFilter.filters ?? untracked(() => this.store.filter().filters),
      sort: [{
        value: 'ascend',
        key: 'name'
      }]
    };
  }

}

type LocationDropdownFormModel = {
  location: LocationDropdownValue
};
