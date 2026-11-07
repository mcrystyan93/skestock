import { ApplicationRef, type EmbeddedViewRef, type TemplateRef } from '@angular/core';
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
      { type: FruitDessertMessageType.Thanks, name: 'Mulțumire', body: '<p>b</p>', lastModified: '2026-01-01T00:00:00Z' },
      { type: FruitDessertMessageType.Informare, name: 'Informare', body: '<p>c</p>', lastModified: '2026-01-01T00:00:00Z' }
    ],
    placeholders: [{ token: '{Date}', label: 'Data', example: '10 noiembrie' }]
  };

  let http: { get: ReturnType<typeof vi.fn>; update: ReturnType<typeof vi.fn> };
  let message: { success: ReturnType<typeof vi.fn> };
  let modalRef: { close: ReturnType<typeof vi.fn>; updateConfig: ReturnType<typeof vi.fn> };
  let fixture: ComponentFixture<TemplatesModal>;

  const create = async () => {
    http.get.mockReturnValue(of(response));
    fixture = TestBed.createComponent(TemplatesModal);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    // nzModalFooter hands its template to the modal via updateConfig({ nzFooter })
    const { nzFooter } = modalRef.updateConfig.mock.calls.at(-1)![0] as { nzFooter: TemplateRef<unknown> };
    footer = nzFooter.createEmbeddedView({});
    TestBed.inject(ApplicationRef).attachView(footer);
  };

  // The footer is a template NzModal normally projects; render it by hand since no modal hosts it here.
  let footer: EmbeddedViewRef<unknown>;
  const saveButton = () => {
    footer.detectChanges();
    return Array.from(footer.rootNodes[0].querySelectorAll('button') as NodeListOf<HTMLButtonElement>).find(
      (b) => b.textContent?.trim() === 'Salvează'
    )!;
  };
  const selectTab = async (index: number) => {
    const tab = fixture.nativeElement.querySelectorAll('.ant-tabs-tab')[index] as HTMLElement;
    tab.click();
    await fixture.whenStable();
    fixture.detectChanges();
  };

  beforeEach(() => {
    http = { get: vi.fn(), update: vi.fn() };
    message = { success: vi.fn() };
    modalRef = { close: vi.fn(), updateConfig: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        provideNzIconsTesting(),
        { provide: FruitDessertTemplatesHttp, useValue: http },
        { provide: NzMessageService, useValue: message },
        { provide: NzModalRef, useValue: modalRef }
      ]
    });
  });

  it('loads and shows all templates as tabs', async () => {
    await create();

    const tabs = Array.from(fixture.nativeElement.querySelectorAll('.ant-tabs-tab') as NodeListOf<HTMLElement>).map((t) =>
      t.textContent?.trim()
    );
    expect(tabs).toEqual(['Reminder', 'Mulțumire', 'Informare']);
    expect(fixture.componentInstance.store.templates().length).toBe(3);
  });

  it('saves the Informare template from its own tab', async () => {
    await create();
    http.update.mockReturnValue(of(undefined));

    await selectTab(2);
    saveButton().click();
    await fixture.whenStable();

    expect(http.update).toHaveBeenCalledWith(FruitDessertMessageType.Informare, { name: 'Informare', body: '<p>c</p>' });
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

  it('saves the second tab with that tab type and values', async () => {
    await create();
    http.update.mockReturnValue(new Subject<void>());

    await selectTab(1);
    saveButton().click();
    await fixture.whenStable();

    expect(http.update).toHaveBeenCalledWith(FruitDessertMessageType.Thanks, { name: 'Mulțumire', body: '<p>b</p>' });
  });
});
