import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { OrderListLineDto, SupplyListDto, SupplyListListItemDto } from '@ske/models';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { SupplyListsHttp } from '@ske/shared/supply-lists';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import {
  NZ_MODAL_DATA,
  NzModalFooterDirective,
  NzModalRef,
  NzModalTitleDirective,
} from 'ng-zorro-antd/modal';
import { forkJoin } from 'rxjs';
import { supplyListsToLines } from '../../../services/order-list-wizard.utils';
import { OrderListSupplyListStep } from '../wizard/steps/supply-list-step';

@Component({
  imports: [
    NzAlertComponent,
    NzButtonComponent,
    NzModalFooterDirective,
    NzModalTitleDirective,
    OrderListSupplyListStep,
  ],
  selector: 'ske-order-list-add-supply-lists-modal',
  template: `
    <div *nzModalTitle>Adaugă liste predefinite</div>

    @if (failed()) {
      <nz-alert nzType="error" nzShowIcon class="mb-3" [nzMessage]="failed()" />
    }

    <ske-order-list-supply-list-step
      [lists]="lists()"
      [loading]="loading()"
      [selectedIds]="selectedIds()"
      (selectedIdsChange)="selectedIds.set($event)"
    />

    <div *nzModalFooter>
      <button nz-button type="button" (click)="close()">Renunță</button>
      <button
        nz-button
        nzType="primary"
        type="button"
        [nzLoading]="adding()"
        [disabled]="selectedIds().length === 0 || adding()"
        (click)="add()"
      >
        Adaugă
      </button>
    </div>
  `,
})
export class OrderListAddSupplyListsModal {
  protected readonly lists = signal<SupplyListListItemDto[]>([]);
  protected readonly selectedIds = signal<string[]>([]);
  protected readonly loading = signal(true);
  protected readonly adding = signal(false);
  protected readonly failed = signal<string | null>(null);

  // Lists already merged into this order are hidden, so their quantities are not added twice.
  private readonly _addedIds = new Set(
    inject<OrderListAddSupplyListsModalData>(NZ_MODAL_DATA).addedIds,
  );
  private readonly _http = inject(SupplyListsHttp);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _nzModalRef = inject(
    NzModalRef<OrderListAddSupplyListsModal, OrderListAddSupplyListsResult>,
  );

  constructor() {
    this._http
      .getAllActive()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (lists) => {
          this.lists.set(lists.filter((list) => !this._addedIds.has(list.id)));
          this.loading.set(false);
        },
        error: () => {
          this.failed.set('Listele nu au putut fi încărcate. Încercați din nou.');
          this.loading.set(false);
        },
      });
  }

  protected close() {
    this._nzModalRef.close();
  }

  protected add() {
    this.adding.set(true);
    this.failed.set(null);

    forkJoin(this.selectedIds().map((id) => this._http.getById(id)))
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe({
        next: (selected: SupplyListDto[]) =>
          this._nzModalRef.close({
            listIds: this.selectedIds(),
            lines: supplyListsToLines(selected),
          }),
        error: () => {
          this.failed.set('Listele nu au putut fi adăugate. Încercați din nou.');
          this.adding.set(false);
        },
      });
  }
}

export type OrderListAddSupplyListsModalData = { addedIds: string[] };
export type OrderListAddSupplyListsResult = { listIds: string[]; lines: OrderListLineDto[] };
