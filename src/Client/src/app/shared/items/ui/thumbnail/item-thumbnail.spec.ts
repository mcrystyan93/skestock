import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { TestBed } from '@angular/core/testing';
import { ItemThumbnail } from './item-thumbnail';

describe('ItemThumbnail', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [
        provideNzIconsTesting(),
        provideNzIcons([{ name: 'icons:box', icon: '<svg viewBox="0 0 24 24"></svg>' }]),
      ],
    }),
  );

  function render(imageFileId: string | null) {
    const fixture = TestBed.createComponent(ItemThumbnail);
    fixture.componentRef.setInput('itemId', 'item-1');
    fixture.componentRef.setInput('imageFileId', imageFileId);
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
});
