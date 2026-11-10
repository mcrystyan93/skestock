import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { ItemDto } from '@ske/models';
import { ItemsHttp } from '@ske/shared/items';
import { NzModalRef } from 'ng-zorro-antd/modal';
import { of, throwError } from 'rxjs';
import { ItemListState } from '../../services/item-list.store';
import { ItemMergeWizard } from './item-merge-wizard';

describe('ItemMergeWizard', () => {
  const a = createItem('item-1', 'Creion');
  const b = createItem('item-2', 'Creion HB');
  let http: { mergePreview: ReturnType<typeof vi.fn> };
  let store: { merge: ReturnType<typeof vi.fn>; clearMergeErrors: ReturnType<typeof vi.fn> };
  let modalRef: { close: ReturnType<typeof vi.fn>; updateConfig: ReturnType<typeof vi.fn> };

  function create() {
    const fixture = TestBed.createComponent(ItemMergeWizard);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    http = { mergePreview: vi.fn().mockReturnValue(of({ impacts: [{ kind: 'supplyListLines', relinked: 1, combined: 2 }] })) };
    store = { merge: vi.fn(), clearMergeErrors: vi.fn() };
    modalRef = { close: vi.fn(), updateConfig: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        { provide: NzModalRef, useValue: modalRef },
        { provide: ItemsHttp, useValue: http },
        {
          provide: ItemListState,
          useValue: {
            ...store,
            selectedItems: signal([a, b]),
            merging: signal(false),
            mergeProblemDetail: signal(null),
            mergeValidationErrors: signal(null)
          }
        }
      ]
    });
  });

  it('shows exactly three steps', () => {
    const fixture = create();

    expect((fixture.nativeElement as HTMLElement).querySelectorAll('nz-step').length).toBe(3);
  });

  it('cannot advance without a primary item', () => {
    const fixture = create();
    const wizard = fixture.componentInstance;

    wizard.next();

    expect(wizard.step()).toBe(0);
    expect(http.mergePreview).not.toHaveBeenCalled();
  });

  it('requests the preview with the chosen primary and all selected ids on step two', () => {
    const fixture = create();
    const wizard = fixture.componentInstance;

    wizard.primaryId.set('item-2');
    wizard.next();
    fixture.detectChanges();

    expect(wizard.step()).toBe(1);
    expect(http.mergePreview).toHaveBeenCalledWith({ primaryItemId: 'item-2', itemIds: ['item-1', 'item-2'] });
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Linii din liste de aprovizionare');
  });

  it.each([
    ['supplyListLines', 'Cantitățile se însumează'],
    ['itemLocationThresholds', 'Se păstrează setările articolului principal']
  ])('explains what happens to colliding %s rows', (kind, expected) => {
    http.mergePreview.mockReturnValue(of({ impacts: [{ kind, relinked: 0, combined: 2 }] }));
    const fixture = create();
    fixture.componentInstance.primaryId.set('item-1');

    fixture.componentInstance.next();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain(expected);
  });

  it('shows the error state and blocks advancing when the preview fails', () => {
    http.mergePreview.mockReturnValue(throwError(() => new Error('boom')));
    const fixture = create();
    const wizard = fixture.componentInstance;
    wizard.primaryId.set('item-1');

    wizard.next();
    fixture.detectChanges();
    wizard.next();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Previzualizarea nu a putut fi încărcată');
    expect(wizard.step()).toBe(1);
  });

  it('confirms through the store and closes the modal on success', () => {
    const fixture = create();
    const wizard = fixture.componentInstance;
    wizard.primaryId.set('item-1');
    wizard.next();
    fixture.detectChanges();
    wizard.next();

    wizard.confirm();

    expect(store.merge).toHaveBeenCalledOnce();
    const { request, onSuccess } = store.merge.mock.calls[0][0];
    expect(request).toEqual({ primaryItemId: 'item-1', itemIds: ['item-1', 'item-2'] });

    onSuccess();

    expect(modalRef.close).toHaveBeenCalledWith(true);
  });
});

function createItem(id: string, name: string): ItemDto {
  return {
    id,
    name,
    sku: 'CR-1',
    unit: 'bucată',
    locationThresholds: [],
    isPerishable: false,
    isActive: true,
    categoryId: 'category-1',
    categoryName: 'Papetărie',
    createdDate: '2026-01-01T00:00:00Z',
    lastModifiedDate: '2026-01-01T00:00:00Z'
  };
}
