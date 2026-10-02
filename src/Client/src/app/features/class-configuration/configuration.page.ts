import {Component, DestroyRef, computed, inject, signal} from '@angular/core';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {form, FormField, max, min, required, submit, validate} from '@angular/forms/signals';
import {HttpClient, HttpErrorResponse} from '@angular/common/http';
import {firstValueFrom} from 'rxjs';
import {NzButtonComponent} from 'ng-zorro-antd/button';
import {NzFormControlComponent, NzFormDirective, NzFormItemComponent, NzFormLabelComponent} from 'ng-zorro-antd/form';
import {NzIconDirective} from 'ng-zorro-antd/icon';
import {NzInputNumberComponent} from 'ng-zorro-antd/input-number';
import {DepartmentTemplateDto, SharedClassConfigurationDto} from '@ske/models';
import {MenuToggle} from '@ske/layouts/menu-toggle';
import {ThemeSwitcher} from '@ske/shared/theme';

type DepartmentDraft = Pick<DepartmentTemplateDto, 'name' | 'responsibilities'> & { id: string | null };

@Component({
  imports: [
    FormField,
    NzButtonComponent,
    NzFormControlComponent,
    NzFormDirective,
    NzFormItemComponent,
    NzFormLabelComponent,
    NzIconDirective,
    NzInputNumberComponent,
    MenuToggle,
    ThemeSwitcher
  ],
  selector: 'ske-class-configuration-page',
  templateUrl: './configuration.page.html',
  host: {class: 'block min-w-0 grow overflow-y-auto px-3 py-4 sm:px-6 lg:px-8'}
})
export class ConfigurationPage {
  private readonly _http = inject(HttpClient);
  private readonly _destroyRef = inject(DestroyRef);

  protected readonly state = signal<SharedClassConfigurationDto | null>(null);
  protected readonly model = signal({invitationCount: 0});
  protected readonly departments = signal<DepartmentDraft[]>([]);
  protected readonly hasAttemptedSave = signal(false);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly savedMessage = signal<string | null>(null);
  protected readonly hasInvalidDepartments = computed(() => {
    const departments = this.departments();
    const names = departments.map(department => department.name.trim().toLowerCase());

    return departments.some(department =>
      !department.name.trim() ||
      department.name.length > 100 ||
      !department.responsibilities.trim()
    ) || new Set(names).size !== names.length;
  });

  protected readonly configurationForm = form(this.model, (path) => {
    required(path.invitationCount, {message: 'Numărul de invitații este obligatoriu.'});
    validate(path.invitationCount, (context) =>
      Number.isInteger(context.value()) ? undefined : {kind: 'integer', message: 'Introduceți un număr întreg.'}
    );
    min(path.invitationCount, 0, {message: 'Numărul de invitații nu poate fi negativ.'});
    max(path.invitationCount, 2147483647, {message: 'Numărul depășește limita acceptată.'});
  });

  constructor() {
    this._http.get<SharedClassConfigurationDto>('/api/ClassConfiguration')
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe({
        next: (state) => {
          this.state.set(state);
          this.model.set({invitationCount: state.invitationCount});
          this.departments.set(state.departments.map(department => ({...department})));
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.errorMessage.set(this.readError(error, 'Nu am putut încărca configurarea.'));
          this.loading.set(false);
        }
      });
  }

  protected async save(): Promise<void> {
    this.errorMessage.set(null);
    this.savedMessage.set(null);
    this.hasAttemptedSave.set(true);

    if (this.hasInvalidDepartments()) {
      this.errorMessage.set('Verificați numele și responsabilitățile departamentelor și eliminați numele duplicate.');
      return;
    }

    await submit(this.configurationForm, async () => {
      this.saving.set(true);
      try {
        const saved = await firstValueFrom(this._http.put<SharedClassConfigurationDto>(
          '/api/ClassConfiguration',
          {...this.model(), departments: this.departments()}
        ));
        this.state.set(saved);
        this.departments.set(saved.departments.map(department => ({...department})));
        this.savedMessage.set('Configurarea a fost salvată.');
      } catch (error: unknown) {
        this.errorMessage.set(this.readError(error, 'Configurarea nu a putut fi salvată. Încercați din nou.'));
      } finally {
        this.saving.set(false);
      }
    });
  }

  protected addDepartment(): void {
    this.departments.update(departments => [...departments, {id: null, name: '', responsibilities: ''}]);
    this.savedMessage.set(null);
  }

  protected removeDepartment(index: number): void {
    this.departments.update(departments => departments.filter((_, itemIndex) => itemIndex !== index));
    this.savedMessage.set(null);
  }

  protected updateDepartmentName(index: number, name: string): void {
    this.updateDepartment(index, department => ({...department, name}));
  }

  protected updateDepartmentResponsibilities(index: number, responsibilities: string): void {
    this.updateDepartment(index, department => ({...department, responsibilities}));
  }

  protected duplicateName(index: number): boolean {
    const current = this.departments()[index]?.name.trim().toLowerCase();

    return !!current && this.departments().some((department, itemIndex) =>
      itemIndex !== index && department.name.trim().toLowerCase() === current
    );
  }

  protected nameError(index: number): string | null {
    if (!this.hasAttemptedSave()) return null;
    const department = this.departments()[index];
    if (!department?.name.trim()) return 'Numele departamentului este obligatoriu.';
    if (department.name.length > 100) return 'Numele nu poate depăși 100 de caractere.';
    if (this.duplicateName(index)) return 'Numele departamentului trebuie să fie unic.';
    return null;
  }

  private updateDepartment(index: number, update: (department: DepartmentDraft) => DepartmentDraft): void {
    this.departments.update(departments => departments.map((department, itemIndex) =>
      itemIndex === index ? update(department) : department
    ));
    this.savedMessage.set(null);
  }

  private readError(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse && typeof error.error?.title === 'string') {
      return error.error.title;
    }

    return fallback;
  }
}
