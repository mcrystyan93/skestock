import { TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { MenuToggle } from './menu-toggle';
import { NavigationDrawerState } from '../navigation-drawer.state';

describe('MenuToggle', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [MenuToggle],
      providers: [
        provideNzIconsTesting(),
        provideNzIcons([{ name: 'icons:bars', icon: '<svg viewBox="0 0 24 24"></svg>' }])
      ]
    });
  });

  it('registers while alive', () => {
    const state = TestBed.inject(NavigationDrawerState);
    const fixture = TestBed.createComponent(MenuToggle);
    expect(state.hasToggle()).toBe(true);
    fixture.destroy();
    expect(state.hasToggle()).toBe(false);
  });

  it('toggles the drawer and reflects aria-expanded', async () => {
    const state = TestBed.inject(NavigationDrawerState);
    const fixture = TestBed.createComponent(MenuToggle);
    await fixture.whenStable();
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');

    expect(button.getAttribute('aria-label')).toBe('Deschide meniul');
    expect(button.getAttribute('aria-expanded')).toBe('false');

    button.click();
    await fixture.whenStable();

    expect(state.open()).toBe(true);
    expect(button.getAttribute('aria-expanded')).toBe('true');
  });
});
