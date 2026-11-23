import { TestBed } from '@angular/core/testing';
import { provideNzDateFnsAdapter } from 'ng-zorro-antd/core/time';
import { provideNzI18n, ro_RO } from 'ng-zorro-antd/i18n';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { ExpenseDto } from '@ske/models';
import { ExpenseForm } from './expense-form';

describe('ExpenseForm', () => {
  function createFixture(expense: Partial<ExpenseDto> | null) {
    const fixture = TestBed.createComponent(ExpenseForm);
    fixture.componentRef.setInput('expense', expense);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => TestBed.configureTestingModule({
    imports: [ExpenseForm],
    providers: [provideNzIconsTesting(), provideNzI18n(ro_RO), provideNzDateFnsAdapter()]
  }));

  it('round-trips the expense date without shifting the day', async () => {
    const fixture = createFixture({ expenseDate: '2026-10-05', currency: 'ron', expenseCategoryId: 'cat1' });

    const { isValid, fields } = await fixture.componentInstance.submit();

    expect(isValid).toBe(true);
    expect(fields).toMatchObject({ expenseDate: '2026-10-05', currency: 'RON', expenseCategoryId: 'cat1' });
  });

  it('rejects a VAT amount above the total', async () => {
    const fixture = createFixture({ currency: 'RON', totalAmount: 10, vatAmount: 20 });

    const { isValid, fields } = await fixture.componentInstance.submit();

    expect(isValid).toBe(false);
    expect(fields).toBeNull();
  });
});
