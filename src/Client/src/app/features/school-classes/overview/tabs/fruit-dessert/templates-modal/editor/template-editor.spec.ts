import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TemplateEditor } from './template-editor';

describe('TemplateEditor', () => {
  let fixture: ComponentFixture<TemplateEditor>;
  let component: TemplateEditor;

  const create = async (value: string) => {
    fixture = TestBed.createComponent(TemplateEditor);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('value', value);
    fixture.componentRef.setInput('placeholders', [{ token: '{Date}', label: 'Data', example: '10 noiembrie' }]);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  };

  it('loads the initial value and counts its text', async () => {
    await create('<p>Salut</p>');

    expect(component.editor()!.getHTML()).toBe('<p>Salut</p>');
    expect(component.textLength()).toBe(5);
    expect(fixture.nativeElement.textContent).toContain('≈ 5 / 2000');
  });

  it('toggles bold on the selection and reports it as active', async () => {
    await create('<p>Salut</p>');
    component.editor()!.commands.selectAll();

    component.toggle('bold');

    expect(component.editor()!.getHTML()).toBe('<p><strong>Salut</strong></p>');
    expect(component.value()).toBe('<p><strong>Salut</strong></p>');
    expect(component.isActive('bold')).toBe(true);
  });

  it('inserts the placeholder token at the cursor from the dropdown handler', async () => {
    await create('<p>Ab</p>');
    component.editor()!.commands.setTextSelection(2);

    component.insertPlaceholder('{Date}');

    expect(component.editor()!.getText()).toBe('A{Date}b');
    expect(component.textLength()).toBe(8);
  });

  it('applies external value changes without echoing them back', async () => {
    await create('<p>a</p>');
    const changes: string[] = [];
    component.value.subscribe(v => changes.push(v));

    fixture.componentRef.setInput('value', '<p>nou</p>');
    fixture.detectChanges();
    await fixture.whenStable();

    expect(component.editor()!.getHTML()).toBe('<p>nou</p>');
    expect(changes).toEqual([]);
  });

  it('writes an empty string when the document is cleared', async () => {
    await create('<p>a</p>');
    component.editor()!.commands.clearContent(true);

    expect(component.value()).toBe('');
  });
});
