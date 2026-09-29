import { Component, computed, inject, input } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ErrorAlert } from '@ske/shared/errors';
import { OrderListWizardStore } from '@ske/shared/order-lists/services';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { gridResponsiveMap, NzBreakpointService } from 'ng-zorro-antd/core/services';
import { NzStepComponent, NzStepsComponent } from 'ng-zorro-antd/steps';
import { OrderListSourceStep } from './steps/source-step';
import { OrderListSuggestionsStep } from './steps/suggestions-step';
import { OrderListSupplyListStep } from './steps/supply-list-step';

/** The wizard's stepper and its two pre-editor steps; the editor itself stays with the modal. */
@Component({
  imports: [
    ErrorAlert,
    NzButtonComponent,
    NzStepsComponent,
    NzStepComponent,
    OrderListSourceStep,
    OrderListSuggestionsStep,
    OrderListSupplyListStep
  ],
  selector: 'ske-order-list-wizard-container',
  template: `
    <nz-steps class="mb-4"
              nzSize="small"
              [nzCurrent]="wizard.stepIndex()"
              [nzDirection]="direction()"
              [nzLabelPlacement]="direction() === 'horizontal' ? 'vertical' : 'horizontal'">
      <nz-step nzTitle="Sursă" />
      <nz-step [nzTitle]="wizard.fromSupplyList() ? 'Listă predefinită' : 'Articole sugerate'" />
      <nz-step nzTitle="Articole comandă" />
    </nz-steps>

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

  protected readonly direction = computed(() => this._breakpoints()?.md ?? true ? 'horizontal' : 'vertical');
}
