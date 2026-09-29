import { computed, Service, signal } from '@angular/core';

@Service()
export class NavigationDrawerState {
  private readonly _open = signal(false);
  private readonly _toggleCount = signal(0);

  public readonly open = this._open.asReadonly();
  public readonly hasToggle = computed(() => this._toggleCount() > 0);

  public toggle(): void {
    this._open.update(open => !open);
  }

  public close(): void {
    this._open.set(false);
  }

  public register(): void {
    this._toggleCount.update(count => count + 1);
  }

  public unregister(): void {
    this._toggleCount.update(count => Math.max(0, count - 1));
    if (!this.hasToggle()) {
      this.close();
    }
  }
}
