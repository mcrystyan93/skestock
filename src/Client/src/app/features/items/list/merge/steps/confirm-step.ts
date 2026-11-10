import { Component, computed, input } from '@angular/core';
import { ItemDto, ProblemDetails, ValidationProblemDetails } from '@ske/models';
import { ErrorAlert } from '@ske/shared/errors';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
@Component({
  imports: [ErrorAlert, NzAlertComponent],
  selector: 'ske-item-merge-confirm-step',
  template: `
    <nz-alert nzType="warning"
              nzShowIcon
              class="mb-4"
              nzMessage="Acțiune ireversibilă"
              nzDescription="Articolele secundare vor fi dezactivate, iar datele lor vor fi mutate definitiv pe articolul principal, în toate clasele." />
    @if (units().length > 1) {
      <nz-alert nzType="warning"
                nzShowIcon
                class="mb-4"
                nzMessage="Unități de măsură diferite"
                [nzDescription]="'Articolele au unități diferite (' + units().join(', ') + '). Cantitățile nu se convertesc: stocul mutat se va aduna ca și cum ar fi în unitatea articolului principal.'" />
    }
    <p class="m-0! mb-3!">
      Se păstrează: <strong>{{ primary()?.name }}</strong>
    </p>
    <p class="m-0!">Se dezactivează:</p>
    <ul class="m-0! mb-3! list-disc pl-6">
      @for (item of secondaries(); track item.id) {
        <li>{{ item.name }}</li>
      }
    </ul>
    <ske-error-display [problemDetail]="problemDetail()"
                       [validationErrors]="validationErrors()" />
  `
})
export class ItemMergeConfirmStep {
  public readonly primary = input<ItemDto | null>(null);
  public readonly secondaries = input<ItemDto[]>([]);
  public readonly problemDetail = input<ProblemDetails | null>(null);
  public readonly validationErrors = input<ValidationProblemDetails | null>(null);
  protected readonly units = computed(() =>
    [...new Set([this.primary(), ...this.secondaries()].flatMap((item) => (item ? [item.unit] : [])))]
  );
}
