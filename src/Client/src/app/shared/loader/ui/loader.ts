import {
  Component,
  ComponentRef,
  Directive,
  effect,
  inject,
  input,
  TemplateRef,
  ViewContainerRef
} from '@angular/core';
import { NzIconDirective } from 'ng-zorro-antd/icon';

@Directive({
  selector: '[skeLoader]'
})
export class LoaderDirective {
  private readonly _templateRef = inject(TemplateRef<unknown>);
  private readonly _viewContainerRef = inject(ViewContainerRef);

  public readonly isLoading = input(false, { alias: 'skeLoader' });

  private _loaderRef: ComponentRef<Loader> | null = null;

  constructor() {
    effect(() => {
      this.render(this.isLoading());
    });
  }

  private render(isLoading: boolean) {
    this._viewContainerRef.clear();
    this._loaderRef = null;

    if (isLoading) {
      this._loaderRef = this._viewContainerRef.createComponent(Loader);
      return;
    }

    this._viewContainerRef.createEmbeddedView(this._templateRef);
  }
}

@Component({
  selector: 'ske-loader',
  template: `
    <span nz-icon
          nzType="loading"
          class="text-2xl"></span> `,
  imports: [NzIconDirective],
  host: {
    class: 'flex w-full h-full justify-center items-center'
  }
})
export class Loader {
}
