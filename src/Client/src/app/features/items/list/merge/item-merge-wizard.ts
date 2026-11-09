import { Component, DestroyRef, computed, effect, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { MergeItemsRequest } from '@ske/models';
import { ItemsHttp } from '@ske/shared/items';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzStepComponent, NzStepsComponent } from 'ng-zorro-antd/steps';
import { ItemListState } from '../../services/item-list.store';
import { ItemMergeConfirmStep } from './steps/confirm-step';
import { ItemMergeImpactStep } from './steps/impact-step';
import { ItemMergePrimaryStep } from './steps/primary-step';

@Component({
  imports: [
    ItemMergeConfirmStep,
    ItemMergeImpactStep,
    ItemMergePrimaryStep,
    NzButtonComponent,
    NzModalFooterDirective,
    NzModalTitleDirective,
    NzStepComponent,
    NzStepsComponent
  ],
  selector: 'ske-item-merge-wizard',
  template: `
    <div *nzModalTitle>Unește articole</div>

    <nz-steps class="mb-6!"
              nzSize="small"
              nzLabelPlacement="vertical"
              [nzCurrent]="step()">
      <nz-step nzTitle="Alege item principal" />
      <nz-step nzTitle="Date afectate" />
      <nz-step nzTitle="Confirmare" />
    </nz-steps>

    @switch (step()) {
      @case (0) {
        <ske-item-merge-primary-step [items]="items()"
                                     [primaryId]="primaryId()"
                                     (primaryIdChange)="primaryId.set($event)" />
      }
      @case (1) {
        <ske-item-merge-impact-step [impacts]="preview.value()?.impacts ?? []"
                                    [loading]="preview.isLoading()"
                                    [errorMessage]="preview.error() ? previewError() : null"
                                    (retry)="preview.reload()" />
      }
      @case (2) {
        <ske-item-merge-confirm-step [primary]="primary()"
                                     [secondaries]="secondaries()"
                                     [problemDetail]="store.mergeProblemDetail()"
                                     [validationErrors]="store.mergeValidationErrors()" />
      }
    }
    <div *nzModalFooter>
      <button nz-button
              type="button"
              [disabled]="store.merging()"
              (click)="close()">
        Anulează
      </button>
      @if (step() > 0) {
        <button nz-button
                type="button"
                [disabled]="store.merging()"
                (click)="back()">
          Înapoi
        </button>
      }
      @if (step() < 2) {
        <button nz-button
                nzType="primary"
                type="button"
                [disabled]="!canAdvance()"
                (click)="next()">
          Înainte
        </button>
      } @else {
        <button nz-button
                nzType="primary"
                nzDanger
                type="button"
                [nzLoading]="store.merging()"
                (click)="confirm()">
          Unește articolele
        </button>
      }
    </div>
  `
})
export class ItemMergeWizard {
  protected readonly store = inject(ItemListState);
  // Read from the store so row updates (e.g. a realtime disable) reach the wizard.
  protected readonly items = this.store.selectedItems;

  private readonly _modalRef = inject(NzModalRef);
  private readonly _http = inject(ItemsHttp);

  constructor() {
    // Esc would close the modal mid-request; the mask is already not closable.
    effect(() => this._modalRef.updateConfig({ nzKeyboard: !this.store.merging() }));
    // Covers every dismissal path (Esc, close button, cancel, success) so a stale error never shows next time.
    inject(DestroyRef).onDestroy(() => this.store.clearMergeErrors());
  }

  public readonly step = signal(0);
  public readonly primaryId = signal<string | null>(null);

  protected readonly primary = computed(() => this.items().find((i) => i.id === this.primaryId()) ?? null);
  protected readonly secondaries = computed(() => this.items().filter((i) => i.id !== this.primaryId()));

  private readonly _request = computed<MergeItemsRequest | null>(() => {
    const primaryItemId = this.primaryId();

    return primaryItemId ? { primaryItemId, itemIds: this.items().map((i) => i.id) } : null;
  });

  // The preview is fetched on entering step 2 and dropped when going back, so it is always exact for the chosen primary.
  protected readonly preview = rxResource({
    params: () => this.step() === 1 ? this._request() ?? undefined : undefined,
    stream: ({ params }) => this._http.mergePreview(params)
  });

  protected readonly previewError = computed(() => {
    const error = this.preview.error();
    const cause = error instanceof HttpErrorResponse ? error : (error as { cause?: unknown } | undefined)?.cause;
    const status = cause instanceof HttpErrorResponse ? cause.status : null;

    return status === 403
      ? 'Nu aveți permisiunea de a efectua această acțiune.'
      : 'A apărut o eroare neașteptată.';
  });

  protected readonly canAdvance = computed(() =>
    this.primaryId() !== null && (this.step() !== 1 || (!this.preview.isLoading() && !this.preview.error()))
  );

  public next() {
    if (this.canAdvance())
      this.step.update((s) => s + 1);
  }

  public back() {
    this.step.update((s) => s - 1);
  }

  public confirm() {
    const request = this._request();

    if (!request || this.store.merging())
      return;

    this.store.merge({ request, onSuccess: () => this._modalRef.close(true) });
  }

  public close() {
    this._modalRef.close(false);
  }
}
