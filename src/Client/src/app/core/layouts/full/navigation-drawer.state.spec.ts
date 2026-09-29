import { TestBed } from '@angular/core/testing';
import { NavigationDrawerState } from './navigation-drawer.state';

describe('NavigationDrawerState', () => {
  let state: NavigationDrawerState;

  beforeEach(() => {
    state = TestBed.inject(NavigationDrawerState);
  });

  it('toggles and closes', () => {
    expect(state.open()).toBe(false);
    state.toggle();
    expect(state.open()).toBe(true);
    state.toggle();
    expect(state.open()).toBe(false);
    state.toggle();
    state.close();
    expect(state.open()).toBe(false);
  });

  it('tracks registered toggles', () => {
    expect(state.hasToggle()).toBe(false);
    state.register();
    state.register();
    state.unregister();
    expect(state.hasToggle()).toBe(true);
  });

  it('closes when the last toggle unregisters', () => {
    state.register();
    state.toggle();
    state.unregister();
    expect(state.hasToggle()).toBe(false);
    expect(state.open()).toBe(false);
  });
});
