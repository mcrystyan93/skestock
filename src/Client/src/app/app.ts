import { Component, inject } from '@angular/core';
import { AuthStore } from '@ske/auth';
import { LoaderDirective } from '@ske/shared/loader';
import { RouterOutlet } from '@angular/router';

@Component({
  imports: [
    LoaderDirective,
    RouterOutlet
  ],
  selector: 'ske-root',
  templateUrl: './app.html'
})
export class App {
  public readonly authStore = inject(AuthStore);
}
