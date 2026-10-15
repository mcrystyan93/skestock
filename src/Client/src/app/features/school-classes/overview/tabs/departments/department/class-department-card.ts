import { Component, computed, input, linkedSignal, output } from '@angular/core';
import { disabled as disabledField, form, FormField, submit } from '@angular/forms/signals';
import { ClassDepartmentDto } from '@ske/models';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzFormControlComponent, NzFormItemComponent, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { NzInputDirective } from 'ng-zorro-antd/input';

@Component({
  selector: 'ske-class-department-card',
  imports: [FormField, NzButtonComponent, NzCardComponent, NzFormControlComponent, NzFormItemComponent, NzFormLabelComponent, NzInputDirective],
  templateUrl: './class-department-card.html',
  host: { class: 'block min-w-0' }
})
export class ClassDepartmentCard {
  public readonly department = input.required<ClassDepartmentDto>();
  public readonly saving = input(false);
  public readonly disabled = input(false);
  public readonly save = output<string>();
  public readonly edited = output<void>();

  private readonly _savedResponsiblePerson = computed(() => this.department().responsiblePerson ?? '');
  private readonly _formModel = linkedSignal(() => ({
    responsiblePerson: this._savedResponsiblePerson()
  }));
  protected readonly responsiblePersonForm = form(this._formModel, path => {
    disabledField(path.responsiblePerson, { when: (valueOf) => this.disabled() });
  });

  protected async saveResponsiblePerson(): Promise<void> {
    if (this.disabled()) return;

    await submit(this.responsiblePersonForm, async () => {
      const responsiblePerson = this._formModel().responsiblePerson.trim();
      this._formModel.set({ responsiblePerson });
      this.save.emit(responsiblePerson);
    });
  }
}
