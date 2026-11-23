import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  CreateExpenseFromDocumentRequest,
  CreateExpenseRequest,
  ExpenseDto,
  ExpenseFieldsRequest,
  GetAllExpensesRequest,
  PaginatedResponse
} from '@ske/models';

/**
 * HTTP client for src/Web/Endpoints/Expenses.cs, mapped under /api/Expenses.
 */
@Service()
export class ExpensesHttp {
  private readonly _httpClient = inject(HttpClient);

  public getAll(request: GetAllExpensesRequest | Partial<GetAllExpensesRequest>) {
    return this._httpClient.post<PaginatedResponse<ExpenseDto>>('/api/Expenses/get-all', request);
  }

  public getById(id: string) {
    return this._httpClient.get<ExpenseDto>(`/api/Expenses/${id}`);
  }

  public createFromDocument(request: CreateExpenseFromDocumentRequest) {
    return this._httpClient.post<ExpenseDto>('/api/Expenses/from-document', request);
  }

  public create(request: CreateExpenseRequest) {
    return this._httpClient.post<ExpenseDto>('/api/Expenses', request);
  }

  public update(id: string, request: ExpenseFieldsRequest) {
    return this._httpClient.put<ExpenseDto>(`/api/Expenses/${id}`, request);
  }

  public retry(id: string) {
    return this._httpClient.post<void>(`/api/Expenses/${id}/retry`, {});
  }

  public delete(id: string) {
    return this._httpClient.delete<void>(`/api/Expenses/${id}`);
  }
}
