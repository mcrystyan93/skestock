import { Pipe, PipeTransform } from '@angular/core';
import { isNil } from 'lodash-es';
import { formatDistanceToNow } from 'date-fns';
import { ro } from 'date-fns/locale';

@Pipe({
  name: 'timeAgo',
  standalone: true
})
export class TimeAgoPipe implements PipeTransform {
  transform(value: Date | string | number | null | undefined): string {
    if (isNil(value))
      return '-';

    return formatDistanceToNow(new Date(value), { addSuffix: true, locale: ro });
  }
}
