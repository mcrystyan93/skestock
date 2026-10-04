import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { RoomConfigurationDto } from '@ske/models';
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
