import { Component, DestroyRef, inject, OnDestroy, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NzModalService } from 'ng-zorro-antd/modal';
import { CongregationDto, GetAllCongregationsRequest } from '@ske/models';
import { CongregationDetailModal } from '@ske/shared/congregations';
import { realtimeGroups, SignalRGroupManagerStore } from '@ske/signalr';
import { CongregationListState } from '../services/congregation-list.store';
import { Header } from './header/header';
import { CongregationListTab } from './tabs/congregation-list-tab';

@Component({
  imports: [
    Header,
    CongregationListTab
  ],
  selector: 'ske-congregations-page',
  templateUrl: './congregations.page.html',
  providers: [CongregationListState, NzModalService],
  host: {
    class: 'flex flex-col grow gap-4'
  }
})
export class CongregationsPage implements OnInit, OnDestroy {
  public readonly store = inject(CongregationListState);

  private readonly _modalService = inject(NzModalService);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _signalRGroupManager = inject(SignalRGroupManagerStore);

  public onFilterChange(filter: GetAllCongregationsRequest) {
    this.store.load(filter);
  }

  public onLoadMore() {
    this.store.loadMore();
  }

  public onAdd() {
    this.openCongregationModal();
  }

  public onEdit(congregation: CongregationDto) {
    this.openCongregationModal(congregation);
  }

  public onDelete(congregation: CongregationDto) {
    this._modalService.confirm({
      nzTitle: 'Confirmați ștergerea congregației?',
      nzContent: `Congregația „${congregation.name}” va fi ștearsă și nu va mai apărea în liste. Sunteți sigur că doriți să continuați?`,
      nzOkText: 'Șterge',
      nzCancelText: 'Nu',
      nzOkDanger: true,
      nzCentered: true,
      nzIconType: 'icons:circle-exclamation',
      nzOnOk: () => this.store.deleteCongregation(congregation)
    });
  }

  public ngOnInit() {
    this._signalRGroupManager.join(realtimeGroups.congregationsList);
  }

  public ngOnDestroy() {
    this._signalRGroupManager.leave(realtimeGroups.congregationsList);
  }

  private openCongregationModal(congregation: CongregationDto | null = null) {
    const modalRef = this._modalService.create({
      nzContent: CongregationDetailModal,
      nzData: {
        congregation
      },
      nzCentered: true,
      nzMaskClosable: false,
      nzWrapClassName: 'modal-100 modal-lg-75'
    });

    modalRef.afterClose.pipe(
      takeUntilDestroyed(this._destroyRef)
    ).subscribe(() => {
      this.store.reload();
    });
  }
}
