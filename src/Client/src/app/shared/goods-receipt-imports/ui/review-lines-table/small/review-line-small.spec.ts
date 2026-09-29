import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { form } from '@angular/forms/signals';
import { ReviewEditableLine } from '../../../services/review.store';
import { ReviewLineSmall } from './review-line-small';

describe('ReviewLineSmall', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ReviewLineSmall]
    }).overrideComponent(ReviewLineSmall, {
      set: { template: '' }
    });
  });

  it('emits split and remove actions for the edited line', () => {
    const fixture = TestBed.createComponent(ReviewLineSmall);
    const component = fixture.componentInstance;
    const line: ReviewEditableLine = {
      rowId: 'row-1',
      sourceLineIndex: 0,
      originalQuantity: 4,
      item: null,
      location: null,
      quantity: 4,
      expiryDate: null,
      unitPrice: 0
    };
    const lineForm = TestBed.runInInjectionContext(() => form(signal(line)));
    const split = vi.spyOn(component.splitLine, 'emit');
    const remove = vi.spyOn(component.removeLine, 'emit');

    fixture.componentRef.setInput('line', line);
    fixture.componentRef.setInput('lineForm', lineForm);
    fixture.componentRef.setInput('canRemove', true);
    fixture.detectChanges();

    component.split();
    component.remove();

    expect(split).toHaveBeenCalledWith(line);
    expect(remove).toHaveBeenCalledWith(line);
    fixture.destroy();
  });
});
