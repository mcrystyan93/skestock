import { Component, DestroyRef, computed, effect, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { ITEM_MERGE_KIND_LABELS, MergeItemsRequest } from '@ske/models';
import { ErrorAlert } from '@ske/shared/errors';
import { ItemsHttp } from '@ske/shared/items';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzRadioComponent, NzRadioGroupComponent } from 'ng-zorro-antd/radio';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzStepComponent, NzStepsComponent } from 'ng-zorro-antd/steps';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { CHECKABLE_CARD_CLASSES } from '../../../../shared/ui/card-classes';
import { ItemListState } from '../../services/item-list.store';

@Component({
  imports: [
    FormsModule,
    ErrorAlert,
    NzAlertComponent,
    NzButtonComponent,
    NzModalFooterDirective,
    NzModalTitleDirective,
    NzRadioComponent,
    NzRadioGroupComponent,
    NzSpinComponent,
    NzStepComponent,
    NzStepsComponent,
    NzTypographyComponent
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
        <p id="merge-primary-label"
           nz-typography
           nzType="secondary">
          Alege articolul care rămâne. Celelalte vor fi dezactivate, iar datele lor vor fi mutate pe articolul ales.
        </p>
        <nz-radio-group class="flex! flex-col gap-2"
                        nzName="primaryItem"
                        aria-labelledby="merge-primary-label"
                        [ngModel]="primaryId()"
                        (ngModelChange)="primaryId.set($event)">
          @for (item of items(); track item.id) {
            <label nz-radio
                   [class]="cardClasses"
                   [nzValue]="item.id"
                   [nzDisabled]="!item.isActive">
              <span class="block font-medium">{{ item.name }}</span>
              <span class="block text-sm text-slate-500">
                {{ item.sku || 'fără cod' }} · {{ item.unit }} · {{ item.categoryName || '—' }}
                @if (!item.isActive) {
                  · inactiv (nu poate fi principal)
                }
              </span>
            </label>
          }
        </nz-radio-group>
      }
      @case (1) {
        @if (preview.isLoading()) {
          <div class="flex justify-center py-8">
            <nz-spin nzTip="Se calculează datele afectate..." />
          </div>
        } @else if (preview.error()) {
          <nz-alert nzType="error"
                    nzShowIcon
                    nzMessage="Previzualizarea nu a putut fi încărcată"
                    [nzDescription]="previewError()" />
          <button nz-button
                  class="mt-3"
                  type="button"
                  (click)="preview.reload()">
            Reîncearcă
          </button>
        } @else {
          @let impacts = preview.value()?.impacts ?? [];
          @if (impacts.length === 0) {
            <p nz-typography
               nzType="secondary">Nu există date de mutat. Articolele secundare vor fi doar dezactivate.</p>
          } @else {
            <p nz-typography
               nzType="secondary">Datele de mai jos vor fi legate de articolul principal.</p>
            <ul class="m-0 flex list-none flex-col gap-2 p-0">
              @for (impact of impacts; track impact.kind) {
                <li class="flex flex-col gap-1 rounded border border-solid border-slate-200 p-3">
                  <div class="flex items-baseline justify-between gap-3">
                    <span class="font-medium">{{ labels[impact.kind] }}</span>
                    <span>{{ impact.relinked }} mutate</span>
                  </div>
                  @if (impact.combined > 0) {
                    <span class="text-sm text-amber-600">
                      {{ impact.combined }} se vor combina (însumat) cu înregistrările articolului principal.
                    </span>
                  }
                </li>
              }
            </ul>
          }
        }
      }
      @case (2) {
        <nz-alert nzType="warning"
                  nzShowIcon
                  class="mb-4"
                  nzMessage="Acțiune ireversibilă"
                  nzDescription="Articolele secundare vor fi dezactivate, iar datele lor vor fi mutate definitiv pe articolul principal, în toate clasele." />
        <p class="m-0! mb-3!">
          Se păstrează: <strong>{{ primary()?.name }}</strong>
        </p>
        <p class="m-0!">Se dezactivează:</p>
        <ul class="m-0! mb-3! list-disc pl-6">
          @for (item of secondaries(); track item.id) {
            <li>{{ item.name }}</li>
          }
        </ul>
        <ske-error-display [problemDetail]="store.mergeProblemDetail()"
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
  protected readonly cardClasses = `m-0! flex! items-start p-3! ${CHECKABLE_CARD_CLASSES}`;
  protected readonly labels = ITEM_MERGE_KIND_LABELS;

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
