import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ClassStatisticsHttp } from './class-statistics.http';

describe('ClassStatisticsHttp', () => {
  let service: ClassStatisticsHttp;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(ClassStatisticsHttp);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTestingController.verify());

  it('requests the class chart for all locations', () => {
    let response: unknown;

    service.getStockByCategoryAllLocations('class-id').subscribe((value) => {
      response = value;
    });

    const request = httpTestingController.expectOne(
      '/api/statistics/class/class-id/stock-by-category'
    );
    expect(request.request.method).toBe('GET');
    request.flush({
      labels: ['Bucătărie'],
      labelIds: ['location-id'],
      series: [{ name: 'Alimente', data: [12] }]
    });

    expect(response).toEqual({
      labels: ['Bucătărie'],
      labelIds: ['location-id'],
      series: [{ name: 'Alimente', data: [12] }]
    });
  });

  it('requests the per-item drill-down chart for a location', () => {
    service.getLocationStockByItem('class-id', 'location-id').subscribe();

    const request = httpTestingController.expectOne(
      '/api/statistics/class/class-id/location/location-id/stock-by-item'
    );
    expect(request.request.method).toBe('GET');
    request.flush({
      labels: ['Orez'],
      labelIds: ['item-id'],
      series: [{ name: 'Alimente', data: [12] }]
    });
  });

  it('requests stock item ids from the visible class stock report without filtering quantities', () => {
    let itemIds: string[] | undefined;
    service.getClassStockItemIds('class-id').subscribe((result) => {
      itemIds = result;
    });

    const request = httpTestingController.expectOne('/api/Stock/class/class-id');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      filters: [],
      searchTerm: null,
      includeHidden: false,
      lowStockOnly: false,
      expiredOnly: false
    });
    request.flush({
      hasExpiredItems: false,
      items: [
        { itemId: 'positive-item', quantity: 4 },
        { itemId: 'zero-item', quantity: 0 },
        { itemId: 'negative-item', quantity: -2 },
        { itemId: 'positive-item', quantity: 1 }
      ]
    });

    expect(itemIds).toEqual(['positive-item', 'zero-item', 'negative-item']);
  });

  it('requests stock evolution for one item in a class', () => {
    service.getItemStockEvolution('class-id', 'item-id').subscribe();

    const request = httpTestingController.expectOne(
      '/api/statistics/class/class-id/item/item-id/stock-evolution'
    );
    expect(request.request.method).toBe('GET');
    request.flush({
      itemId: 'item-id',
      itemName: 'Creion',
      sku: null,
      unit: 'bucată',
      points: [
        { date: '2026-09-01', cumulativeQuantity: 5 },
        { date: '2026-09-05', cumulativeQuantity: 3 }
      ]
    });
  });
});
