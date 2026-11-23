import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { ExpenseDto, FileMetadataDto } from '@ske/models';
import { ExpensesHttp } from './expenses.http';
import { ExpenseUploadStore } from './expense-upload.store';

describe('ExpenseUploadStore', () => {
  const serverError = () => new HttpErrorResponse({ status: 500, error: { title: 'boom', status: 500 } });
  const files = [{ id: 'f1' }, { id: 'f2' }, { id: 'f3' }] as FileMetadataDto[];
  const expenseFor = (id: string) => ({ id }) as ExpenseDto;

  function createStore(createFromDocument: ExpensesHttp['createFromDocument']) {
    TestBed.configureTestingModule({
      providers: [ExpenseUploadStore, { provide: ExpensesHttp, useValue: { createFromDocument } }]
    });
    return TestBed.inject(ExpenseUploadStore);
  }

  it('keeps the expenses created before a later file fails', () => {
    const store = createStore(({ fileMetadataId }) =>
      fileMetadataId === 'f2' ? throwError(() => serverError()) : of(expenseFor(fileMetadataId)));

    store.createFromDocuments({ files, classId: 'c1' });

    expect(store.expenses().map((e) => e.id)).toEqual(['f1', 'f3']);
    expect(store.failedCount()).toBe(1);
  });

  it('reports the error when no file could be created', () => {
    const store = createStore(() => throwError(() => serverError()));

    store.createFromDocuments({ files, classId: 'c1' });

    expect(store.expenses()).toEqual([]);
    expect(store.expenseUploadProblemDetail()).not.toBeNull();
  });
});
