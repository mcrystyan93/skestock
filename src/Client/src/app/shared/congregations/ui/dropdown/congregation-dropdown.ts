import {
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  input,
  linkedSignal,
  model,
  TemplateRef,
  untracked
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { disabled, form, FormField, type FormValueControl } from '@angular/forms/signals';
import {
  type CongregationDropdownValue,
  type CongregationDto,
  ColumnFilter,
  GetAllCongregationsRequest,
  PAGINATION_PAGE_SIZE
} from '@ske/models';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { CongregationDropdownStore } from '../../services/congregation-dropdown.store';
import { NzOptionComponent, NzSelectComponent } from 'ng-zorro-antd/select';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzSpaceCompactComponent } from 'ng-zorro-antd/space';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { isNil } from 'lodash-es';
import { CongregationDetailModal } from '../modals/detail/congregation-detail-modal';
import { NzModalService } from 'ng-zorro-antd/modal';
import { NgTemplateOutlet } from '@angular/common';

@Component({
  selector: 'ske-congregation-dropdown',
  imports: [
    NzSelectComponent,
    FormField,
    NzSpinComponent,
    NzOptionComponent,
    NzSpaceCompactComponent,
    NzButtonComponent,
    NzIconDirective,
    NgTemplateOutlet
  ],
  template: `
    <nz-space-compact class="w-full">
      <nz-select [formField]="congregationForm.congregation"
                 [nzPlaceHolder]="placeholder()"
                 nzShowSearch
                 nzShowArrow
                 [nzLoading]="store.loading()"
                 [nzAllowClear]="allowClear()"
                 nzServerSearch
                 class="w-full"
                 [compareWith]="compareCongregations"
                 (nzOnSearch)="onSearch($event)"
                 [nzDropdownMatchSelectWidth]="false"
                 [nzDropdownRender]="loadingMoreTemplate"
                 (nzScrollToBottom)="loadMore()">
        @if (value(); as congregation) {
          <nz-option [nzValue]="congregation"
                     nzHide
                     [nzLabel]="selectedCongregationLabel()"></nz-option>
        }

        @for (congregation of store.congregations(); track congregation.id) {
          <nz-option [nzValue]="congregation"
                     [nzLabel]="congregation.name ?? ''"></nz-option>
        }
      </nz-select>
      @if (allowEdit() && value()?.id && !congregationForm().disabled()) {
        <button nz-button
                nzType="default"
                type="button"
                (click)="onEdit(value())">
          @if (editButtonTemplate(); as editTemplate) {
            <ng-container *ngTemplateOutlet="editTemplate"></ng-container>
          } @else {
            <nz-icon nzType="icons:pencil"></nz-icon>
          }
        </button>
      }
      @if (allowCreate() && !congregationForm().disabled()) {
        <button nz-button
                nzType="default"
                type="button"
                (click)="onAdd()">
          @if (createButtonTemplate(); as createTemplate) {
            <ng-container *ngTemplateOutlet="createTemplate"></ng-container>
          } @else {
            <nz-icon nzType="icons:plus"></nz-icon>
          }
        </button>
      }
    </nz-space-compact>

    <ng-template #loadingMoreTemplate>
      @if (store.isLoadingMore()) {
        <nz-spin></nz-spin>
      }
    </ng-template>
  `,
  providers: [CongregationDropdownStore, NzModalService],
})
export class CongregationDropdown implements FormValueControl<CongregationDropdownValue> {
  public readonly value = model<CongregationDropdownValue>(null);
  public readonly disabled = input<boolean>(false);
  public readonly allowClear = input<boolean>(false);
  public readonly allowEdit = input<boolean>(true);
  public readonly allowCreate = input<boolean>(true);
  public readonly placeholder = input<string>('Selectați o congregație');
  public readonly createPrefill = input<Partial<CongregationDto> | null>(null);
  public readonly createButtonTemplate = input<TemplateRef<unknown> | null>(null);
  /** When true, only congregations that can receive students are offered. */
  public readonly canReceiveStudents = input<boolean>(false);
  public readonly editButtonTemplate = input<TemplateRef<unknown> | null>(null);

