import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ClassStatus } from '@ske/models';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { NzModalService } from 'ng-zorro-antd/modal';
import { TemplatesModal } from '../../tabs/fruit-dessert/templates-modal/templates-modal';
import { Header } from './header';

describe('Header stock count button', () => {
  function create(selectedTabIndex: number, status: ClassStatus) {
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideNzIconsTesting(),
        { provide: NzModalService, useValue: {} }, // provided by HeaderContainer in the app
        provideNzIcons(
          ['xmark', 'lock', 'plus', 'clipboard-list-check', 'ellipsis-vertical', 'bars', 'sun-bright', 'moon'].map((name) => ({
            name: `icons:${name}`,
            icon: '<svg viewBox="0 0 24 24"></svg>'
          }))
        )
      ] });
    const fixture = TestBed.createComponent(Header);
    fixture.componentRef.setInput('schoolClass', { id: 'c-1', name: 'Clasa', status });
    fixture.componentRef.setInput('summary', {});
    fixture.componentRef.setInput('selectedTabIndex', selectedTabIndex);
    fixture.detectChanges();
    return fixture;
  }

  // The action sits in the "more" dropdown, whose menu renders in an overlay outside the component.
  async function hasStockCountItem(fixture: ComponentFixture<Header>) {
    const trigger = fixture.nativeElement.querySelector('button[aria-label="Mai multe acțiuni"]') as HTMLElement | null;
    if (!trigger) return false;
    trigger.click();
    fixture.detectChanges();
    await fixture.whenStable();
    await new Promise((r) => setTimeout(r, 200)); // ng-zorro opens the dropdown after a 150ms audit
    return Array.from(document.querySelectorAll('li[nz-menu-item]')).some((li) => li.textContent?.includes('Inventar stoc'));
  }

  it('is shown in the more menu on the Stoc tab of an open class', async () => {
    expect(await hasStockCountItem(create(0, ClassStatus.Active))).toBe(true);
  });

  it('is hidden on other tabs', async () => {
    expect(await hasStockCountItem(create(1, ClassStatus.Active))).toBe(false);
  });

  it('is hidden for a closed class', async () => {
    expect(await hasStockCountItem(create(0, ClassStatus.Closed))).toBe(false);
  });
});

describe('Header message templates button', () => {
  const modalService = { create: vi.fn() };

  function create(selectedTabIndex: number, status: ClassStatus) {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideNzIconsTesting(),
        provideNzIcons(
          ['xmark', 'comment-dots', 'plus', 'file-import', 'lock', 'clipboard-list-check', 'ellipsis-vertical', 'bars', 'sun-bright', 'moon'].map((name) => ({
            name: `icons:${name}`,
            icon: '<svg viewBox="0 0 24 24"></svg>'
          }))
        ),
        { provide: NzModalService, useValue: modalService }
      ]
    });
    const fixture = TestBed.createComponent(Header);
    fixture.componentRef.setInput('schoolClass', { id: 'c-1', name: 'Clasa', status });
    fixture.componentRef.setInput('summary', {});
    fixture.componentRef.setInput('selectedTabIndex', selectedTabIndex);
    fixture.detectChanges();
    return fixture;
  }

  const button = (fixture: ComponentFixture<Header>) =>
    fixture.nativeElement.querySelector('button[aria-label="Șabloane mesaje"]') as HTMLButtonElement | null;

  beforeEach(() => modalService.create.mockClear());

  it('opens the templates modal from the Fructe și desert tab', () => {
    const fixture = create(7, ClassStatus.Active);

    button(fixture)!.click();

    expect(modalService.create).toHaveBeenCalledOnce();
    expect(modalService.create.mock.calls[0][0].nzContent).toBe(TemplatesModal);
  });

  it('is hidden on other tabs and for a closed class', () => {
    expect(button(create(0, ClassStatus.Active))).toBeNull();
    TestBed.resetTestingModule();
    expect(button(create(7, ClassStatus.Closed))).toBeNull();
  });
});
