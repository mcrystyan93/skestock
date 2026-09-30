import { HttpHeaders, HttpResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { NzMessageService } from 'ng-zorro-antd/message';
import { of } from 'rxjs';
import { OrderListExportService } from './order-list-export.service';
import { OrderListsHttp } from './order-lists.http';

describe('OrderListExportService', () => {
  let orderListsHttp: { export: ReturnType<typeof vi.fn> };
  let service: OrderListExportService;
  let anchor: HTMLAnchorElement;

  beforeEach(() => {
    orderListsHttp = { export: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        OrderListExportService,
        { provide: OrderListsHttp, useValue: orderListsHttp },
        { provide: NzMessageService, useValue: { error: vi.fn() } }
      ]
    });
    service = TestBed.inject(OrderListExportService);

    URL.createObjectURL = vi.fn(() => 'blob:test');
    URL.revokeObjectURL = vi.fn();
    anchor = document.createElement('a');
    vi.spyOn(anchor, 'click').mockImplementation(() => undefined);
    vi.spyOn(document, 'createElement').mockReturnValue(anchor);
  });

  it('downloads xlsx by default', () => {
    orderListsHttp.export.mockReturnValue(of(new HttpResponse({ body: new Blob(['x']) })));

    service.download('1');

    expect(orderListsHttp.export).toHaveBeenCalledWith('1', 'xlsx');
    expect(anchor.download).toBe('comanda.xlsx');
  });

  it('requests png and uses the server file name', () => {
    orderListsHttp.export.mockReturnValue(of(new HttpResponse({
      body: new Blob(['x']),
      headers: new HttpHeaders({ 'Content-Disposition': 'attachment; filename="Lista.png"' })
    })));

    service.download('1', 'png');

    expect(orderListsHttp.export).toHaveBeenCalledWith('1', 'png');
    expect(anchor.download).toBe('Lista.png');
  });
});
