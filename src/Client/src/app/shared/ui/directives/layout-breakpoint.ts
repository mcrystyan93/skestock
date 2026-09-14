import {DestroyRef, Directive, effect, inject, input, TemplateRef, ViewContainerRef} from '@angular/core';
import {Breakpoint, gridResponsiveMap, NzBreakpointService} from 'ng-zorro-antd/core/services';
import {toSignal} from '@angular/core/rxjs-interop';

@Directive({
  selector: '[skeLayoutBreakpoint]',
  standalone: true,
})
export class LayoutBreakpoint {
  public readonly layoutBreakpoint = input<Breakpoint>('lg', {alias: 'skeLayoutBreakpoint'});
  public readonly skeLayoutBreakpointElse = input<TemplateRef<unknown>>();

  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainerRef = inject(ViewContainerRef);
  private readonly _breakpointService = inject(NzBreakpointService);
  private readonly _destroyRef = inject(DestroyRef);

  private readonly breakpointState = toSignal(
    this._breakpointService.subscribe(gridResponsiveMap, true),
    {initialValue: null}
  );

  constructor() {
    effect(() => {
      const breakpointMap = this.breakpointState();

      if (!breakpointMap) {
        return;
      }

      this.viewContainerRef.clear();

      if (breakpointMap[this.layoutBreakpoint()] ?? false) {
        this.viewContainerRef.createEmbeddedView(this.templateRef);
        return;
      }

      const elseTemplate = this.skeLayoutBreakpointElse();

      if (elseTemplate) {
        this.viewContainerRef.createEmbeddedView(elseTemplate);
      }
    });
  }

}
