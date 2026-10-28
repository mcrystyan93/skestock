import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { TestBed } from '@angular/core/testing';
import { Form } from './form';

describe('Item Form imageFileId', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [
        provideNzIconsTesting(),
        provideNzIcons([
          { name: 'icons:pencil', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:plus', icon: '<svg viewBox="0 0 24 24"></svg>' },
        ]),
      ],
    }),
  );

  function create(imageFileId: string | null | undefined) {
    const fixture = TestBed.createComponent(Form);
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('item', {
      id: 'i1',
      name: 'N',
      unit: 'buc',
      categoryId: 'c1',
      imageFileId,
    });
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  it('maps the item image into the form model and submits it', async () => {
    const form = create('f1');
    const { isValid, formData } = await form.submit();

    expect(isValid).toBe(true);
    expect(formData?.imageFileId).toBe('f1');
  });

  it('submits null when the image was removed', async () => {
    const form = create('f1');
    form.itemForm.imageFileId().value.set(null);
    const { formData } = await form.submit();

    expect(formData?.imageFileId).toBeNull();
  });
});
