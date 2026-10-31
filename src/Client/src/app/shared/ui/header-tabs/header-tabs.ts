import { afterRenderEffect, Component, computed, ElementRef, inject, input, model } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzSegmentedComponent } from 'ng-zorro-antd/segmented';
import { NzTabComponent, NzTabsComponent } from 'ng-zorro-antd/tabs';

export type HeaderTabOption = {
  label: string;
  compactLabel?: string;
};

@Component({
  imports: [FormsModule, NzSegmentedComponent, NzTabsComponent, NzTabComponent],
  selector: 'ske-header-tabs',
  templateUrl: './header-tabs.html',
})
export class HeaderTabs {
  public readonly options = input.required<readonly HeaderTabOption[]>();
  public readonly compact = input(false);
  public readonly ariaLabel = input('Secțiuni');
  public readonly selectedIndex = model(0);

  private readonly _host = inject<ElementRef<HTMLElement>>(ElementRef);

  // The compact strip scrolls horizontally, so keep the active segment in view (e.g. when opened with ?tab=6).
  private readonly _scrollSelectedIntoView = afterRenderEffect(() => {
    const index = this.selectedIndex();
    this.compact();
    this._host.nativeElement
      .querySelectorAll('.ant-segmented-item')[index]
      ?.scrollIntoView?.({ block: 'nearest', inline: 'nearest' });
  });

  protected readonly segmentOptions = computed(() =>
    this.options().map((option, index) => ({ label: option.compactLabel ?? option.label, value: index }))
  );
}
