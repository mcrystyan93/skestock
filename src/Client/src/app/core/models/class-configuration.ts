export type DepartmentTemplateDto = {
  id: string;
  name: string;
  responsibilities: string;
};

export type SharedClassConfigurationDto = {
  isConfigured: boolean;
  invitationCount: number;
  canManage: boolean;
  departments: DepartmentTemplateDto[];
};

export type SaveSharedClassConfigurationRequest = {
  invitationCount: number;
};
