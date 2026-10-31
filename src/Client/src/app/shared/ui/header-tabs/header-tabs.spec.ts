import { TestBed } from '@angular/core/testing';
import { HeaderTabs } from './header-tabs';

describe('HeaderTabs', () => {
  const options = [{ label: 'Comenzi recepționate', compactLabel: 'Recepții' }, { label: 'Stoc' }];

  function create(compact: boolean) {
    const fixture = TestBed.createComponent(HeaderTabs);
    fixture.componentRef.setInput('options', options);
    fixture.componentRef.setInput('compact', compact);
    fixture.detectChanges();
    return fixture;
  }

  it('renders tabs on large screens', () => {
    const el: HTMLElement = create(false).nativeElement;
    expect(el.querySelector('nz-tabs')).not.toBeNull();
    expect(el.querySelector('nz-segmented')).toBeNull();
    expect(el.textContent).toContain('Comenzi recepționate');
  });

  it('renders segments with compact labels on small screens', () => {
    const el: HTMLElement = create(true).nativeElement;
    expect(el.querySelector('nz-segmented')).not.toBeNull();
    expect(el.querySelector('nz-tabs')).toBeNull();
    expect(el.textContent).toContain('Recepții');
  });

  it('lets the compact segments scroll horizontally instead of truncating', () => {
    const el: HTMLElement = create(true).nativeElement;
    expect(el.querySelector('nz-segmented')?.parentElement?.classList).toContain('overflow-x-auto');
    expect(el.querySelector('nz-segmented')?.hasAttribute('nzblock')).toBe(false);
  });

  it('scrolls the selected segment into view', async () => {
    const scrollIntoView = vi.fn();
    Element.prototype.scrollIntoView = scrollIntoView;
    const fixture = create(true);
    fixture.componentRef.setInput('selectedIndex', 1);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(scrollIntoView).toHaveBeenCalledWith({ block: 'nearest', inline: 'nearest' });
  });
});
