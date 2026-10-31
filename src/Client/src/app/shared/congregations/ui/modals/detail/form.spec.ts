import { TestBed } from '@angular/core/testing';
import { CongregationForm } from './form';

describe('CongregationForm', () => {
  function createForm(congregation = {}) {
    const fixture = TestBed.createComponent(CongregationForm);
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('congregation', congregation);
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  beforeEach(() => TestBed.configureTestingModule({ imports: [CongregationForm] }));

  it('requires a name and an email', async () => {
    const form = createForm();

    const { isValid, formData } = await form.submit();

    expect(isValid).toBe(false);
    expect(formData).toBeNull();
    expect(form.congregationForm.name().errors().map((e) => e.message)).toContain('Numele congregației este obligatoriu.');
    expect(form.congregationForm.email().errors().map((e) => e.message)).toContain('Emailul este obligatoriu.');
  });

  it('rejects an invalid email', async () => {
    const form = createForm({ name: 'Central', email: 'nope' });

    expect((await form.submit()).isValid).toBe(false);
    expect(form.congregationForm.email().errors().map((e) => e.message)).toContain('Emailul nu este valid.');
  });

  it('rejects values above the backend max lengths', async () => {
    const form = createForm({
      name: 'a'.repeat(101),
      email: 'central@example.com',
      coordinatorPhone: '1'.repeat(31),
      message: 'm'.repeat(1001)
    });

    expect((await form.submit()).isValid).toBe(false);
    expect(form.congregationForm.name().errors().length).toBe(1);
    expect(form.congregationForm.coordinatorPhone().errors().length).toBe(1);
    expect(form.congregationForm.message().errors().length).toBe(1);
  });

  it('submits a valid form with booleans defaulting to false', async () => {
    const form = createForm({ name: 'Central', email: 'central@example.com' });

    const { isValid, formData } = await form.submit();

    expect(isValid).toBe(true);
    expect(formData).toMatchObject({
      name: 'Central',
      email: 'central@example.com',
      canReceiveStudents: false,
      isSmallCongregation: false
    });
  });

  it('prefills the form from an existing congregation', () => {
    const form = createForm({ id: 'c-1', name: 'Central', email: 'c@example.com', canReceiveStudents: true });

    expect(form.congregationForm.name().value()).toBe('Central');
    expect(form.congregationForm.canReceiveStudents().value()).toBe(true);
  });
});
