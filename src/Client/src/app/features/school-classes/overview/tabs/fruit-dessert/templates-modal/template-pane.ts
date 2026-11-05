import { Component, computed, input, linkedSignal, output, untracked } from '@angular/core';
import { form, FormField, maxLength, required, submit, validate } from '@angular/forms/signals';
import { MessagePlaceholderDto, MessageTemplateDto, UpdateMessageTemplateRequest } from '@ske/models';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzFormControlComponent, NzFormDirective, NzFormItemComponent, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { NzInputDirective } from 'ng-zorro-antd/input';
import { TemplateEditor } from './editor/template-editor';

export const MAX_TEMPLATE_NAME_LENGTH = 100;

@Component({
  selector: 'ske-fruit-dessert-template-pane',
  templateUrl: './template-pane.html',
  imports: [
    FormField,
    NzAlertComponent,
    NzButtonComponent,
    NzFormDirective,
    NzFormItemComponent,
    NzFormLabelComponent,
    NzFormControlComponent,
    NzInputDirective,
    TemplateEditor
  ]
})
export class TemplatePane {
  public readonly template = input.required<MessageTemplateDto>();
  public readonly placeholders = input.required<MessagePlaceholderDto[]>();
  public readonly saving = input(false);
  public readonly error = input<string | null>(null);
  public readonly saved = output<UpdateMessageTemplateRequest>();

  // A computed dedupes: the source only changes when the template identity does.
  private readonly _type = computed(() => this.template().type);

  private readonly _model = linkedSignal<MessageTemplateDto['type'], UpdateMessageTemplateRequest>({
    // Keyed on identity so the post-save patch (204, same type) doesn't overwrite edits typed during the PUT.
    source: this._type,
    computation: () => {
      const { name, body } = untracked(this.template);
      return { name, body };
    }
  });

  protected readonly templateForm = form(this._model, (path) => {
    required(path.name, { message: 'Numele șablonului este obligatoriu.' });
    maxLength(path.name, MAX_TEMPLATE_NAME_LENGTH, {
      message: `Numele nu poate depăși ${MAX_TEMPLATE_NAME_LENGTH} de caractere.`
    });
    validate(path.name, ({ value }) =>
      value() && !value().trim() ? { kind: 'required', message: 'Numele șablonului este obligatoriu.' } : undefined
    );
  });

  protected async save(): Promise<void> {
    await submit(this.templateForm, async () => {
      const { name, body } = this.templateForm().value();
      this.saved.emit({ name: name.trim(), body });
    });
  }
}
