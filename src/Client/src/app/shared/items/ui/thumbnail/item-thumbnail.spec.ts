import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { TestBed } from '@angular/core/testing';
import { NzImageService } from 'ng-zorro-antd/image';
import { ItemThumbnail } from './item-thumbnail';

describe('ItemThumbnail', () => {
  let preview: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideNzIconsTesting(),
        provideNzIcons([{ name: 'icons:box', icon: '<svg viewBox="0 0 24 24"></svg>' }]),
      ],
    });
  });

  function render(imageFileId: string | null) {
    const fixture = TestBed.createComponent(ItemThumbnail);
    fixture.componentRef.setInput('itemId', 'item-1');
    fixture.componentRef.setInput('imageFileId', imageFileId);
    preview = vi
      .spyOn(fixture.debugElement.injector.get(NzImageService), 'preview')
      .mockReturnValue(undefined as never);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
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
});
