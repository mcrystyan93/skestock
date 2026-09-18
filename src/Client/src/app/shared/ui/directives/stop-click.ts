import { Directive, HostListener } from '@angular/core';

@Directive({
  selector: '[skeStopClick]',
  standalone: true
})
export class StopClick {
  @HostListener('click', ['$event'])
  public onClick(event: MouseEvent): void {
    event.stopPropagation();
    event.preventDefault();
  }
}
