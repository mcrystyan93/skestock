import { TestBed } from '@angular/core/testing';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { ExpenseUploadForm } from './expense-upload-form';

describe('ExpenseUploadForm', () => {
  beforeEach(() => TestBed.configureTestingModule({ imports: [ExpenseUploadForm], providers: [provideNzIconsTesting()] }));

  it('lists the failed files only while progress is shown', () => {
    const fixture = TestBed.createComponent(ExpenseUploadForm);
    fixture.componentRef.setInput('failedFileNames', ['bon.pdf']);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('bon.pdf');

    fixture.componentRef.setInput('showProgress', true);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('bon.pdf');
  });
});
