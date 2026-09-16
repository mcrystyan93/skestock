import { Service, signal } from '@angular/core';

const SHOW_HIDDEN_PRODUCTS_KEY = 'stock.showHiddenProducts';

@Service()
export class StockPreferencesService {
  private readonly _showHiddenProducts = signal(
    localStorage.getItem(SHOW_HIDDEN_PRODUCTS_KEY) === 'true'
  );

  public readonly showHiddenProducts = this._showHiddenProducts.asReadonly();

  public setShowHiddenProducts(showHiddenProducts: boolean): void {
    this._showHiddenProducts.set(showHiddenProducts);
    localStorage.setItem(SHOW_HIDDEN_PRODUCTS_KEY, String(showHiddenProducts));
  }
}
