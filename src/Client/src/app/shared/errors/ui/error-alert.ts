import { Component, input } from '@angular/core';
import { ProblemDetails, ValidationProblemDetails } from '@ske/models';
import { ProblemDetailAlert } from './problem-detail/problem-detail-alert';
import { ValidationProblemDetailAlert } from './validation-problem-detail/validation-problem-detail-alert.component';

@Component({
  imports: [
    ProblemDetailAlert,
    ValidationProblemDetailAlert
  ],
  selector: 'ske-error-alert',
  styles: ``,
  templateUrl: './error-alert.html'
})
export class ErrorAlert {
  public readonly problemDetail = input<ProblemDetails | null>();
  public readonly validationErrors = input<ValidationProblemDetails | null>();
  public readonly title = input<string>();
}
