import { Component, input, output, signal } from '@angular/core';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzUploadComponent, NzUploadFile } from 'ng-zorro-antd/upload';

@Component({
  imports: [
    NzUploadComponent,
    NzIconDirective
  ],
  selector: 'ske-file-upload',
  templateUrl: './file-upload.html'
})
export class FileUpload {
  public readonly multiple = input(false);
  public readonly fileList = signal<Array<NzUploadFile>>([]);
  public readonly onFileListChange = output<Array<NzUploadFile>>();

  public beforeUpload = (file: NzUploadFile) => {
    const files = this.multiple()
      ? [...this.fileList(), file]
      : [file];

    this.fileList.set(files);
    this.onFileListChange.emit(files);

    return false;
  };

  public removeFile(file: NzUploadFile) {
    const files = this.fileList().filter((currentFile) => currentFile !== file);

    this.fileList.set(files);
    this.onFileListChange.emit(files);
  }
}
