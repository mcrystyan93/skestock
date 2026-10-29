import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzUploadFile } from 'ng-zorro-antd/upload';
import { StorageHttp } from '@ske/shared/storage';
import { ItemImageUpload } from './item-image-upload';

describe('ItemImageUpload', () => {
  const http = {
    requestUpload: vi.fn(() => of({ fileId: 'f1', uploadUrl: 'http://blob' })),
    uploadViaSasUri: vi.fn(() => of({})),
    confirmUpload: vi.fn(() => of({ fileId: 'f1' })),
  };
  const message = { error: vi.fn() };

  function setup() {
    TestBed.configureTestingModule({
      providers: [
        provideNzIconsTesting(),
        provideNzIcons([
          { name: 'picture-twotone', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:plus', icon: '<svg viewBox="0 0 24 24"></svg>' },
        ]),
        { provide: StorageHttp, useValue: http },
        { provide: NzMessageService, useValue: message },
      ],
    });
    const fixture = TestBed.createComponent(ItemImageUpload);
    fixture.detectChanges();
    return fixture;
  }

  const file = (type: string, size: number) =>
    ({ uid: '1', name: 'a', type, size }) as NzUploadFile;
  const run = (f: ReturnType<typeof setup>, upload: NzUploadFile) =>
    (f.componentInstance as unknown as { beforeUpload: (x: NzUploadFile) => boolean }).beforeUpload(
      upload,
    );

  beforeEach(() => vi.clearAllMocks());

  it('rejects an unsupported type', () => {
    const f = setup();
    run(f, file('application/pdf', 10));

    expect(message.error).toHaveBeenCalledWith('Format neacceptat');
    expect(http.requestUpload).not.toHaveBeenCalled();
  });

  it('rejects files over 5 MB', () => {
    const f = setup();
    run(f, file('image/png', 5 * 1024 * 1024 + 1));

    expect(message.error).toHaveBeenCalledWith('Imaginea depășește 5 MB');
    expect(http.requestUpload).not.toHaveBeenCalled();
  });

  it('emits the file id after upload and clears on remove', () => {
    const f = setup();
    run(f, file('image/png', 100));
    f.detectChanges();

    expect(f.componentInstance.value()).toBe('f1');

    (f.componentInstance as unknown as { remove: () => boolean }).remove();

    expect(f.componentInstance.value()).toBeNull();
  });

  it('reports uploading while in flight and clears it when done', () => {
    const pending = new Subject<object>();
    http.uploadViaSasUri.mockReturnValueOnce(pending);
    const f = setup();
    run(f, file('image/png', 100));
    f.detectChanges();

    expect(f.componentInstance.uploading()).toBe(true);
    expect(f.componentInstance.value()).toBeNull();

    pending.next({});
    pending.complete();
    f.detectChanges();

    expect(f.componentInstance.uploading()).toBe(false);
    expect(f.componentInstance.value()).toBe('f1');
  });

  it('shows one error toast when the upload fails and leaves the value null', () => {
    http.uploadViaSasUri.mockReturnValueOnce(throwError(() => new Error('boom')));
    const f = setup();
    run(f, file('image/png', 100));
    f.detectChanges();
    f.detectChanges();

    expect(message.error).toHaveBeenCalledTimes(1);
    expect(message.error).toHaveBeenCalledWith('Încărcarea imaginii a eșuat');
    expect(f.componentInstance.uploading()).toBe(false);
    expect(f.componentInstance.value()).toBeNull();
  });
});
