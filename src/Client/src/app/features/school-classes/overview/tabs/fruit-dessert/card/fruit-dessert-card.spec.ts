import { TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { FruitDessertMessageType, type FruitDessertPlanEntryDto } from '@ske/models';
import { CongregationsHttp } from '@ske/shared/congregations';
import { FruitDessertCard } from './fruit-dessert-card';

describe('FruitDessertCard', () => {
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

  function create(entry: Partial<FruitDessertPlanEntryDto> = {}, readOnly = false, downloading = false) {
    TestBed.configureTestingModule({
      providers: [
        { provide: CongregationsHttp, useValue: { getAll: vi.fn(), getById: vi.fn() } },
        provideNzIconsTesting(),
        provideNzIcons([
          { name: 'icons:circle-check', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:paper-plane', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:file-pdf', icon: '<svg viewBox="0 0 24 24"></svg>' }
        ])
      ]
    });
    const fixture = TestBed.createComponent(FruitDessertCard);
    fixture.componentRef.setInput('entry', { ...base, ...entry });
    fixture.componentRef.setInput('readOnly', readOnly);
    fixture.componentRef.setInput('downloading', downloading);
    fixture.detectChanges();
    return fixture;
  }

  const text = (el: HTMLElement) => el.textContent?.replace(/\s+/g, ' ') ?? '';
  const sendButtons = (el: HTMLElement) =>
    Array.from(el.querySelectorAll<HTMLButtonElement>('button')).filter(b =>
      /aducere aminte|mulțumire/.test(b.getAttribute('aria-label') ?? '')
    );

  const pdfButton = (el: HTMLElement) =>
    Array.from(el.querySelectorAll<HTMLButtonElement>('button')).find(b =>
      /informarea PDF/.test(b.getAttribute('aria-label') ?? '')
    )!;

  it('offers the Informare PDF, enabled even when read-only', () => {
    const el = create({}, true).nativeElement as HTMLElement;

    expect(pdfButton(el).textContent).toContain('Descarcă PDF');
    expect(pdfButton(el).disabled).toBe(false);
  });

  it('disables the PDF button without a congregation or while downloading', () => {
    expect(pdfButton(create({ congregationId: null }).nativeElement).disabled).toBe(true);
    TestBed.resetTestingModule();
    expect(pdfButton(create({}, false, true).nativeElement).disabled).toBe(true);
  });

  it('emits downloadInformare when the PDF button is clicked', () => {
    const fixture = create();
    let emitted = 0;
    fixture.componentInstance.downloadInformare.subscribe(() => emitted++);

    pdfButton(fixture.nativeElement).click();

    expect(emitted).toBe(1);
  });

  it('renders the date, contact and a tappable phone link', () => {
    const el = create().nativeElement as HTMLElement;

    expect(text(el)).toContain('Marți, 10 Noiembrie 2026');
    expect(text(el)).toContain('Ana Pop');
    expect(el.querySelector<HTMLAnchorElement>('a[href="tel:+40712345678"]')).not.toBeNull();
  });

  it('replaces a send button with a sent tag', () => {
    const el = create({ reminderSent: true }).nativeElement as HTMLElement;

    expect(text(el)).toContain('Aducere aminte trimisă');
    expect(sendButtons(el)).toHaveLength(1);
  });

  it('disables the send buttons without a usable phone and when read-only', () => {
    expect(sendButtons(create({ normalizedPhone: null }).nativeElement).every(b => b.disabled)).toBe(true);
    TestBed.resetTestingModule();
    expect(sendButtons(create({}, true).nativeElement).every(b => b.disabled)).toBe(true);
  });

  it('emits sendMessage for the clicked button', () => {
    const fixture = create();
    const sent: FruitDessertMessageType[] = [];
    fixture.componentInstance.sendMessage.subscribe(t => sent.push(t));
    const [reminder, thanks] = sendButtons(fixture.nativeElement);

    reminder.click();
    thanks.click();

    expect(sent).toEqual([FruitDessertMessageType.Reminder, FruitDessertMessageType.Thanks]);
  });

  it('offers a link when the popup was blocked', () => {
    const fixture = create();
    fixture.componentRef.setInput('fallbackUrl', 'https://wa.me/40712345678?text=x');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('a[href^="https://wa.me/"]')).not.toBeNull();
  });
});
