import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { form } from '@angular/forms/signals';
import { ReviewEditableLine } from '../../../services/review.store';
import { ReviewLine } from './review-line';

describe('ReviewLine', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ReviewLine]
    }).overrideComponent(ReviewLine, {
      set: { template: '' }
    });
  });

  function setup(item: ReviewEditableLine['item'], location: ReviewEditableLine['location'] = null) {
    const fixture = TestBed.createComponent(ReviewLine);
    const line: ReviewEditableLine = {
      rowId: 'row-1',
      sourceLineIndex: 0,
      originalQuantity: 4,
      item,
      location,
      quantity: 4,
      expiryDate: null,
      unitPrice: 0
    };
    const lineForm = TestBed.runInInjectionContext(() => form(signal(line)));

    fixture.componentRef.setInput('line', line);
    fixture.componentRef.setInput('lineForm', lineForm);
    fixture.detectChanges();
    return { fixture, lineForm };
  }

  it('prefills the location from an item default location', () => {
    const { fixture, lineForm } = setup({ id: 'i1', locationId: 'l1' });

    expect(lineForm.location().value()?.id).toBe('l1');
    fixture.destroy();
  });

  it('keeps a location that is already set instead of overwriting it with the item default', () => {
    const { fixture, lineForm } = setup({ id: 'i1', locationId: 'l1' }, { id: 'picked' });

    expect(lineForm.location().value()?.id).toBe('picked');
    fixture.destroy();
  });

  it('leaves the location untouched for an item without a default location', () => {
    const { fixture, lineForm } = setup({ id: 'i1', locationId: null });

    expect(lineForm.location().value()).toBeNull();
    fixture.destroy();
  });
});
