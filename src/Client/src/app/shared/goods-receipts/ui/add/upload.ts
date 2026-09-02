import { Component, output, signal } from '@angular/core';
import { NzUploadComponent, NzUploadFile } from 'ng-zorro-antd/upload';
import { NzIconDirective } from 'ng-zorro-antd/icon';

@Component({
  imports: [
    NzUploadComponent,
    NzIconDirective
  ],
  selector: 'ske-add-goods-receipt-upload',
  styles: ``,
  templateUrl: './upload.html'
})
export class Upload {
  public readonly fileList = signal<Array<NzUploadFile>>([]);

  public readonly onFileListChange = output<Array<NzUploadFile>>();

  public beforeUpload = (file: NzUploadFile) => {
    this.fileList.update((prev) => [...prev, file]);

    this.onFileListChange.emit(this.fileList());

    return false;
  };

  public removeFile(file: NzUploadFile) {
    this.fileList.update((prev) => prev.filter((f) => f !== file));

    this.onFileListChange.emit(this.fileList());
  }
}
