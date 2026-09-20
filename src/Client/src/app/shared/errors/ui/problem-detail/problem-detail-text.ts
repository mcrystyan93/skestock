import { Component, computed, inject, input } from '@angular/core';
import { ProblemDetails } from '@ske/models';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { ErrorMessageService } from '@ske/shared/errors';

@Component({
  imports: [
    NzTypographyComponent,
    NzIconDirective
  ],
  selector: 'ske-problem-detail-text',
  styles: ``,
  template: `
    <div nz-typography
         nzType="danger"
         class="flex gap-2 mb-0!">
      <nz-icon nzType="icons:circle-info"></nz-icon>
      {{ messageToDisplay() }}
    </div>
  `
})
export class ProblemDetailText {
  public readonly problemDetail = input<ProblemDetails | null>();

  private readonly errorMessages = inject(ErrorMessageService);

  /** Human-readable Romanian phrase resolved from the backend error code. */
  public readonly messageToDisplay = computed(() =>
    this.errorMessages.resolveProblem(this.problemDetail() ?? null));
}
