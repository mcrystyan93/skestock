import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { FruitDessertMessageType, type FruitDessertPlanEntryDto } from '@ske/models';
import { CongregationsHttp } from '@ske/shared/congregations';
import { CongregationDropdown } from '@ske/shared/congregations';
import { FruitDessertRow } from './fruit-dessert-row';

describe('FruitDessertRow', () => {
  const base: FruitDessertPlanEntryDto = {
    id: 'e1',
    date: '2026-11-10',
    congregationId: 'c1',
    congregationName: 'Central',
    congregationDeleted: false,
    contactPerson: 'Ana Pop',
    contactPhone: '0712 345 678',
    normalizedPhone: '40712345678',
    emailSent: false,
    reminderSent: false,
    thanksSent: false
  };

  function create(entry: Partial<FruitDessertPlanEntryDto> = {}, readOnly = false) {
    TestBed.configureTestingModule({
      providers: [
        { provide: CongregationsHttp, useValue: { getAll: vi.fn(), getById: vi.fn() } },
        provideNzIconsTesting(),
        provideNzIcons([
          { name: 'icons:circle-check', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:paper-plane', icon: '<svg viewBox="0 0 24 24"></svg>' }
        ])
      ]
    });
    const fixture = TestBed.createComponent(FruitDessertRow);
    fixture.componentRef.setInput('entry', { ...base, ...entry });
    fixture.componentRef.setInput('readOnly', readOnly);
    fixture.detectChanges();
    return fixture;
  }

  const text = (el: HTMLElement) => el.textContent?.replace(/\s+/g, ' ') ?? '';
  const buttons = (el: HTMLElement) => Array.from(el.querySelectorAll<HTMLButtonElement>('button'));
  const whatsappButtons = (el: HTMLElement) => buttons(el).filter(b => /aducere aminte|mulțumire/.test(b.getAttribute('aria-label') ?? ''));

  it('renders the date, contact person and phone', () => {
    const el = create().nativeElement as HTMLElement;

    expect(text(el)).toContain('Marți, 10 Noiembrie 2026');
    expect(text(el)).toContain('Ana Pop');
    expect(text(el)).toContain('0712 345 678');
  });

  it('marks a deleted congregation', () => {
    const el = create({ congregationDeleted: true }).nativeElement as HTMLElement;

    expect(text(el)).toContain('Central (ștearsă)');
  });

  it('shows "Trimis" only for the message types already sent', () => {
    expect(text(create().nativeElement)).not.toContain('Trimis');
    TestBed.resetTestingModule();
    expect(text(create({ reminderSent: true }).nativeElement).match(/Trimis/g)).toHaveLength(1);
    TestBed.resetTestingModule();
    expect(text(create({ reminderSent: true, thanksSent: true }).nativeElement).match(/Trimis/g)).toHaveLength(2);
  });

  it('disables the WhatsApp buttons when there is no usable phone', () => {
    const el = create({ normalizedPhone: null }).nativeElement as HTMLElement;

    expect(whatsappButtons(el)).toHaveLength(2);
    expect(whatsappButtons(el).every(b => b.disabled)).toBe(true);
  });

  it('disables every control when read-only', async () => {
    const fixture = create({}, true);
    await fixture.whenStable();
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;

    expect(whatsappButtons(el).every(b => b.disabled)).toBe(true);
    expect(el.querySelector<HTMLInputElement>('input[type="checkbox"]')?.disabled).toBe(true);
  });

  it('emits sendMessage for the clicked button', () => {
    const fixture = create();
    const sent: FruitDessertMessageType[] = [];
    fixture.componentInstance.sendMessage.subscribe(t => sent.push(t));
    const [reminder, thanks] = whatsappButtons(fixture.nativeElement);

    reminder.click();
    thanks.click();

    expect(sent).toEqual([FruitDessertMessageType.Reminder, FruitDessertMessageType.Thanks]);
  });

  it('emits emailSentChange when the checkbox is toggled', async () => {
    const fixture = create();
    const changes: boolean[] = [];
    fixture.componentInstance.emailSentChange.subscribe(v => changes.push(v));

    fixture.nativeElement.querySelector('input[type="checkbox"]').click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(changes).toEqual([true]);
  });

  it('emits congregationChange when another congregation is picked, and null when cleared', async () => {
    const fixture = create();
    const changes: (string | null)[] = [];
    fixture.componentInstance.congregationChange.subscribe(v => changes.push(v));
    const dropdown = fixture.debugElement.query(By.directive(CongregationDropdown)).componentInstance as CongregationDropdown;

    dropdown.value.set({ id: 'c2', name: 'Nord' });
    fixture.detectChanges();
    await fixture.whenStable();
    dropdown.value.set(null);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(changes).toEqual(['c2', null]);
  });

  it('does not emit congregationChange on first render', async () => {
    const fixture = create();
    const changes: (string | null)[] = [];
    fixture.componentInstance.congregationChange.subscribe(v => changes.push(v));

    fixture.detectChanges();
    await fixture.whenStable();

    expect(changes).toEqual([]);
  });

  it('offers a link when the popup was blocked', () => {
    const fixture = create();
    fixture.componentRef.setInput('fallbackUrl', 'https://wa.me/x');
    fixture.detectChanges();

    const link = (fixture.nativeElement as HTMLElement).querySelector('a');
    expect(link?.getAttribute('href')).toBe('https://wa.me/x');
    expect(link?.textContent).toContain('Deschide WhatsApp');
  });
});
