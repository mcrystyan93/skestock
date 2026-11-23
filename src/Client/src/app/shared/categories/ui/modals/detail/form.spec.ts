import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideNzI18n, ro_RO } from 'ng-zorro-antd/i18n';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { CategoryDto } from '@ske/models';
import { Form } from './form';

describe('Category Form parent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideNzIconsTesting(), provideNzI18n(ro_RO)]
    });
  });

  function create(category: Partial<CategoryDto>) {
    const fixture = TestBed.createComponent(Form);
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('category', category);
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  it('prefills the parent from the category', async () => {
    const form = create({ id: 'c1', name: 'Milk', parentId: 'p1', parentName: 'Dairy' });

    const { isValid, formData } = await form.submit();

    expect(isValid).toBe(true);
    expect(formData?.parent).toMatchObject({ id: 'p1', name: 'Dairy' });
  });

  it('has no parent for a root category', async () => {
    const form = create({ id: 'c1', name: 'Dairy' });

    const { formData } = await form.submit();

    expect(formData?.parent).toBeNull();
  });
});
