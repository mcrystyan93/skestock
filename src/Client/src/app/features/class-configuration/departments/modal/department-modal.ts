import { Component, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Events } from '@ngrx/signals/events';
import { departmentApiEvents, DepartmentDetailState } from '../../services/department-detail.store';
import { NzSpaceCompactComponent, NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { DepartmentTemplateDto } from '@ske/models';
import { ErrorAlert } from '@ske/shared/errors';
import { DepartmentForm } from './form';

export type DepartmentModalData = {
  department: DepartmentTemplateDto | null;
};

@Component({
  imports: [
    DepartmentForm,
    ErrorAlert,
    NzButtonComponent,
    NzModalFooterDirective,
    NzModalTitleDirective,
    NzSpaceComponent,
    NzSpaceItemDirective,
    NzSpaceCompactComponent,
    NzDropdownDirective,
    NzDropdownMenuComponent,
    NzMenuDirective,
    NzMenuItemComponent,
    NzIconDirective
  ],
  selector: 'ske-configuration-department-modal',
  templateUrl: './department-modal.html',
  providers: [DepartmentDetailState]
})
export class DepartmentModal {
  public readonly modalData = signal<DepartmentModalData>(inject(NZ_MODAL_DATA));
  public readonly store = inject(DepartmentDetailState);

  protected readonly formComponent = viewChild(DepartmentForm);

  private readonly _modalRef = inject(NzModalRef<DepartmentModal, DepartmentTemplateDto>);

  private _shouldCloseAfterSave = false;
  private readonly _saveSuccessSubscription = inject(Events)
    .on(departmentApiEvents.saveSuccess)
    .pipe(takeUntilDestroyed())
    .subscribe(() => {
      if (this._shouldCloseAfterSave) this._modalRef.close(this.store.department() ?? undefined);
    });

  constructor() {
    this.store.loadDepartment(this.modalData().department?.id ?? null);
  }

  public close(): void {
    if (!this.store.departmentLoading()) this._modalRef.close();
  }

  protected async save(shouldClose: boolean = true): Promise<void> {
    const formComponent = this.formComponent();
    if (!formComponent)
      return;

    const { isValid, formData } = await formComponent.submit();

    if (!isValid || !formData) return;

    this._shouldCloseAfterSave = shouldClose;
    
    this.store.saveDepartment(formData);
  }
}
