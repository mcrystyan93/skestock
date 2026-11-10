import { signal, WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { InvitationSplitDto } from '@ske/models';
import { ConfigurationStore } from '../services/configuration.store';
import { InvitationSplitCard } from './invitation-split-card';
import { InvitationSplitSection } from './invitation-split-section';

describe('InvitationSplitSection', () => {
  let fixture: ComponentFixture<InvitationSplitSection>;
  let store: {
    invitationSplit: WritableSignal<InvitationSplitDto>;
    invitationCount: WritableSignal<number>;
    invitationSplitLoading: WritableSignal<boolean>;
    invitationSplitProblemDetail: WritableSignal<null>;
    invitationSplitValidationErrors: WritableSignal<null>;
    saveInvitationSplit: ReturnType<typeof vi.fn>;
  };
  const split: InvitationSplitDto = {
    studentInvitationCount: 10,
    instructorInvitationCount: 5,
    openingPrayerInvitationCount: 2,
    experienceInvitationCount: 3,
    changeInvitationCount: 4,
  };

  beforeEach(() => {
    store = {
      invitationSplit: signal(split),
      invitationCount: signal(30),
      invitationSplitLoading: signal(false),
      invitationSplitProblemDetail: signal(null),
      invitationSplitValidationErrors: signal(null),
      saveInvitationSplit: vi.fn(),
    };
    TestBed.configureTestingModule({
      imports: [InvitationSplitSection],
      providers: [{ provide: ConfigurationStore, useValue: store }],
    });
    fixture = TestBed.createComponent(InvitationSplitSection);
    fixture.detectChanges();
  });

  const card = () =>
    fixture.debugElement.query(By.directive(InvitationSplitCard)).componentInstance as InvitationSplitCard;

  const submitCardForm = async () => {
    fixture.nativeElement
      .querySelector('form')
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    await fixture.whenStable();
  };

  it('renders the heading and the saved invitation count in the total line', () => {
    expect(fixture.nativeElement.querySelector('h2').textContent).toContain('Împărțirea invitațiilor');
    expect(fixture.nativeElement.textContent.replace(/\s+/g, ' ')).toContain('Total: 24 × 30 = 720 invitații');
  });

  it('submits all five values through the configuration store', async () => {
    card().splitForm.changeInvitationCount().value.set(9);
    fixture.detectChanges();

    await submitCardForm();

    expect(store.saveInvitationSplit).toHaveBeenCalledWith({ ...split, changeInvitationCount: 9 });
  });

  it('does not send an invalid split to the store', async () => {
    card().splitForm.studentInvitationCount().value.set(201);
    fixture.detectChanges();

    await submitCardForm();

    expect(store.saveInvitationSplit).not.toHaveBeenCalled();
  });

  it('does not render the form while a store request is active', () => {
    store.invitationSplitLoading.set(true);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    expect(store.saveInvitationSplit).not.toHaveBeenCalled();
  });
});
