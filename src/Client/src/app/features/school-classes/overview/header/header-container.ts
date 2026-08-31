import { Component, inject, model } from '@angular/core';
import { SchoolClassOverviewStore } from '../../services/school-class-overview.store';
import { Header } from './header';

@Component({
  imports: [
    Header
  ],
  selector: 'ske-header-container',
  styles: ``,
  templateUrl: './header-container.html',
})
export class HeaderContainer {
  public readonly selectedTabIndex = model<number>(0);
  public readonly store = inject(SchoolClassOverviewStore);
}
