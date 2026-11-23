import { Component, input, output } from '@angular/core';
import { NzProgressComponent, NzProgressStatusType } from 'ng-zorro-antd/progress';
import { NzUploadFile } from 'ng-zorro-antd/upload';
import { FileUpload } from '@ske/shared/storage';

@Component({
  imports: [NzProgressComponent, FileUpload],
  selector: 'ske-expense-upload-form',
  templateUrl: './expense-upload-form.html'
})
export class ExpenseUploadForm {
  public readonly showProgress = input(false);
  public readonly percent = input(0);
  public readonly status = input<NzProgressStatusType>('active');
  public readonly progressLabel = input('');
  public readonly currentFileName = input<string | null>(null);
  public readonly failedFileNames = input<string[]>([]);

  public readonly onFileListChange = output<NzUploadFile[]>();

  public readonly format = () => this.progressLabel();
}
