import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { of, Subject, throwError } from 'rxjs';
import { CameraViewComponent, CameraViewData } from './camera-view.component';

describe('CameraViewComponent', () => {
  let component: CameraViewComponent;
  let fixture: ComponentFixture<CameraViewComponent>;
  let mockDialogRef: MatDialogRef<CameraViewComponent>;
  let fetchImage: ReturnType<typeof vi.fn>;
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

    fetchImage = vi.fn().mockReturnValue(of(refreshedFile));
  });

  it('should create', () => {
    createComponent({ file: initialFile, fetchImage });
    expect(component).toBeTruthy();
  });

  it('should not render the image before ngOnInit runs', () => {
    createComponent({ file: initialFile, fetchImage });

    const img: HTMLImageElement | null = fixture.nativeElement.querySelector('img.parking-frame');
    expect(img).toBeFalsy();
  });

  it('should create an object URL from the initial file on init', () => {
    createComponent({ file: initialFile, fetchImage });
    fixture.detectChanges();

    expect(URL.createObjectURL).toHaveBeenCalledWith(initialFile);
    expect(component.imagePath()).toBeTruthy();
  });

  it('should render the image once the safe url is available', () => {
    createComponent({ file: initialFile, fetchImage });
    fixture.detectChanges();

    const img: HTMLImageElement | null = fixture.nativeElement.querySelector('img.parking-frame');
    expect(img).toBeTruthy();
  });

  it('should close the dialog when the image is clicked', () => {
    createComponent({ file: initialFile, fetchImage });
    fixture.detectChanges();

    const img: HTMLImageElement = fixture.nativeElement.querySelector('img.parking-frame');
    img.click();

    expect(mockDialogRef.close).toHaveBeenCalled();
  });

  it('should close the dialog when onClose is called directly', () => {
    createComponent({ file: initialFile, fetchImage });

    component.onClose();

    expect(mockDialogRef.close).toHaveBeenCalled();
  });

  it('should revoke the object URL on destroy', () => {
    createComponent({ file: initialFile, fetchImage });
    fixture.detectChanges();

    const firstUrl = 'blob:http://localhost/1';
    component.ngOnDestroy();

    expect(URL.revokeObjectURL).toHaveBeenCalledWith(firstUrl);
  });

  it('should fetch a new image and replace the current one when refresh is triggered', () => {
    createComponent({ file: initialFile, fetchImage });
    fixture.detectChanges();

    component.onRefresh();

    expect(fetchImage).toHaveBeenCalled();
    expect(URL.createObjectURL).toHaveBeenCalledWith(refreshedFile);
    expect(component.isRefreshing()).toBe(false);
    expect(component.error()).toBeNull();
  });

  it('should revoke the previous object URL after a successful refresh', () => {
    createComponent({ file: initialFile, fetchImage });
    fixture.detectChanges();

    const firstUrl = 'blob:http://localhost/1';
    component.onRefresh();

    expect(URL.revokeObjectURL).toHaveBeenCalledWith(firstUrl);
  });

  it('should disable the refresh button while a refresh is in flight', () => {
    fetchImage.mockReturnValue(new Subject());
    createComponent({ file: initialFile, fetchImage });
    fixture.detectChanges();

    component.onRefresh();
    fixture.detectChanges();

    expect(component.isRefreshing()).toBe(true);
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('.refresh-button');
    expect(button.disabled).toBe(true);
  });

  it('should not start a second refresh while one is already in flight', () => {
    fetchImage.mockReturnValue(new Subject());
    createComponent({ file: initialFile, fetchImage });
    fixture.detectChanges();

    component.onRefresh();
    component.onRefresh();

    expect(fetchImage).toHaveBeenCalledTimes(1);
  });

  it('should surface an error message and keep the previous image if refresh fails', () => {
    fetchImage.mockReturnValue(throwError(() => new Error('network error')));
    createComponent({ file: initialFile, fetchImage });
    fixture.detectChanges();

    const pathBeforeRefresh = component.imagePath();
    component.onRefresh();

    expect(component.isRefreshing()).toBe(false);
    expect(component.error()).toBeTruthy();
    expect(component.imagePath()).toBe(pathBeforeRefresh);
  });
});
