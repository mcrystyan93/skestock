import { TestBed } from '@angular/core/testing';
import { ClassStatus, type SchoolClassDto } from '@ske/models';
import { Form } from './form';

describe('Form (school class)', () => {
  const classOf = (status: ClassStatus): SchoolClassDto => ({
    id: 'class-1',
    name: 'Clasa I',
    startDate: '2026-09-01',
    endDate: '2027-06-15',
    status,
    createdDate: '2026-01-01T00:00:00Z',
    lastModifiedDate: '2026-01-01T00:00:00Z'
  });

  const create = (status: ClassStatus) => {
    const fixture = TestBed.createComponent(Form);
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('schoolClass', classOf(status));
    return fixture.componentInstance;
  };

  it('does not offer Closed on an open class', () => {
    const form = create(ClassStatus.Active);

    expect(form.statusOptions().map(o => o.value)).not.toContain(ClassStatus.Closed);
    expect(form.schoolClassForm.status().disabled()).toBe(false);
  });

  it('keeps a closed class editable but locks its status', async () => {
    const form = create(ClassStatus.Closed);
    form.schoolClassForm.name().value.set('Clasa I - corectat');

    const { isValid, formData } = await form.submit();

    expect(isValid).toBe(true);
    expect(formData?.name).toBe('Clasa I - corectat');
    expect(formData?.status).toBe(ClassStatus.Closed);
    expect(form.statusOptions().map(o => o.value)).toContain(ClassStatus.Closed);
    expect(form.schoolClassForm.status().disabled()).toBe(true);
  });
});
