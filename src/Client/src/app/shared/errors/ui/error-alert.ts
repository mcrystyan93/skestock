import { Component, input } from '@angular/core';
import { ProblemDetails, ValidationProblemDetails } from '@ske/models';
import { ProblemDetailAlert } from './problem-detail/problem-detail-alert';
import { ValidationProblemDetailAlert } from './validation-problem-detail/validation-problem-detail-alert.component';
import { ProblemDetailText } from './problem-detail/problem-detail-text';

@Component({
  imports: [
    ProblemDetailAlert,
    ValidationProblemDetailAlert,
    ProblemDetailText
  ],
  selector: 'ske-error-display',
  styles: ``,
  templateUrl: './error-alert.html'
})
export class ErrorAlert {
  public readonly problemDetail = input<ProblemDetails | null>();
  public readonly validationErrors = input<ValidationProblemDetails | null>();
  public readonly title = input<string>();
  public readonly type = input<AlertDisplayType>('alert');
}
type AlertDisplayType = 'alert' | 'text';
