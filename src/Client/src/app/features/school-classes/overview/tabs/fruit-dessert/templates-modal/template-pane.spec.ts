import { TestBed } from '@angular/core/testing';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { FruitDessertMessageType, type MessageTemplateDto } from '@ske/models';
import { TemplatePane } from './template-pane';

type PaneInternals = { templateForm: () => { value: { set(v: { name: string; body: string }): void; (): { name: string } } } };

describe('TemplatePane', () => {
  const template: MessageTemplateDto = {
    type: FruitDessertMessageType.Reminder,
    name: 'Reminder',
    body: '<p>a</p>',
    lastModified: '2026-01-01T00:00:00Z'
  };

  const create = () => {
    TestBed.configureTestingModule({ providers: [provideNzIconsTesting()] });
    const fixture = TestBed.createComponent(TemplatePane);
    fixture.componentRef.setInput('template', template);
    fixture.componentRef.setInput('placeholders', []);
    fixture.detectChanges();
    const form = () => (fixture.componentInstance as unknown as PaneInternals).templateForm();
    return { fixture, form };
  };

  it('keeps edits typed while a save was in flight when the template is patched after the save', () => {
    const { fixture, form } = create();
    form().value.set({ name: 'Reminder', body: '<p>newer</p>' });
    fixture.componentRef.setInput('template', { ...template, body: '<p>sent</p>', lastModified: '2026-01-02T00:00:00Z' });
    fixture.detectChanges();
    expect(form().value()).toEqual({ name: 'Reminder', body: '<p>newer</p>' });
  });

  it('reloads the form when a different template is shown', () => {
    const { fixture, form } = create();

    fixture.componentRef.setInput('template', { ...template, type: FruitDessertMessageType.Thanks, name: 'Mulțumire' });
    fixture.detectChanges();

    expect(form().value().name).toBe('Mulțumire');
  });
});
