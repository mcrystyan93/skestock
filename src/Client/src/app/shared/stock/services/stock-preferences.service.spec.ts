import { TestBed } from '@angular/core/testing';
import { StockPreferencesService } from './stock-preferences.service';

describe('StockPreferencesService', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [StockPreferencesService]
    });
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('defaults to hiding zero-stock products', () => {
    const service = TestBed.inject(StockPreferencesService);

    expect(service.showHiddenProducts()).toBe(false);
  });

  it('updates and persists the show-hidden preference', () => {
    const service = TestBed.inject(StockPreferencesService);

    service.setShowHiddenProducts(true);

    expect(service.showHiddenProducts()).toBe(true);
    expect(localStorage.getItem('stock.showHiddenProducts')).toBe('true');
  });
});
