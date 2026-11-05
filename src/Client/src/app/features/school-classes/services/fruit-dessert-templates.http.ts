import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { FruitDessertMessageType, MessageTemplatesDto, UpdateMessageTemplateRequest } from '@ske/models';

/**
 * HTTP client for the fruit-dessert message templates, mapped under /api/FruitDessertTemplates.
 */
@Service()
export class FruitDessertTemplatesHttp {
  private readonly _httpClient = inject(HttpClient);

  public get() {
    return this._httpClient.get<MessageTemplatesDto>('/api/FruitDessertTemplates');
  }

  public update(type: FruitDessertMessageType, request: UpdateMessageTemplateRequest) {
    return this._httpClient.put<void>(`/api/FruitDessertTemplates/${type}`, request);
  }
}
