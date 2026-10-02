import {Component, input, output, signal} from '@angular/core';
import {SchoolClassDto} from '@ske/models';
import {SchoolClassesHttp} from '@ske/shared/school-classes';
import {inject} from '@angular/core';

@Component({
  selector: 'ske-school-class-departments-tab',
  templateUrl: './class-departments-tab.html',
  host: {class: 'block min-w-0 p-4 md:p-6'}
})
export class ClassDepartmentsTab {
  public readonly schoolClass = input.required<Partial<SchoolClassDto>>();
  public readonly refreshed = output<void>();
  protected readonly busyId = signal<string | null>(null);
  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly drafts = signal<Record<string, string>>({});
  private readonly _schoolClassesHttp = inject(SchoolClassesHttp);

  protected draft(departmentId: string, savedValue: string | null | undefined): string {
    return this.drafts()[departmentId] ?? savedValue ?? '';
  }

  protected setDraft(departmentId: string, value: string): void {
    this.drafts.update(drafts => ({...drafts, [departmentId]: value}));
    this.message.set(null);
    this.error.set(null);
  }

  protected saveResponsiblePerson(departmentId: string, savedValue: string | null | undefined): void {
    const classId = this.schoolClass().id;
    if (!classId) return;
    const value = this.draft(departmentId, savedValue).trim();
    this.busyId.set(departmentId);
    this.error.set(null);
    this.message.set(null);
    this._schoolClassesHttp.updateDepartmentResponsiblePerson(classId, departmentId, value).subscribe({
      next: () => {
        this.busyId.set(null);
        this.drafts.update(drafts => ({...drafts, [departmentId]: value}));
        this.message.set('Responsabilul a fost salvat.');
        this.refreshed.emit();
      },
      error: () => {
        this.busyId.set(null);
        this.error.set('Nu am putut salva responsabilul. Încercați din nou.');
      }
    });
  }

  protected initialize(): void {
    const classId = this.schoolClass().id;
    if (!classId) return;
    this.busyId.set('initialize');
    this.error.set(null);
    this._schoolClassesHttp.initializeConfiguration(classId).subscribe({
      next: () => {
        this.busyId.set(null);
        this.message.set('Departamentele și numărul de invitații au fost copiate în clasă.');
        this.refreshed.emit();
      },
      error: () => {
        this.busyId.set(null);
        this.error.set('Configurarea nu a putut fi copiată. Reîncărcați pagina și încercați din nou.');
      }
    });
  }
}
