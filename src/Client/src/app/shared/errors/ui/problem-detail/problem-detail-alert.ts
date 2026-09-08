import { Component, computed, inject, input } from '@angular/core';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { ProblemDetails } from '@ske/models';
import { ErrorMessageService } from '../../services/error-message.service';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';

@Component({
  imports: [
    NzAlertComponent,
    NzTypographyComponent
  ],
  selector: 'ske-problem-detail-alert',
  styles: ``,
  templateUrl: './problem-detail-alert.html'
})
export class ProblemDetailAlert {
  private readonly errorMessages = inject(ErrorMessageService);

  public readonly problemDetail = input<ProblemDetails | null>();
  public readonly title = input<string>();

  public readonly titleToDisplay = computed(() => this.title() ?? this.problemDetail()?.title ?? 'Eroare');

  /** Human-readable Romanian phrase resolved from the backend error code. */
  public readonly messageToDisplay = computed(() =>
    this.errorMessages.resolveProblem(this.problemDetail() ?? null));

  /** Raw backend detail, surfaced alongside the resolved phrase (even when a code is present). */
  public readonly detailToDisplay = computed(() => this.problemDetail()?.detail);

  public readonly correlationId = computed(() => this.problemDetail()?.error?.diagnostics?.correlationId);
}
