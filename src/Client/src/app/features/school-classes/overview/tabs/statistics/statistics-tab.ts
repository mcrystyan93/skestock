import { Component, effect, inject, input, untracked } from '@angular/core';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { ClassStatisticsStore } from '../../../services/class-statistics.store';
import { AllLocationsStockCard } from './all-locations-stock-card';
import { SelectedLocationStockCard } from './selected-location-stock-card';
import { ItemStockEvolutionCard } from './item-stock-evolution-card';

@Component({
  imports: [
    NzColDirective,
    NzRowDirective,
    AllLocationsStockCard,
    SelectedLocationStockCard,
    ItemStockEvolutionCard
  ],
  selector: 'ske-school-class-overview-statistics-tab',
  templateUrl: './statistics-tab.html',
  providers: [ClassStatisticsStore],
  host: {
    class: 'block min-w-0 grow overflow-y-auto p-4'
  }
})
export class StatisticsTab {
  public readonly classId = input.required<string | null>();
  public readonly active = input(false);
  public readonly store = inject(ClassStatisticsStore);

  private readonly _loadAllLocationsEffectRef = effect(() => {
    const classId = this.classId();
    const isActive = this.active();

    if (!isActive || !classId) {
      untracked(() => this.store.deactivate());
      return;
    }

    untracked(() => this.store.loadAllLocations(classId));
  });
}
