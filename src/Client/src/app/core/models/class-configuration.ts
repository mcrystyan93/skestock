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

export type RoomConfigurationDto = {
  room4SeatCount: number;
  room2SeatCount: number;
  room6SeatCount: number;
};

export type SaveRoomConfigurationRequest = RoomConfigurationDto;
