import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { NzMessageService } from 'ng-zorro-antd/message';
import { of, Subject, throwError } from 'rxjs';
import { ConfigurationHttp } from '../services/configuration.http';
import { ConfigurationHeader } from './configuration-header';

describe('ConfigurationHeader', () => {
  let http: { materializePurchaseStatistics: ReturnType<typeof vi.fn> };
  let messageService: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  const create = () => TestBed.inject(ConfigurationHeader);

  beforeEach(() => {
    http = { materializePurchaseStatistics: vi.fn() };
    messageService = { success: vi.fn(), error: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        ConfigurationHeader,
        { provide: ConfigurationHttp, useValue: http },
        { provide: NzMessageService, useValue: messageService },
      ],
    });
  });

  it('shows a success message and stops loading after a successful run', () => {
    http.materializePurchaseStatistics.mockReturnValue(of(42));
    const header = create();

    header.recalculatePurchaseStatistics();

    expect(messageService.success).toHaveBeenCalledOnce();
    expect(header.recalculating()).toBe(false);
  });

  it('ignores a second click while a run is in flight', () => {
    const pending = new Subject<number>();
    http.materializePurchaseStatistics.mockReturnValue(pending);
    const header = create();

    header.recalculatePurchaseStatistics();
    header.recalculatePurchaseStatistics();

    expect(http.materializePurchaseStatistics).toHaveBeenCalledOnce();
    expect(header.recalculating()).toBe(true);

    pending.complete();
    expect(header.recalculating()).toBe(false);
  });

  it.each([
    [409, 'deja în curs'],
    [403, 'Doar administratorii'],
    [500, 'nu au putut fi recalculate'],
  ])('shows a message for a %i error and stops loading', (status, expected) => {
    http.materializePurchaseStatistics.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status })),
    );
    const header = create();

    header.recalculatePurchaseStatistics();

    expect(messageService.error).toHaveBeenCalledWith(expect.stringContaining(expected));
    expect(header.recalculating()).toBe(false);
  });
});