  public readonly store = inject(CongregationDropdownStore);
  private readonly _search$ = new Subject<string>();
  private readonly _modalService = inject(NzModalService);
  private readonly _destroyRef = inject(DestroyRef);

  private readonly _formModel = linkedSignal({
    source: () => this.value(),
    computation: (value) => <CongregationDropdownFormModel>{congregation: value},
  });

  public readonly congregationForm = form(this._formModel, (schemaPath) => {
    disabled(schemaPath, {when: () => this.disabled()});
  });

  private readonly _formCongregationChangeEffectRef = effect(() => {
    const congregation = this.congregationForm.congregation().value();

    untracked(() => this.value.set(congregation));
  });

  private readonly _searchSub = this._search$
    .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
    .subscribe((searchTerm) => {
      this.store.load(this.buildFilter({searchTerm}));
    });

  private readonly _selectedValueEffectRef = effect(() => {
    const congregation = this.value();

    untracked(() => this.store.resolveSelectedCongregation(congregation));
  });

  private readonly _canReceiveStudentsEffectRef = effect(() => {
    const canReceiveStudents = this.canReceiveStudents();

    // Only refresh options that were already loaded; the first load still happens on search.
    untracked(() => {
      if (this.store.paginationData() !== null)
        this.store.load(this.buildFilter({ searchTerm: this.store.filter().searchTerm, filters: this.buildFilters(canReceiveStudents) }));
    });
  });

  public readonly selectedCongregationLabel = computed(() => {
    const congregation = this.value();

    if (!congregation?.id) return '';

    const resolvedCongregation = this.store.selectedCongregation();

    if (resolvedCongregation?.id === congregation.id && resolvedCongregation.name)
      return this.congregationLabel(resolvedCongregation);

    if (this.store.selectedCongregationLoading()) return 'Se încarcă congregația…';

    if (this.store.selectedCongregationUnavailable())
      return 'Congregație indisponibilă';

    return congregation.name ? this.congregationLabel(congregation) : 'Se încarcă congregația…';
  });

  public readonly compareCongregations = (a: CongregationDropdownValue, b: CongregationDropdownValue) =>
    a && b ? a.id === b.id : a === b;

  public loadMore() {
    this.store.loadMore();
  }

  public onSearch(searchTerm: string) {
    this._search$.next(searchTerm);
  }

  public congregationLabel(congregation: CongregationDropdownValue): string {
    return congregation?.name ?? 'Congregație indisponibilă';
  }

  public onAdd() {
    this.openCongregationModal(null, this.createPrefill());
  }

  public onEdit(congregation: CongregationDropdownValue) {
    if (isNil(congregation)) return;

    this.openCongregationModal(congregation as CongregationDto);
  }

  private openCongregationModal(
    congregation: CongregationDto | null = null,
    prefill: Partial<CongregationDto> | null = null,
  ) {
    const modalRef = this._modalService.create({
      nzContent: CongregationDetailModal,
      nzData: {
        congregation,
        prefill,
      },
      nzCentered: true,
      nzMaskClosable: false,
    });

    modalRef.afterClose
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe((savedCongregation: Partial<CongregationDto> | null) => {
        if (savedCongregation?.id) this.value.set(savedCongregation);

        this.store.load(this.store.filter());
      });
  }

  private buildFilters(canReceiveStudents = this.canReceiveStudents()): ColumnFilter[] {
    return canReceiveStudents
      ? [{ field: 'canReceiveStudents', operator: 'equals', value: true, fieldType: 'boolean' }]
      : [];
  }

  private buildFilter(partialFilter: Partial<GetAllCongregationsRequest>): GetAllCongregationsRequest {
    return {
      ...partialFilter,
      pageSize: partialFilter.pageSize ?? PAGINATION_PAGE_SIZE,
      filters: partialFilter.filters ?? this.buildFilters(),
      sort: [
        {
          value: 'ascend',
          key: 'name',
        },
      ],
    };
  }
}

type CongregationDropdownFormModel = {
  congregation: CongregationDropdownValue;
};
