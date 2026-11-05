import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, Subject, throwError } from 'rxjs';
import { FruitDessertMessageType, type MessageTemplatesDto } from '@ske/models';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalRef } from 'ng-zorro-antd/modal';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { FruitDessertTemplatesHttp } from '../../../../services/fruit-dessert-templates.http';
import { TemplatesModal } from './templates-modal';

describe('TemplatesModal', () => {
  const response: MessageTemplatesDto = {
    templates: [
      { type: FruitDessertMessageType.Reminder, name: 'Reminder', body: '<p>a</p>', lastModified: '2026-01-01T00:00:00Z' },
      { type: FruitDessertMessageType.Thanks, name: 'Mulțumire', body: '<p>b</p>', lastModified: '2026-01-01T00:00:00Z' }
    ],
    placeholders: [{ token: '{Date}', label: 'Data', example: '10 noiembrie' }]
  };

  let http: { get: ReturnType<typeof vi.fn>; update: ReturnType<typeof vi.fn> };
  let message: { success: ReturnType<typeof vi.fn> };
  let fixture: ComponentFixture<TemplatesModal>;

  const create = async () => {
    http.get.mockReturnValue(of(response));
    fixture = TestBed.createComponent(TemplatesModal);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  };

  const saveButton = () =>
    Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>).find(
      (b) => b.textContent?.trim() === 'Salvează'
    )!;

  beforeEach(() => {
    http = { get: vi.fn(), update: vi.fn() };
    message = { success: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        provideNzIconsTesting(),
        { provide: FruitDessertTemplatesHttp, useValue: http },
        { provide: NzMessageService, useValue: message },
        { provide: NzModalRef, useValue: { close: vi.fn(), updateConfig: vi.fn() } }
      ]
    });
  });

  it('loads and shows both templates as tabs', async () => {
    await create();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Reminder');
    expect(text).toContain('Mulțumire');
    expect(fixture.componentInstance.store.templates().length).toBe(2);
  });

  it('saves the form values of the active template and confirms', async () => {
    await create();
    const response$ = new Subject<void>();
    http.update.mockReturnValue(response$);

    saveButton().click();
    await fixture.whenStable();
    fixture.detectChanges(); // lets the saving effect observe the in-flight state
    response$.next();
    response$.complete();
    fixture.detectChanges();

    expect(http.update).toHaveBeenCalledWith(FruitDessertMessageType.Reminder, { name: 'Reminder', body: '<p>a</p>' });
    expect(message.success).toHaveBeenCalledWith('Șablon salvat');
  });

  it('disables saving while a save is in flight so a second save is not dropped', async () => {
    await create();
    http.update.mockReturnValue(new Subject<void>());

    saveButton().click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(saveButton().disabled).toBe(true);
  });

  it('shows the server error in the tab and does not confirm', async () => {
    await create();
    http.update.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 400, error: { status: 400, detail: 'Mesajul este prea lung.' } }))
    );

    saveButton().click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Mesajul este prea lung.');
    expect(message.success).not.toHaveBeenCalled();
  });
});
