import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  DepartmentTemplateDto,
  InvitationCountDto,
  RoomConfigurationDto,
  SaveRoomConfigurationRequest,
  SaveDepartmentRequest,
  SaveInvitationCountRequest,
} from '@ske/models';

const CONFIGURATION_ENDPOINT = '/api/ClassConfiguration';

@Service()
export class ConfigurationHttp {
  private readonly _httpClient = inject(HttpClient);

  public getDepartments() {
    return this._httpClient.get<DepartmentTemplateDto[]>(`${CONFIGURATION_ENDPOINT}/departments`);
  }

  public getDepartment(id: string) {
    return this._httpClient.get<DepartmentTemplateDto>(
      `${CONFIGURATION_ENDPOINT}/departments/${id}`,
    );
  }

  public saveDepartment(request: SaveDepartmentRequest) {
    return this._httpClient.put<DepartmentTemplateDto>(
      `${CONFIGURATION_ENDPOINT}/departments`,
      request,
    );
  }

  public deleteDepartment(id: string) {
    return this._httpClient.delete<void>(`${CONFIGURATION_ENDPOINT}/departments/${id}`);
  }

  public getInvitationCount() {
    return this._httpClient.get<InvitationCountDto>(`${CONFIGURATION_ENDPOINT}/invitations`);
  }

  public saveInvitationCount(request: SaveInvitationCountRequest) {
    return this._httpClient.put<InvitationCountDto>(
      `${CONFIGURATION_ENDPOINT}/invitations`,
      request,
    );
  }

  public getRoomConfiguration() {
    return this._httpClient.get<RoomConfigurationDto>(`${CONFIGURATION_ENDPOINT}/rooms`);
  }

  public saveRoomConfiguration(request: SaveRoomConfigurationRequest) {
    return this._httpClient.put<RoomConfigurationDto>(`${CONFIGURATION_ENDPOINT}/rooms`, request);
  }
}
