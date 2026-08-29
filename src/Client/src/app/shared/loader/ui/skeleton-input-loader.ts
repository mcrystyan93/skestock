import {
  Component,
  ComponentRef,
  Directive,
  effect,
  inject,
  input,
  TemplateRef,
  ViewContainerRef,
} from '@angular/core';
import {NzSkeletonComponent} from 'ng-zorro-antd/skeleton';

@Directive({
  selector: '[skeletonInputLoader]',
})
export class SkeletonInputLoaderDirective {
  private readonly _templateRef = inject(TemplateRef<unknown>);
  private readonly _viewContainerRef = inject(ViewContainerRef);

  public readonly isLoading = input(false, {alias: 'skeletonInputLoader'});

  private _loaderRef: ComponentRef<SkeletonInputLoader> | null = null;

  constructor() {
    effect(() => {
      this.render(this.isLoading());
    });
  }

  private render(isLoading: boolean) {
    this._viewContainerRef.clear();
    this._loaderRef = null;

    if (isLoading) {
      this._loaderRef = this._viewContainerRef.createComponent(SkeletonInputLoader);
      return;
    }

    this._viewContainerRef.createEmbeddedView(this._templateRef);
  }
}

@Component({
  selector: 'app-skeleton-input-loader',
  template: `
    <nz-skeleton
      [nzTitle]="true"
      [nzParagraph]="{ rows: 1, width: '100%' }"
    />
  `,
  imports: [NzSkeletonComponent],
  // host: {
  //   class: 'flex w-full h-full justify-center items-center',
  // },
})
export class SkeletonInputLoader {
}
