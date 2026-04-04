import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { BASE_URL } from './app.config';

describe('App', () => {

  beforeEach(() => {
    (window as any).ymaps3 = {
      ready: Promise.resolve(),
      YMap: class { },
      YMapDefaultSchemeLayer: class { },
      YMapDefaultFeaturesLayer: class { },
      import: vi.fn().mockResolvedValue({})
    };

    TestBed.configureTestingModule({
      imports: [App],
      providers: [
        { provide: BASE_URL, useValue: 'https://localhost:7196' }
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });
});
