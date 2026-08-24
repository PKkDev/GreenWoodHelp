import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Observable, of, Subject, throwError } from 'rxjs';
import { CameraViewComponent, CameraViewData } from './camera-view.component';

describe('CameraViewComponent', () => {
  let component: CameraViewComponent;
  let fixture: ComponentFixture<CameraViewComponent>;
  let mockDialogRef: MatDialogRef<CameraViewComponent>;
  let fetchImage: ReturnType<typeof vi.fn<() => Observable<Blob>>>;
  const initialFile = new Blob(['initial'], { type: 'image/jpeg' });
  const refreshedFile = new Blob(['refreshed'], { type: 'image/jpeg' });
  let objectUrlCounter = 0;

  function createComponent(data: CameraViewData): void {
    TestBed.configureTestingModule({
      imports: [CameraViewComponent],
      providers: [
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: data }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CameraViewComponent);
    component = fixture.componentInstance;
  }

  beforeEach(() => {
    objectUrlCounter = 0;
    URL.createObjectURL = vi.fn().mockImplementation(() => `blob:http://localhost/${++objectUrlCounter}`);
    URL.revokeObjectURL = vi.fn();

    mockDialogRef = {
      close: vi.fn()
    } as unknown as MatDialogRef<CameraViewComponent>;

    fetchImage = vi.fn<() => Observable<Blob>>()
      .mockReturnValueOnce(of(initialFile))
      .mockReturnValue(of(refreshedFile));
  });

  it('should create', () => {
    createComponent({ fetchImage });
    expect(component).toBeTruthy();
  });

  it('should not render the image before ngOnInit runs', () => {
    createComponent({ fetchImage });

    const img: HTMLImageElement | null = fixture.nativeElement.querySelector('img.parking-frame');
    expect(img).toBeFalsy();
  });

  it('should show a loading indicator until the initial image arrives', () => {
    fetchImage.mockReset().mockReturnValue(new Subject());
    createComponent({ fetchImage });
    fixture.detectChanges();

    expect(component.isLoading()).toBe(true);
    const img: HTMLImageElement | null = fixture.nativeElement.querySelector('img.parking-frame');
    expect(img).toBeFalsy();
    const overlay = fixture.nativeElement.querySelector('.loading-overlay');
    expect(overlay).toBeTruthy();
  });

  it('should fetch the initial image on init', () => {
    createComponent({ fetchImage });
    fixture.detectChanges();

    expect(fetchImage).toHaveBeenCalledTimes(1);
    expect(URL.createObjectURL).toHaveBeenCalledWith(initialFile);
    expect(component.imagePath()).toBeTruthy();
    expect(component.isLoading()).toBe(false);
  });

  it('should render the image once the safe url is available', () => {
    createComponent({ fetchImage });
    fixture.detectChanges();

    const img: HTMLImageElement | null = fixture.nativeElement.querySelector('img.parking-frame');
    expect(img).toBeTruthy();
  });

  it('should close the dialog when the image is clicked', () => {
    createComponent({ fetchImage });
    fixture.detectChanges();

    const img: HTMLImageElement = fixture.nativeElement.querySelector('img.parking-frame');
    img.click();

    expect(mockDialogRef.close).toHaveBeenCalled();
  });

  it('should close the dialog when onClose is called directly', () => {
    createComponent({ fetchImage });

    component.onClose();

    expect(mockDialogRef.close).toHaveBeenCalled();
  });

  it('should revoke the object URL on destroy', () => {
    createComponent({ fetchImage });
    fixture.detectChanges();

    const firstUrl = 'blob:http://localhost/1';
    component.ngOnDestroy();

    expect(URL.revokeObjectURL).toHaveBeenCalledWith(firstUrl);
  });

  it('should fetch a new image and replace the current one when refresh is triggered', () => {
    createComponent({ fetchImage });
    fixture.detectChanges();

    component.onRefresh();

    expect(fetchImage).toHaveBeenCalledTimes(2);
    expect(URL.createObjectURL).toHaveBeenCalledWith(refreshedFile);
    expect(component.isRefreshing()).toBe(false);
    expect(component.error()).toBeNull();
  });

  it('should revoke the previous object URL after a successful refresh', () => {
    createComponent({ fetchImage });
    fixture.detectChanges();

    const firstUrl = 'blob:http://localhost/1';
    component.onRefresh();

    expect(URL.revokeObjectURL).toHaveBeenCalledWith(firstUrl);
  });

  it('should disable the refresh button while a refresh is in flight', () => {
    fetchImage.mockReset().mockReturnValueOnce(of(initialFile)).mockReturnValue(new Subject());
    createComponent({ fetchImage });
    fixture.detectChanges();

    component.onRefresh();
    fixture.detectChanges();

    expect(component.isRefreshing()).toBe(true);
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('.refresh-button');
    expect(button.disabled).toBe(true);
  });

  it('should not start a second refresh while one is already in flight', () => {
    fetchImage.mockReset().mockReturnValueOnce(of(initialFile)).mockReturnValue(new Subject());
    createComponent({ fetchImage });
    fixture.detectChanges();

    component.onRefresh();
    component.onRefresh();

    expect(fetchImage).toHaveBeenCalledTimes(2);
  });

  it('should surface an error message and keep the previous image if refresh fails', () => {
    fetchImage.mockReset().mockReturnValueOnce(of(initialFile)).mockReturnValue(throwError(() => new Error('network error')));
    createComponent({ fetchImage });
    fixture.detectChanges();

    const pathBeforeRefresh = component.imagePath();
    component.onRefresh();

    expect(component.isRefreshing()).toBe(false);
    expect(component.error()).toBeTruthy();
    expect(component.imagePath()).toBe(pathBeforeRefresh);
  });

  it('should surface an error and stop loading if the initial fetch fails', () => {
    fetchImage.mockReset().mockReturnValue(throwError(() => new Error('network error')));
    createComponent({ fetchImage });
    fixture.detectChanges();

    expect(component.isLoading()).toBe(false);
    expect(component.error()).toBeTruthy();
    expect(component.imagePath()).toBeUndefined();
  });
});
