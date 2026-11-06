/** Mirrors src/Domain/Enums/FruitDessertMessageType.cs (serialized camelCase by JsonStringEnumConverter). */
export enum FruitDessertMessageType {
  Reminder = 'reminder',
  Thanks = 'thanks',
}

/** Mirrors src/Domain/Enums/FruitDessertMessageStatus.cs. */
export enum FruitDessertMessageStatus {
  Opened = 'opened',
  Sent = 'sent',
  Delivered = 'delivered',
  Failed = 'failed',
}

/** Mirrors src/Application/Features/FruitDessertPlans/Models/FruitDessertPlanEntryDto.cs. `date` is a DateOnly (yyyy-MM-dd). */
export type FruitDessertPlanEntryDto = {
  id: string;
  date: string;
  congregationId?: string | null;
  congregationName?: string | null;
  congregationDeleted: boolean;
  contactPerson?: string | null;
  contactPhone?: string | null;
  normalizedPhone?: string | null;
  emailSent: boolean;
  reminderSent: boolean;
  thanksSent: boolean;
};

/** Mirrors src/Application/Features/FruitDessertPlans/Models/FruitDessertMessageDispatchDto.cs. */
export type FruitDessertMessageDispatchDto = {
  url: string;
  type: FruitDessertMessageType;
  status: FruitDessertMessageStatus;
  sentAt: string;
};

/** Mirrors the message template DTOs of GET /api/FruitDessertTemplates. `body` is HTML (WhatsApp-formatted server side). */
export type MessageTemplateDto = {
  type: FruitDessertMessageType;
  name: string;
  body: string;
  lastModified: string;
};

export type MessagePlaceholderDto = {
  token: string;
  label: string;
  example: string;
};

export type MessageTemplatesDto = {
  templates: MessageTemplateDto[];
  placeholders: MessagePlaceholderDto[];
};

export type UpdateMessageTemplateRequest = {
  name: string;
  body: string;
};
