import { Component, input } from '@angular/core';
import { ExpenseList } from '@ske/shared/expenses';

@Component({
  imports: [
    ExpenseList
  ],
  selector: 'ske-school-class-overview-expenses-tab',
  template: `
    <ske-expense-list [classId]="classId()" />
  `
})
export class ExpensesTab {
  public readonly classId = input.required<string | null>();
}
