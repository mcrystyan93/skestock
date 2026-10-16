import { Component, DestroyRef, inject, linkedSignal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, required } from '@angular/forms/signals';
import { Events } from '@ngrx/signals/events';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzOptionComponent, NzSelectComponent } from 'ng-zorro-antd/select';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { ErrorAlert } from '@ske/shared/errors';
import { CloseSchoolClassStore, closeSchoolClassEvents } from '../../services/close-school-class.store';

export type CloseSchoolClassModalData = {
  classId: string;
  className: string;
};

@Component({
  imports: [
    NzModalTitleDirective,
    NzModalFooterDirective,
    NzAlertComponent,
    NzSelectComponent,
    NzOptionComponent,
    NzSpinComponent,
    NzSpaceComponent,
    NzSpaceItemDirective,
    NzButtonComponent,
    FormField,
    ErrorAlert
  ],
  selector: 'ske-close-school-class-modal',
  templateUrl: './close-school-class-modal.html',
  providers: [CloseSchoolClassStore]
})
export class CloseSchoolClassModal {
  public readonly data = inject<CloseSchoolClassModalData>(NZ_MODAL_DATA);
  public readonly store = inject(CloseSchoolClassStore);

  private readonly _modalRef = inject(NzModalRef);
  private readonly _messageService = inject(NzMessageService);
  private readonly _destroyRef = inject(DestroyRef);

  private readonly _model = linkedSignal(() => ({ targetClassId: null as string | null }));
  public readonly closeForm = form(this._model, (path) => {
    required(path.targetClassId);
  });

  private readonly _closedSub = inject(Events).on(closeSchoolClassEvents.closed)
    .pipe(takeUntilDestroyed(this._destroyRef))
    .subscribe(() => {
      this._messageService.success('Clasa a fost închisă, iar stocul a fost transferat.');
      this._modalRef.close(true);
    });

  public constructor() {
    this.store.loadTargets(this.data.classId);
  }

  public cancel() {
    this._modalRef.close(false);
  }

  public confirm() {
    const targetClassId = this._model().targetClassId;

    if (!targetClassId || this.store.closeLoading())
      return;

    this.store.close({ classId: this.data.classId, targetClassId });
  }
}
