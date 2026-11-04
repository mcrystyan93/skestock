import { TestBed } from '@angular/core/testing';
import { NZ_MODAL_DATA, NzModalRef } from 'ng-zorro-antd/modal';
import { NEVER, of, throwError } from 'rxjs';
import { type CongregationDto } from '@ske/models';
import { CongregationDetailModal } from './congregation-detail-modal';
// noinspection ES6PreferShortImport
import { CongregationsHttp } from '../../../services/congregations.http';

describe('CongregationDetailModal', () => {
  const existing: CongregationDto = {
    id: 'congregation-1',
    name: 'Central',
    email: 'central@example.com',
    canReceiveStudents: false,
    isSmallCongregation: false,
    createdDate: '2026-01-01T00:00:00Z',
    lastModifiedDate: '2026-01-01T00:00:00Z'
  };

  let http: { getById: ReturnType<typeof vi.fn>; create: ReturnType<typeof vi.fn>; update: ReturnType<typeof vi.fn> };
  let modalRef: { close: ReturnType<typeof vi.fn>; updateConfig: ReturnType<typeof vi.fn> };

  function setup(data: { congregation: CongregationDto | null }) {
    http = { getById: vi.fn().mockReturnValue(of(existing)), create: vi.fn(), update: vi.fn() };
    modalRef = { close: vi.fn(), updateConfig: vi.fn() };
    TestBed.configureTestingModule({
      imports: [CongregationDetailModal],
      providers: [
        { provide: CongregationsHttp, useValue: http },
        { provide: NZ_MODAL_DATA, useValue: data },
        { provide: NzModalRef, useValue: modalRef }
      ]
    });
    const fixture = TestBed.createComponent(CongregationDetailModal);
    fixture.detectChanges();
    return fixture;
  }

  it('does not call the API when the new congregation form is invalid', async () => {
    const fixture = setup({ congregation: null });

    await fixture.componentInstance.save();

    expect(http.getById).not.toHaveBeenCalled();
    expect(http.create).not.toHaveBeenCalled();
    expect(modalRef.close).not.toHaveBeenCalled();
  });

  it('loads an existing congregation by id', () => {
    setup({ congregation: existing });

    expect(http.getById).toHaveBeenCalledWith(existing.id);
  });

  it('updates the loaded congregation and closes with the saved value', async () => {
    const fixture = setup({ congregation: existing });
    http.update.mockReturnValue(of({ ...existing, name: 'Renamed' }));
    fixture.detectChanges();

    await fixture.componentInstance.save();

    expect(http.update).toHaveBeenCalledWith(
      existing.id,
      expect.objectContaining({ name: 'Central', email: 'central@example.com' })
    );
    expect(modalRef.close).toHaveBeenCalledWith(expect.objectContaining({ id: existing.id, name: 'Renamed' }));
  });

  it('keeps the modal open on a plain save after a failed "save and close"', async () => {
    const fixture = setup({ congregation: existing });
    http.update.mockReturnValueOnce(throwError(() => ({ status: 400 })));
    fixture.detectChanges();

    await fixture.componentInstance.save(true);
    expect(modalRef.close).not.toHaveBeenCalled();

    http.update.mockReturnValueOnce(of({ ...existing, name: 'Fixed' }));
    await fixture.componentInstance.save(false);

    expect(http.update).toHaveBeenCalledTimes(2);
    expect(modalRef.close).not.toHaveBeenCalled();
  });

  it('does not close early on "save and close" after an earlier plain save', async () => {
    const fixture = setup({ congregation: existing });
    fixture.detectChanges();
    http.update.mockReturnValueOnce(of({ ...existing, name: 'First' }));
    await fixture.componentInstance.save(false);
    expect(modalRef.close).not.toHaveBeenCalled();

    // The request never answers, so the modal must stay open.
    http.update.mockReturnValueOnce(NEVER);
    await fixture.componentInstance.save(true);

    expect(modalRef.close).not.toHaveBeenCalled();
  });

  describe('server validation errors', () => {
    const duplicateName = {
      status: 400,
      error: { code: 'validation', errors: [{ field: 'Name', code: 'validation.duplicate_name' }] }
    };

    async function saveWith(error: unknown) {
      const fixture = setup({ congregation: existing });
      http.update.mockReturnValue(throwError(() => error));
      fixture.detectChanges();
      await fixture.componentInstance.save();
      fixture.detectChanges();
      return fixture.nativeElement as HTMLElement;
    }

    it('shows a duplicate-name error under the Name field and not in the alert', async () => {
      const element = await saveWith(duplicateName);

      const form = element.querySelector('ske-congregation-form')!;
      expect(form.textContent).toContain('Există deja o înregistrare cu acest nume.');
      expect(element.querySelector('ske-validation-problem-detail-alert')).toBeNull();
    });

    it('keeps errors that belong to no field in the alert', async () => {
      const element = await saveWith({
        status: 400,
        error: { code: 'validation', errors: [{ field: 'Other', code: 'validation.required' }] }
      });

      expect(element.querySelector('ske-validation-problem-detail-alert')).not.toBeNull();
    });

    it('splits mixed errors between the field and the alert', async () => {
      const element = await saveWith({
        status: 400,
        error: {
          code: 'validation',
          errors: [
            { field: 'name', code: 'validation.duplicate_name' },
            { field: 'Other', code: 'validation.required' }
          ]
        }
      });

      expect(element.querySelector('ske-congregation-form')!.textContent).toContain('Există deja o înregistrare');
      expect(element.querySelector('ske-validation-problem-detail-alert')).not.toBeNull();
    });
  });
});
