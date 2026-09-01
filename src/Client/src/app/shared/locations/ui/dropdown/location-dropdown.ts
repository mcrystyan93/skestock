import { Component, effect, inject, input, linkedSignal, model, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, type FormValueControl } from '@angular/forms/signals';
import { type LocationDropdownValue, GetAllLocationsRequest, PAGINATION_PAGE_SIZE } from '@ske/models';
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
               [nzLoading]="store.locationsLoading()"
               [nzAllowClear]="allowClear()"
               nzServerSearch
               class="w-full"
               [compareWith]="(a, b) => a && b ? a.id === b.id : a === b"
               (nzOnSearch)="onSearch($event)"
               [nzDropdownRender]="loadingMoreTemplate"
               (nzScrollToBottom)="loadMore()">
      @if (value(); as location) {
        <nz-option [nzValue]="location"
                   nzHide
                   [nzLabel]="location.name ?? ''"></nz-option>
      }

      @for (location of store.locations(); track location.id) {
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

  public readonly store = inject(LocationDropdownStore);
  private readonly _search$ = new Subject<string>();

  private readonly _formModel = linkedSignal({
    source: () => this.value(),
    computation: (value) => (<LocationDropdownFormModel>{ location: value })
  });

  public readonly locationForm = form(this._formModel);

  private readonly _formLocationChangeEffectRef = effect(() => {
    const location = this.locationForm.location().value();

    untracked(() => this.value.set(location));
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

  public loadMore() {
    this.store.loadMore();
  }

  public onSearch(searchTerm: string) {
    this._search$.next(searchTerm);
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
