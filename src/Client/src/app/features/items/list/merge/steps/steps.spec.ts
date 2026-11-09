import { TestBed } from '@angular/core/testing';
import { ItemDto } from '@ske/models';
import { ItemMergeConfirmStep } from './confirm-step';
import { ItemMergeImpactStep } from './impact-step';
import { ItemMergePrimaryStep } from './primary-step';

describe('item merge steps', () => {
  const item = (id: string, name: string, isActive = true) => ({ id, name, unit: 'buc', isActive }) as ItemDto;
  const text = (fixture: { nativeElement: unknown }) => (fixture.nativeElement as HTMLElement).textContent ?? '';

  describe('ItemMergePrimaryStep', () => {
    it('disables inactive items and says why', () => {
      const fixture = TestBed.createComponent(ItemMergePrimaryStep);
      fixture.componentRef.setInput('items', [item('a', 'Creion'), item('b', 'Vechi', false)]);
      fixture.detectChanges();

      expect(text(fixture)).toContain('inactiv (nu poate fi principal)');
      expect((fixture.nativeElement as HTMLElement).querySelectorAll('input[type="radio"]:disabled')).toHaveLength(1);
    });
  });

  describe('ItemMergeImpactStep', () => {
    const create = (inputs: Record<string, unknown>) => {
      const fixture = TestBed.createComponent(ItemMergeImpactStep);
      for (const [key, value] of Object.entries(inputs))
        fixture.componentRef.setInput(key, value);
      fixture.detectChanges();
      return fixture;
    };

    it('says nothing is moved when there are no impacts', () => {
      expect(text(create({}))).toContain('Nu există date de mutat');
    });

    it('shows the error and emits retry', () => {
      const fixture = create({ errorMessage: 'A apărut o eroare neașteptată.' });
      const retries: unknown[] = [];
      fixture.componentInstance.retry.subscribe(() => retries.push(true));

      (fixture.nativeElement as HTMLElement).querySelector('button')?.click();

      expect(text(fixture)).toContain('Previzualizarea nu a putut fi încărcată');
      expect(retries).toHaveLength(1);
    });

    it('shows the relinked count per kind', () => {
      const fixture = create({ impacts: [{ kind: 'supplyListLines', relinked: 3, combined: 0 }] });

      expect(text(fixture)).toContain('3 mutate');
    });
  });

  describe('ItemMergeConfirmStep', () => {
    it('names the kept and the disabled items', () => {
      const fixture = TestBed.createComponent(ItemMergeConfirmStep);
      fixture.componentRef.setInput('primary', item('a', 'Creion'));
      fixture.componentRef.setInput('secondaries', [item('b', 'Creion HB')]);
      fixture.detectChanges();

      expect(text(fixture)).toContain('Se păstrează: Creion');
      expect(text(fixture)).toContain('Creion HB');
    });
  });
});
