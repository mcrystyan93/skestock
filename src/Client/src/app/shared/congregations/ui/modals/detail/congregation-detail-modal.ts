import { Component, computed, DestroyRef, effect, inject, signal, viewChild } from '@angular/core';
import { congregationApiEvents, CongregationDetailState, NEW_CONGREGATION_ROUTE_ID } from '../../../services/congregation-detail.store';
import { CongregationDto, CreateCongregationRequest, UpdateCongregationRequest } from '@ske/models';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzSpaceCompactComponent, NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { CongregationForm, CongregationFormModel } from './form';
import { isNil } from 'lodash-es';
import { Events } from '@ngrx/signals/events';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NzMessageService } from 'ng-zorro-antd/message';
import { BehaviorSubject, filter, switchMap, tap } from 'rxjs';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { ErrorAlert, ErrorMessageService } from '@ske/shared/errors';

@Component({
  imports: [
    NzModalTitleDirective,
    NzModalFooterDirective,
    NzSpaceComponent,
    NzButtonComponent,
    CongregationForm,
    NzSpaceItemDirective,
    NzSpaceCompactComponent,
    NzDropdownMenuComponent,
    NzMenuDirective,
    NzMenuItemComponent,
    NzIconDirective,
    NzDropdownDirective,
    ErrorAlert
  ],
  selector: 'ske-congregation-detail-modal',
  styles: ``,
  templateUrl: './congregation-detail-modal.html',
  providers: [CongregationDetailState]
})
export class CongregationDetailModal {
  public readonly modalData = signal<CongregationDetailModalData>(inject(NZ_MODAL_DATA));
  public readonly store = inject(CongregationDetailState);

  // Server errors for these form fields are shown under the field; everything else stays in the alert.
  private static readonly FIELD_ERROR_NAMES = ['name', 'email'];

  public readonly fieldErrors = computed(() => {
    const result: Record<string, string[]> = {};

    for (const item of this.store.congregationValidationErrors()?.error?.errors ?? []) {
      const field = item.field?.toLowerCase();

      if (field && CongregationDetailModal.FIELD_ERROR_NAMES.includes(field))
        (result[field] ??= []).push(this._errorMessages.resolveItem(item));
    }

    return result;
  });

  public readonly alertValidationErrors = computed(() => {
    const validationErrors = this.store.congregationValidationErrors();
    const remaining = (validationErrors?.error.errors ?? []).filter(
      (item) => !CongregationDetailModal.FIELD_ERROR_NAMES.includes(item.field?.toLowerCase() ?? '')
    );

    return validationErrors && remaining.length > 0
      ? { ...validationErrors, error: { ...validationErrors.error, errors: remaining } }
      : null;
  });

  private initialLoad = false;

  private readonly _nzModalRef = inject(NzModalRef);
  private readonly _storeEvents = inject(Events);
  private readonly _formComponent = viewChild(CongregationForm);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _nzMessageService = inject(NzMessageService);
  private readonly _errorMessages = inject(ErrorMessageService);
  private readonly _close$ = new BehaviorSubject(false);

  private readonly _initialLoadEffectRef = effect(() => {
    if (this.initialLoad)
      return;

    const { congregation, prefill } = this.modalData();

    this.store.loadCongregation({ id: congregation?.id ?? NEW_CONGREGATION_ROUTE_ID, prefill: congregation ? null : prefill });
    this.initialLoad = true;
  });

  private readonly _saveSuccessRef = this._storeEvents.on(congregationApiEvents.saveSuccess)
    .pipe(
      takeUntilDestroyed(this._destroyRef),
      tap(() => this._nzMessageService.success('Congregația a fost salvată cu succes!')),
      switchMap(() => this._close$),
      filter((shouldClose) => shouldClose),
      tap(() => this.close(this.store.congregation()))
    )
    .subscribe();


  public close(savedCongregation: Partial<CongregationDto> | null = null) {
    this._nzModalRef.close(savedCongregation);
  }

  public async save(shouldClose: boolean = true) {
    const formComponent = this._formComponent();

    if (isNil(formComponent))
      return;

    const { isValid, formData } = await formComponent.submit();

    if (!isValid || isNil(formData))
      return;

    // Set on every save so a previous "save and close" that failed cannot close a later plain "save".
    this._close$.next(shouldClose);
    this.store.saveCongregation(this.mapSaveRequest(formData));
  }

  private mapSaveRequest(formData: CongregationFormModel): CreateCongregationRequest | UpdateCongregationRequest {
    return {
      name: formData.name,
      email: formData.email,
      coordinator: formData.coordinator,
      coordinatorPhone: formData.coordinatorPhone,
      dessertContactPerson: formData.dessertContactPerson,
      dessertContactPhone: formData.dessertContactPhone,
      canReceiveStudents: formData.canReceiveStudents,
      isSmallCongregation: formData.isSmallCongregation,
      message: formData.message
    };
  }
}

type CongregationDetailModalData = {
  congregation: CongregationDto | null;
  prefill?: Partial<CongregationDto> | null;
}
