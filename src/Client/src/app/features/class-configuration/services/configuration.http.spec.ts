import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { InvitationSplitDto, RoomConfigurationDto } from '@ske/models';
import { ConfigurationHttp } from './configuration.http';

describe('ConfigurationHttp room configuration', () => {
  let service: ConfigurationHttp;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ConfigurationHttp);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTestingController.verify());

  it('loads room seat counts from the configuration endpoint', () => {
    let response: RoomConfigurationDto | undefined;
    service.getRoomConfiguration().subscribe((result) => (response = result));

    const request = httpTestingController.expectOne('/api/ClassConfiguration/rooms');
    expect(request.request.method).toBe('GET');
    request.flush({ room4SeatCount: 100, room2SeatCount: 20, room6SeatCount: 30 });

    expect(response).toEqual({ room4SeatCount: 100, room2SeatCount: 20, room6SeatCount: 30 });
  });

  it('saves all room seat counts to the configuration endpoint', () => {
    const configuration = { room4SeatCount: 200, room2SeatCount: 0, room6SeatCount: 20 };
    service.saveRoomConfiguration(configuration).subscribe();

    const request = httpTestingController.expectOne('/api/ClassConfiguration/rooms');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(configuration);
    request.flush(configuration);
  });
});

describe('ConfigurationHttp invitation split', () => {
  let service: ConfigurationHttp;
  let httpTestingController: HttpTestingController;
  const split = {
    studentInvitationCount: 10,
    instructorInvitationCount: 5,
    openingPrayerInvitationCount: 2,
    experienceInvitationCount: 3,
    changeInvitationCount: 4,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ConfigurationHttp);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTestingController.verify());

  it('loads the split from the configuration endpoint', () => {
    let response: InvitationSplitDto | undefined;
    service.getInvitationSplit().subscribe((result) => (response = result));

    const request = httpTestingController.expectOne('/api/ClassConfiguration/invitation-split');
    expect(request.request.method).toBe('GET');
    request.flush(split);

    expect(response).toEqual(split);
  });

  it('saves the split to the configuration endpoint', () => {
    service.saveInvitationSplit(split).subscribe();

    const request = httpTestingController.expectOne('/api/ClassConfiguration/invitation-split');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(split);
    request.flush(split);
  });
});

describe('ConfigurationHttp purchase statistics', () => {
  it('posts to the materialize endpoint', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const service = TestBed.inject(ConfigurationHttp);
    const httpTestingController = TestBed.inject(HttpTestingController);

    service.materializePurchaseStatistics().subscribe();

    const request = httpTestingController.expectOne('/api/statistics/purchases/materialize');
    expect(request.request.method).toBe('POST');
    request.flush(42);
    httpTestingController.verify();
  });
});
