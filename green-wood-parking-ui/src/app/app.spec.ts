import { TestBed } from '@angular/core/testing';
import { App } from './app';

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
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });
});
