import { TestBed } from '@angular/core/testing';
import { SupplyListsHttp } from '@ske/shared/supply-lists';
import { NZ_MODAL_DATA, NzModalRef } from 'ng-zorro-antd/modal';
import { of, throwError } from 'rxjs';
import { OrderListAddSupplyListsModal } from './order-list-add-supply-lists-modal';

describe('OrderListAddSupplyListsModal', () => {
  const setup = (getById: () => unknown, addedIds: string[] = []) => {
    const close = vi.fn();

    TestBed.configureTestingModule({
      providers: [
        { provide: NzModalRef, useValue: { close } },
        { provide: NZ_MODAL_DATA, useValue: { addedIds } },
        {
          provide: SupplyListsHttp,
          useValue: {
            getAllActive: () =>
              of([
                { id: 'l1', name: 'L1', lineCount: 1 },
                { id: 'l2', name: 'L2', lineCount: 1 },
              ]),
            getById,
          },
        },
      ],
    });

    const modal = TestBed.runInInjectionContext(
      () => new OrderListAddSupplyListsModal(),
    ) as unknown as {
      lists(): unknown[];
      selectedIds: { set(ids: string[]): void };
      failed(): string | null;
      add(): void;
    };

    return { modal, close };
  };

  it('closes with the merged lines of the selected lists', () => {
    const list = {
      id: 'l1',
      name: 'L1',
      lines: [{ itemId: 'a', itemName: 'A', itemSku: null, quantity: 2, unit: 'buc', notes: null }],
    };
    const { modal, close } = setup(() => of(list));

    expect(modal.lists()).toHaveLength(2);
    modal.selectedIds.set(['l1']);
    modal.add();

    expect(close).toHaveBeenCalledWith({
      listIds: ['l1'],
      lines: [expect.objectContaining({ itemId: 'a', quantity: 2 })],
    });
  });

  it('shows an error and stays open when a list cannot be fetched', () => {
    const { modal, close } = setup(() => throwError(() => new Error('x')));

    modal.selectedIds.set(['l1']);
    modal.add();

    expect(modal.failed()).toContain('nu au putut fi adăugate');
    expect(close).not.toHaveBeenCalled();
  });

  it('hides lists already added to the order', () => {
    const { modal } = setup(() => of(null), ['l1']);

    expect(modal.lists()).toEqual([expect.objectContaining({ id: 'l2' })]);
  });
});
