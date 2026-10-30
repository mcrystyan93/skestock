import { of, throwError } from 'rxjs';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { TestBed } from '@angular/core/testing';
import { NzImageService } from 'ng-zorro-antd/image';
import { NzMessageService } from 'ng-zorro-antd/message';
import { StorageHttp } from '@ske/shared/storage';
import { ItemThumbnail } from './item-thumbnail';

describe('ItemThumbnail', () => {
  let preview: ReturnType<typeof vi.spyOn>;
  const http = {
    requestUpload: vi.fn(() => of({ fileId: 'f1', uploadUrl: 'http://blob' })),
    uploadViaSasUri: vi.fn((_file: unknown, _uploadResult: unknown) => of({})),
    confirmUpload: vi.fn(() => of({ fileId: 'f1' })),
  };
  const message = { error: vi.fn() };

  beforeEach(() => {
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      providers: [
        provideNzIconsTesting(),
        provideNzIcons([{ name: 'icons:box', icon: '<svg viewBox="0 0 24 24"></svg>' }]),
        { provide: StorageHttp, useValue: http },
        { provide: NzMessageService, useValue: message },
      ],
    });
  });

  function create(imageFileId: string | null, uploadable = false) {
    const fixture = TestBed.createComponent(ItemThumbnail);
    fixture.componentRef.setInput('itemId', 'item-1');
    fixture.componentRef.setInput('imageFileId', imageFileId);
    fixture.componentRef.setInput('uploadable', uploadable);
    preview = vi
      .spyOn(fixture.debugElement.injector.get(NzImageService), 'preview')
      .mockReturnValue(undefined as never);
    fixture.detectChanges();
    return fixture;
  }

  function render(imageFileId: string | null) {
    return create(imageFileId).nativeElement as HTMLElement;
  }

  function pick(fixture: ReturnType<typeof create>, file: File) {
    const picker = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(
      'input[type="file"]',
    );
    Object.defineProperty(picker, 'files', { value: [file] });
    picker?.dispatchEvent(new Event('change'));
    fixture.detectChanges();
  }

  it('shows the placeholder icon when there is no image', () => {
    const element = render(null);

    expect(element.querySelector('img')).toBeNull();
    expect(element.querySelector('nz-icon')).not.toBeNull();
  });

  it('renders the thumb url with the version query', () => {
    const img = render('file-9').querySelector('img');

    expect(img?.getAttribute('src')).toBe('/api/Items/item-1/image?size=thumb&v=file-9');
    expect(img?.getAttribute('loading')).toBe('lazy');
  });

  it('opens the full-size image in the preview when the thumbnail is clicked', () => {
    const element = render('file-9');
    const rowClick = vi.fn();
    element.addEventListener('click', rowClick);

    element.querySelector('button')?.click();

    expect(preview).toHaveBeenCalledWith([
      { src: '/api/Items/item-1/image?size=full&v=file-9', alt: 'Imagine articol' },
    ]);
    expect(rowClick).not.toHaveBeenCalled();
  });

  it('does not offer a preview button for the placeholder', () => {
    expect(render(null).querySelector('button')).toBeNull();
  });

  it('keeps keyboard presses from reaching the surrounding row or card', () => {
    const element = render('file-9');
    const rowKeydown = vi.fn();
    element.addEventListener('keydown', rowKeydown);

    for (const key of ['Enter', ' ']) {
      element
        .querySelector('button')
        ?.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true }));
    }

    expect(rowKeydown).not.toHaveBeenCalled();
  });

  it('offers an upload button for the placeholder only when uploadable', () => {
    expect(create(null, true).nativeElement.querySelector('button')?.getAttribute('aria-label')).toBe(
      'Încarcă imaginea',
    );
    expect(create(null, false).nativeElement.querySelector('input[type="file"]')).toBeNull();
  });

  it('uploads the picked image and emits its file id', () => {
    const fixture = create(null, true);
    const uploaded = vi.fn();
    fixture.componentInstance.imageUploaded.subscribe(uploaded);

    pick(fixture, new File(['x'], 'new.png', { type: 'image/png' }));

    expect(http.requestUpload).toHaveBeenCalledWith({ fileName: 'new.png', contentType: 'image/png' });
    expect(http.uploadViaSasUri.mock.calls.at(-1)?.[0]).toBeInstanceOf(File);
    expect(uploaded).toHaveBeenCalledWith('f1');
  });

  it('keeps the file picker click from reaching the surrounding row', () => {
    const element = create(null, true).nativeElement as HTMLElement;
    const rowClick = vi.fn();
    element.addEventListener('click', rowClick);

    element.querySelector<HTMLButtonElement>('button')?.click();

    expect(rowClick).not.toHaveBeenCalled();
  });

  it('rejects an unsupported file without uploading', () => {
    const fixture = create(null, true);

    pick(fixture, new File(['x'], 'doc.pdf', { type: 'application/pdf' }));

    expect(message.error).toHaveBeenCalledWith('Format neacceptat');
    expect(http.requestUpload).not.toHaveBeenCalled();
  });

  it('shows one error and emits nothing when the upload fails', () => {
    http.uploadViaSasUri.mockReturnValueOnce(throwError(() => new Error('boom')));
    const fixture = create(null, true);
    const uploaded = vi.fn();
    fixture.componentInstance.imageUploaded.subscribe(uploaded);

    pick(fixture, new File(['x'], 'new.png', { type: 'image/png' }));

    expect(message.error).toHaveBeenCalledTimes(1);
    expect(message.error).toHaveBeenCalledWith('Încărcarea imaginii a eșuat');
    expect(uploaded).not.toHaveBeenCalled();
  });
});
