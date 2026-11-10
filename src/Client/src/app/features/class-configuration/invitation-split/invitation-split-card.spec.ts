import {ComponentFixture, TestBed} from '@angular/core/testing';
import {InvitationSplitCard} from './invitation-split-card';

describe('InvitationSplitCard', () => {
  let fixture: ComponentFixture<InvitationSplitCard>;
  const fields = [
    'studentInvitationCount',
    'instructorInvitationCount',
    'openingPrayerInvitationCount',
    'experienceInvitationCount',
    'changeInvitationCount',
  ] as const;

  beforeEach(() => {
    TestBed.configureTestingModule({imports: [InvitationSplitCard]});
    fixture = TestBed.createComponent(InvitationSplitCard);
    fixture.componentRef.setInput('split', {
      studentInvitationCount: 10,
      instructorInvitationCount: 5,
      openingPrayerInvitationCount: 2,
      experienceInvitationCount: 3,
      changeInvitationCount: 4,
    });
    fixture.componentRef.setInput('invitationCount', 30);
    fixture.detectChanges();
  });

  const total = () =>
    (fixture.nativeElement.querySelector('[data-testid="invitation-split-total"]') as HTMLElement)
      .textContent!.replace(/\s+/g, ' ')
      .trim();

  it('renders the five labelled inputs and a save button', () => {
    expect(fixture.nativeElement.querySelectorAll('nz-input-number')).toHaveLength(5);
    for (const label of ['Cursanți', 'Instructori', 'Rugăciune deschidere', 'Experiențe', 'Schimbări']) {
      expect(fixture.nativeElement.textContent).toContain(label);
    }
    expect(fixture.nativeElement.querySelector('button[type="submit"]').textContent).toContain('Salvează');
  });

  it('shows the hints under Cursanți and Instructori', () => {
    expect(fixture.nativeElement.querySelector('#student-invitation-count-hint').textContent).toBe('De obicei în jur de 28–30.');
    expect(fixture.nativeElement.querySelector('#instructor-invitation-count-hint').textContent).toBe('De obicei 2.');
    expect(fixture.nativeElement.querySelectorAll('.ant-form-item-extra')).toHaveLength(2);
  });

  it('multiplies the allocated sum by the invitation count and follows edits', () => {
    expect(total()).toBe('Total: 24 × 30 = 720 invitații');

    fixture.componentInstance.splitForm.studentInvitationCount().value.set(Number.NaN);
    fixture.componentInstance.splitForm.changeInvitationCount().value.set(20);
    fixture.detectChanges();

    expect(total()).toBe('Total: 30 × 30 = 900 invitații');
  });

  it('shows only the sum while the invitation count is not set', () => {
    fixture.componentRef.setInput('invitationCount', 0);
    fixture.detectChanges();

    expect(total()).toBe('Total: 24 (setați „Număr de invitații” pentru a calcula totalul)');
  });

  it('returns a complete payload', async () => {
    fixture.componentInstance.splitForm.experienceInvitationCount().value.set(200);

    const result = await fixture.componentInstance.submit();

    expect(result.isValid).toBe(true);
    expect(result.formData).toEqual({
      studentInvitationCount: 10,
      instructorInvitationCount: 5,
      openingPrayerInvitationCount: 2,
      experienceInvitationCount: 200,
      changeInvitationCount: 4,
    });
  });

  it.each(fields.flatMap((field) => [-1, 201, 2.5, Number.NaN].map((value) => [field, value] as const)))(
    'rejects %s=%s',
    async (field, value) => {
      fixture.componentInstance.splitForm[field]().value.set(value);

      const result = await fixture.componentInstance.submit();

      expect(result.isValid).toBe(false);
      expect(result.formData).toBeNull();
    },
  );

  it('requires a value when the input is cleared', async () => {
    const input = fixture.nativeElement.querySelector('#student-invitation-count') as HTMLInputElement;
    input.value = '';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const result = await fixture.componentInstance.submit();

    expect(result.isValid).toBe(false);
    expect(
      fixture.componentInstance.splitForm.studentInvitationCount()
        .errors()
        .map((error) => error.kind),
    ).toEqual(['required']);
  });
});
