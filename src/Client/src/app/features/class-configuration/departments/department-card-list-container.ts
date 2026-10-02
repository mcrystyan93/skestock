import {Component, DestroyRef, inject, ViewContainerRef} from '@angular/core';
import {NzButtonComponent} from 'ng-zorro-antd/button';
import {NzEmptyComponent} from 'ng-zorro-antd/empty';
import {NzIconDirective} from 'ng-zorro-antd/icon';
import {NzModalService} from 'ng-zorro-antd/modal';
import {NzTypographyComponent} from 'ng-zorro-antd/typography';
import {LoaderDirective} from '@ske/shared/loader';
import {ConfigurationStore} from '../services/configuration.store';
import {DepartmentTemplateDto} from '@ske/models';
import {ErrorAlert} from '@ske/shared/errors';
import {DepartmentCard} from './department-card';
import {DepartmentModal, DepartmentModalData} from './modal/department-modal';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';

@Component({
  imports: [
    NzButtonComponent,
    NzEmptyComponent,
    NzIconDirective,
    NzTypographyComponent,
    LoaderDirective,
    DepartmentCard,
    ErrorAlert
  ],
  selector: 'ske-department-card-list-container',
  templateUrl: './department-card-list-container.html'
})
export class DepartmentCardListContainer {
  public readonly store = inject(ConfigurationStore);
  private readonly _viewContainerRef = inject(ViewContainerRef);
  private readonly _modalService = inject(NzModalService);
  private readonly _destroyRef = inject(DestroyRef);

  protected deleteDepartment(department: DepartmentTemplateDto): void {
    if (this.store.departmentsLoading()) return;

    this._modalService.confirm({
      nzTitle: 'Confirmați ștergerea departamentului?',
      nzContent: 'Departamentul va fi șters definitiv. Doriți să continuați?',
      nzOkText: 'Șterge',
      nzCancelText: 'Anulează',
      nzOkDanger: true,
      nzCentered: true,
      nzIconType: 'icons:circle-exclamation',
      nzOnOk: () => {
        if (!this.store.departmentsLoading()) this.store.deleteDepartment(department.id);
      }
    });
  }

  protected openDepartment(department: DepartmentTemplateDto | null = null): void {
    if (this.store.departmentsLoading()) return;

    const modalRef = this._modalService.create<DepartmentModal, DepartmentModalData, DepartmentTemplateDto>({
      nzContent: DepartmentModal,
      nzData: {department},
      nzViewContainerRef: this._viewContainerRef,
      nzOnCancel: (instance) => {
        instance.close();
        return false;
      },
      nzCentered: true,
      nzMaskClosable: false,
      nzWrapClassName: 'modal-100 modal-lg-75 modal-xl-50'
    });

    modalRef.afterClose.pipe(takeUntilDestroyed(this._destroyRef)).subscribe((department) => {
      if (department) {
        this.store.loadDepartments();
      }
    });
  }
}
