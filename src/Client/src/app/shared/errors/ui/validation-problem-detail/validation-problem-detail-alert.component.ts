import { Component, computed, inject, input } from '@angular/core';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { ValidationProblemDetails } from '@ske/models';
import { ErrorMessageService } from '../../services/error-message.service';

interface ResolvedValidationError {
  field: string | null;
  message: string;
}

@Component({
  imports: [
    NzAlertComponent
  ],
  selector: 'ske-validation-problem-detail-alert',
  styles: ``,
  templateUrl: './validation-problem-detail-alert.component.html'
})
export class ValidationProblemDetailAlert {
  private readonly errorMessages = inject(ErrorMessageService);

  public readonly validationErrors = input<ValidationProblemDetails | null>();
  public readonly title = input<string>();

  public readonly titleToDisplay = computed(() =>
    this.title() ?? this.validationErrors()?.title ?? 'Datele trimise nu sunt valide');

  /** Per-field validation errors resolved to Romanian phrases. */
  public readonly resolvedErrors = computed<ResolvedValidationError[]>(() => {
    const items = this.validationErrors()?.error?.errors ?? [];
    return items.map((item) => ({
      field: item.field ?? null,
      message: this.errorMessages.resolveItem(item),
    }));
  });

  public readonly correlationId = computed(() => this.validationErrors()?.error?.diagnostics?.correlationId);
}
