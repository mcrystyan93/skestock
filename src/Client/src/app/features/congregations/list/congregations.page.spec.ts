import { TestBed } from '@angular/core/testing';
import { NzModalService } from 'ng-zorro-antd/modal';
import { of } from 'rxjs';
import { type CongregationDto } from '@ske/models';
import { CongregationDetailModal } from '@ske/shared/congregations';
import { realtimeGroups, SignalRGroupManagerStore } from '@ske/signalr';
import { CongregationListState } from '../services/congregation-list.store';
import { CongregationsPage } from './congregations.page';

describe('CongregationsPage', () => {
  const congregation: CongregationDto = {
    id: 'c1',
    name: 'Central',
    email: 'central@example.com',
    canReceiveStudents: true,
    isSmallCongregation: false,
    createdDate: '2026-01-01T00:00:00Z',
    lastModifiedDate: '2026-01-01T00:00:00Z'
  };

  let store: { deleteCongregation: ReturnType<typeof vi.fn>; reload: ReturnType<typeof vi.fn> };
  let modal: { create: ReturnType<typeof vi.fn>; confirm: ReturnType<typeof vi.fn> };
  let groups: { join: ReturnType<typeof vi.fn>; leave: ReturnType<typeof vi.fn> };

  // The page is never rendered here: its template is covered by the child component specs,
  // so the injected collaborators are plain mocks and only the page's own logic is exercised.
  function createPage() {
    store = { deleteCongregation: vi.fn(), reload: vi.fn() };
    modal = { create: vi.fn().mockReturnValue({ afterClose: of(null) }), confirm: vi.fn() };
    groups = { join: vi.fn(), leave: vi.fn() };

    TestBed.configureTestingModule({
      imports: [CongregationsPage],
      providers: [{ provide: SignalRGroupManagerStore, useValue: groups }]
    });
    TestBed.overrideComponent(CongregationsPage, {
      set: {
        providers: [
          { provide: CongregationListState, useValue: store },
          { provide: NzModalService, useValue: modal }
        ]
      }
    });

    return TestBed.createComponent(CongregationsPage).componentInstance;
  }

  it('opens an empty modal to add a congregation and reloads the list afterwards', () => {
    const page = createPage();

    page.onAdd();

    expect(modal.create).toHaveBeenCalledWith(
      expect.objectContaining({ nzContent: CongregationDetailModal, nzData: { congregation: null } })
    );
    expect(store.reload).toHaveBeenCalledTimes(1);
  });

  it('opens the modal with the selected congregation to edit it', () => {
    const page = createPage();

    page.onEdit(congregation);

    expect(modal.create).toHaveBeenCalledWith(expect.objectContaining({ nzData: { congregation } }));
  });

  it('asks for confirmation and deletes only after the user confirms', () => {
    const page = createPage();

    page.onDelete(congregation);

    expect(store.deleteCongregation).not.toHaveBeenCalled();
    const options = modal.confirm.mock.calls[0][0];
    expect(options.nzContent).toContain(congregation.name);

    options.nzOnOk();

    expect(store.deleteCongregation).toHaveBeenCalledWith(congregation);
  });

  it('joins the congregations realtime group on init and leaves it on destroy', () => {
    const page = createPage();

    page.ngOnInit();
    page.ngOnDestroy();

    expect(groups.join).toHaveBeenCalledWith(realtimeGroups.congregationsList);
    expect(groups.leave).toHaveBeenCalledWith(realtimeGroups.congregationsList);
  });
});
