import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ClassStatus } from '@ske/models';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { Header } from './header';

describe('Header stock count button', () => {
  function create(selectedTabIndex: number, status: ClassStatus) {
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideNzIconsTesting(),
        provideNzIcons(
          ['xmark', 'lock', 'plus', 'clipboard-list-check', 'bars', 'sun-bright', 'moon'].map((name) => ({
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

  const hasButton = (el: HTMLElement) =>
    Array.from(el.querySelectorAll('button')).some(
      (b) => b.getAttribute('aria-label') === 'Inventar stoc' || b.textContent?.includes('Inventar stoc')
    );

  it('is shown on the Stoc tab of an open class', () => {
    expect(hasButton(create(0, ClassStatus.Active).nativeElement)).toBe(true);
  });

  it('is hidden on other tabs', () => {
    expect(hasButton(create(1, ClassStatus.Active).nativeElement)).toBe(false);
  });

  it('is hidden for a closed class', () => {
    expect(hasButton(create(0, ClassStatus.Closed).nativeElement)).toBe(false);
  });
});
