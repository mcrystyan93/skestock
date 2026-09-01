import { isNil } from 'lodash-es';

export const toDateOnlyString = (date: Date | null): string | null => {
  if (isNil(date)) return null;

  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');

  return `${year}-${month}-${day}`;
};
