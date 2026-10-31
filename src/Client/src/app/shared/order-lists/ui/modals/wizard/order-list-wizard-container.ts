import { Component, computed, inject, input } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ErrorAlert } from '@ske/shared/errors';
import { OrderListWizardStore } from '@ske/shared/order-lists/services';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { gridResponsiveMap, NzBreakpointService } from 'ng-zorro-antd/core/services';
import { NzProgressComponent } from 'ng-zorro-antd/progress';
import { NzStepComponent, NzStepsComponent } from 'ng-zorro-antd/steps';
import { OrderListDetailsStep } from './steps/details-step';
import { OrderListSourceStep } from './steps/source-step';
import { OrderListSuggestionsStep } from './steps/suggestions-step';
import { OrderListSupplyListStep } from './steps/supply-list-step';

/** The wizard's stepper and its two pre-editor steps; the editor itself stays with the modal. */
@Component({
  imports: [
    ErrorAlert,
    NzButtonComponent,
    NzProgressComponent,
    NzStepsComponent,
    NzStepComponent,
    OrderListDetailsStep,
    OrderListSourceStep,
    OrderListSuggestionsStep,
    OrderListSupplyListStep
  ],
  selector: 'ske-order-list-wizard-container',
  template: `
    @if (direction() === 'horizontal') {
      <nz-steps class="mb-4"
                nzSize="small"
                [nzCurrent]="wizard.stepIndex()"
                nzLabelPlacement="vertical">
        @for (title of titles(); track $index) {
          <nz-step [nzTitle]="title" />
        }
      </nz-steps>
    } @else {
      <div class="mb-3"
           role="status">
        <div class="text-sm font-medium">
          Pasul {{ wizard.stepIndex() + 1 }} din {{ titles().length }} · {{ titles()[wizard.stepIndex()] }}
        </div>
        <nz-progress [nzPercent]="(wizard.stepIndex() + 1) / titles().length * 100"
                     [nzShowInfo]="false"
                     nzSize="small" />
      </div>
    }

    @if (wizard.step() === 'source') {
      <ske-order-list-source-step [source]="wizard.source()"
                                  (sourceChange)="wizard.setSource($event)" />
    } @else if (wizard.step() === 'selection') {
      <ske-error-display [problemDetail]="wizard.activeProblemDetail()"
                         [validationErrors]="wizard.activeValidationErrors()" />
      @if (wizard.loadFailed()) {
        @if (!wizard.activeProblemDetail()) {
          <p class="mb-1 mt-0" role="alert">Datele nu au putut fi încărcate.</p>
        }
        <button nz-button
                nzType="link"
                type="button"
                (click)="wizard.retry(classId())">
          Reîncearcă
        </button>
      }
      @if (wizard.fromSupplyList()) {
        <ske-order-list-supply-list-step [lists]="wizard.supplyLists()"
                                         [selectedIds]="wizard.supplyListIds()"
                                         [loading]="wizard.supplyListsLoading()"
                                         [summaryLoading]="wizard.selectedSupplyListsLoading()"
                                         [distinctItemCount]="wizard.distinctSelectedItemCount()"
                                         (selectedIdsChange)="wizard.selectSupplyLists($event)" />
      } @else {
        <ske-order-list-suggestions-step [items]="wizard.suggestions()"
                                         [selectedIds]="wizard.selectedItemIds()"
                                         [loading]="wizard.suggestionsLoading()"
                                         (selectedIdsChange)="wizard.setSelectedItemIds($event)" />
      }
    } @else if (wizard.step() === 'details') {
      <ske-order-list-details-step [name]="wizard.name()"
                                   [note]="wizard.note()"
                                   (detailsChange)="wizard.setDetails($event)" />
    }
  `
})
export class OrderListWizardContainer {
  public readonly classId = input<string | null>(null);

  protected readonly wizard = inject(OrderListWizardStore);

  private readonly _breakpoints = toSignal(
    inject(NzBreakpointService).subscribe(gridResponsiveMap, true),
    { initialValue: null }
  );

  protected readonly titles = computed(() => [
    'Sursă',
    this.wizard.fromSupplyList() ? 'Listă predefinită' : 'Articole sugerate',
    'Detalii',
    'Articole comandă'
  ]);

  protected readonly direction = computed(() => this._breakpoints()?.md ?? true ? 'horizontal' : 'vertical');
}
