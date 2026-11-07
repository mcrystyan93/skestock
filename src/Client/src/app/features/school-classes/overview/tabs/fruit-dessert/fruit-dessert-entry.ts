import { Component, computed, effect, input, linkedSignal, output, untracked } from '@angular/core';
import { disabled, form } from '@angular/forms/signals';
import {
  type CongregationDropdownValue,
  FruitDessertMessageType,
  type FruitDessertPlanEntryDto
} from '@ske/models';
import { formatPlanDate } from './format-plan-date';

/** Inputs, outputs and the Signal Form shared by the table row and the small-screen card of one plan day. */
@Component({
  template: ''
})
export abstract class FruitDessertEntry {
  public readonly entry = input.required<FruitDessertPlanEntryDto>();
  public readonly readOnly = input<boolean>(false);
  public readonly sending = input<boolean>(false);
  public readonly downloading = input<boolean>(false);
  /** Set when the popup was blocked, so the user can still open the message. */
  public readonly fallbackUrl = input<string | null>(null);

  public readonly congregationChange = output<string | null>();
  public readonly emailSentChange = output<boolean>();
  public readonly sendMessage = output<FruitDessertMessageType>();
  public readonly downloadInformare = output<void>();

  protected readonly MessageType = FruitDessertMessageType;
  protected readonly date = computed(() => formatPlanDate(this.entry().date));
  protected readonly canMessage = computed(() => !!this.entry().normalizedPhone);
  protected readonly canDownload = computed(() => !!this.entry().congregationId && !this.downloading());
  protected readonly noPhoneTooltip = computed(() =>
    this.canMessage() ? null : 'Congregația nu are un număr de telefon valid'
  );

  // A deleted congregation is shown as text, not as a selection, so it can be replaced but is never re-sent.
  private readonly _selected = computed<CongregationDropdownValue>(() => {
    const { congregationId, congregationName, congregationDeleted } = this.entry();

    return congregationId && !congregationDeleted ? { id: congregationId, name: congregationName ?? undefined } : null;
  });

  private readonly _model = linkedSignal(() => ({
    congregation: this._selected(),
    emailSent: this.entry().emailSent
  }));

  protected readonly rowForm = form(this._model, (path) => {
    disabled(path.congregation, { when: () => this.readOnly() });
    disabled(path.emailSent, { when: () => this.readOnly() });
  });

  private readonly _congregationEffectRef = effect(() => {
    const id = this.rowForm.congregation().value()?.id ?? null;

    untracked(() => {
      if (id !== (this._selected()?.id ?? null)) this.congregationChange.emit(id);
    });
  });

  private readonly _emailSentEffectRef = effect(() => {
    const emailSent = this.rowForm.emailSent().value();

    untracked(() => {
      if (emailSent !== this.entry().emailSent) this.emailSentChange.emit(emailSent);
    });
  });
}
