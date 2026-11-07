import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { FruitDessertMessageDispatchDto, FruitDessertMessageType, FruitDessertPlanEntryDto } from '@ske/models';

/**
 * HTTP client for src/Web/Endpoints/FruitDessertPlans.cs, mapped under /api/FruitDessertPlans.
 */
@Service()
export class FruitDessertPlansHttp {
  private readonly _httpClient = inject(HttpClient);

  public get(classId: string) {
    return this._httpClient.get<FruitDessertPlanEntryDto[]>(`/api/FruitDessertPlans/${classId}`);
  }

  public setCongregation(classId: string, entryId: string, congregationId: string | null) {
    return this._httpClient.put<FruitDessertPlanEntryDto>(
      `/api/FruitDessertPlans/${classId}/entries/${entryId}/congregation`, { congregationId });
  }

  public setEmailSent(classId: string, entryId: string, emailSent: boolean) {
    return this._httpClient.put<FruitDessertPlanEntryDto>(
      `/api/FruitDessertPlans/${classId}/entries/${entryId}/email-sent`, { emailSent });
  }

  public sendMessage(classId: string, entryId: string, type: FruitDessertMessageType) {
    return this._httpClient.post<FruitDessertMessageDispatchDto>(
      `/api/FruitDessertPlans/${classId}/entries/${entryId}/messages`, { type });
  }

  public downloadInformare(classId: string, entryId: string) {
    return this._httpClient.get(`/api/FruitDessertPlans/${classId}/entries/${entryId}/informare`, {
      responseType: 'blob',
      observe: 'response'
    });
  }
}
