export type DepartmentTemplateDto = {
  id: string;
  name: string;
  responsibilities: string;
};

export type SaveDepartmentRequest = {
  id: string | null;
  name: string;
  responsibilities: string;
};

export type InvitationCountDto = {
  invitationCount: number;
};

export type SaveInvitationCountRequest = {
  invitationCount: number;
};
