import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NzBreakpointService } from 'ng-zorro-antd/core/services';
import { of } from 'rxjs';
import { LayoutBreakpoint } from './layout-breakpoint';

@Component({
  imports: [LayoutBreakpoint],
  template: `
    <ng-template [skeLayoutBreakpoint]="'lg'">
      <span data-layout-breakpoint>visible</span>
    </ng-template>
  `
})
class TestHost {}

describe('LayoutBreakpoint', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TestHost],
      providers: [{
        provide: NzBreakpointService,
        useValue: {
          subscribe: vi.fn().mockReturnValue(of({
            xxxl: false,
            xxl: false,
            xl: false,
            lg: true,
            md: false,
            sm: false,
            xs: false
          }))
        }
      }]
    });
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('creates through Angular and renders content for the active breakpoint', () => {
    const fixture = TestBed.createComponent(TestHost);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-layout-breakpoint]')).toBeTruthy();
  });
});
