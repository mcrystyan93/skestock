import { inject, Service, Signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

@Service({ autoProvided: false })
export class QueryParamState {
  private readonly _route = inject(ActivatedRoute);
  private readonly _router = inject(Router);

  /** Read-only signal for a single query param */
  public param(key: string, defaultValue: string | null = null): Signal<string | null> {
    return toSignal(
      this._route.queryParamMap.pipe(map(params => params.get(key) ?? defaultValue)),
      { initialValue: this._route.snapshot.queryParamMap.get(key) ?? defaultValue }
    );
  }

  public set(key: string, value: string | null | undefined): void {
    this._router.navigate([], {
      relativeTo: this._route,
      queryParams: { [key]: value ?? null },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }
}
