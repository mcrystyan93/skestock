import { Component, DestroyRef, effect, inject, signal, viewChild } from '@angular/core';
import {
  NEW_SCHOOL_CLASS_ROUTE_ID,
  schoolClassApiEvents,
  SchoolClassDetailState
} from '../../services/school-class-detail.store';
import { CreateSchoolClassRequest, SchoolClassDto, UpdateSchoolClassRequest } from '@ske/models';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzSpaceCompactComponent, NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { Form, SchoolClassFormModel } from './form';
import { isNil } from 'lodash-es';
import { Events } from '@ngrx/signals/events';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NzMessageService } from 'ng-zorro-antd/message';
import { BehaviorSubject, filter, switchMap, tap } from 'rxjs';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzIconDirective } from 'ng-zorro-antd/icon';

@Component({
  imports: [
    NzModalTitleDirective,
    NzModalFooterDirective,
    NzSpaceComponent,
    NzButtonComponent,
    Form,
    NzSpaceItemDirective,
    NzSpaceCompactComponent,
    NzDropdownMenuComponent,
    NzMenuDirective,
    NzMenuItemComponent,
    NzIconDirective,
    NzDropdownDirective
  ],
  selector: 'ske-school-class-detail-modal',
  styles: ``,
  templateUrl: './school-class-detail-modal.html',
  providers: [SchoolClassDetailState]
})
export class SchoolClassDetailModal {
  public readonly modalData = signal<SchoolClassDetailModalData>(inject(NZ_MODAL_DATA));
  public readonly store = inject(SchoolClassDetailState);

  private initialLoad = false;

  private readonly _nzModalRef = inject(NzModalRef);
  private readonly _storeEvents = inject(Events);
  private readonly _formComponent = viewChild(Form);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _nzMessageService = inject(NzMessageService);
  private readonly _close$ = new BehaviorSubject(false);

  private readonly _initialLoadEffectRef = effect(() => {
    if (this.initialLoad)
      return;

    const { schoolClass } = this.modalData();

    this.store.loadSchoolClass(schoolClass?.id ?? NEW_SCHOOL_CLASS_ROUTE_ID);
    this.initialLoad = true;
  });

  private readonly _saveSuccessRef = this._storeEvents.on(schoolClassApiEvents.saveSuccess)
    .pipe(
      takeUntilDestroyed(this._destroyRef),
      tap(() => this._nzMessageService.success('Clasa a fost salvată cu succes!')),
      switchMap(() => this._close$),
      filter((shouldClose) => shouldClose),
      tap(() => this.close())
    )
    .subscribe();


  public close() {
    this._nzModalRef.close();
  }

  public async save(shouldClose: boolean = true) {
    const formComponent = this._formComponent();

    if (isNil(formComponent))
      return;

    const { isValid, formData } = await formComponent.submit();

    if (!isValid || isNil(formData))
      return;

    this.store.saveSchoolClass(this.mapSaveRequest(formData));

    if (shouldClose)
      this._close$.next(true);
  }

  private mapSaveRequest(formData: SchoolClassFormModel): CreateSchoolClassRequest | UpdateSchoolClassRequest {
    return {
      name: formData.name,
      startDate: this.toDateOnlyString(formData.startDate),
      endDate: this.toDateOnlyString(formData.endDate),
      status: formData.status
    };
  }

  private toDateOnlyString(date: Date | null): string {
    if (isNil(date))
      return '';

    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');

    return `${year}-${month}-${day}`;
  }
}

type SchoolClassDetailModalData = {
  schoolClass: SchoolClassDto | null;
}
