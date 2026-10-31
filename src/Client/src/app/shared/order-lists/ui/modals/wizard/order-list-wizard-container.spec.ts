import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { OrderListWizardStore } from '@ske/shared/order-lists/services';
import { NzBreakpointService } from 'ng-zorro-antd/core/services';
import { of } from 'rxjs';
import { OrderListWizardContainer } from './order-list-wizard-container';

describe('OrderListWizardContainer stepper', () => {
  const fromSupplyList = signal(false);
  const stepIndex = signal(1);

  function create(md: boolean) {
    TestBed.configureTestingModule({
      providers: [
        { provide: NzBreakpointService, useValue: { subscribe: () => of({ md }) } },
        // An unknown step keeps the step bodies out of the render.
        { provide: OrderListWizardStore, useValue: { step: signal('none'), stepIndex, fromSupplyList } }
      ]
    });
    const fixture = TestBed.createComponent(OrderListWizardContainer);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(() => {
    fromSupplyList.set(false);
    stepIndex.set(1);
  });

  it('shows a compact "Pasul N din 4" with a progress bar below md', () => {
    const root = create(false);

    expect(root.textContent).toContain('Pasul 2 din 4 · Articole sugerate');
    expect(root.querySelector('nz-progress')).not.toBeNull();
    expect(root.querySelector('nz-steps')).toBeNull();
  });

  it('names step 2 after the supply list source', () => {
    fromSupplyList.set(true);

    expect(create(false).textContent).toContain('Pasul 2 din 4 · Listă predefinită');
  });

  it('shows the four-step stepper from md up', () => {
    const root = create(true);

    expect(root.querySelectorAll('nz-step')).toHaveLength(4);
    expect(root.textContent).not.toContain('Pasul');
    expect(root.querySelector('nz-progress')).toBeNull();
  });
});
