import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { CameraViewComponent } from './camera-view.component';

describe('CameraViewComponent', () => {
  let component: CameraViewComponent;
  let fixture: ComponentFixture<CameraViewComponent>;
  let mockDialogRef: MatDialogRef<CameraViewComponent>;
  const mockData = { file: new Blob(['test'], { type: 'image/jpeg' }) };
  const fakeObjectUrl = 'blob:http://localhost/fake-url';

  beforeEach(() => {
    URL.createObjectURL = vi.fn().mockReturnValue(fakeObjectUrl);
    URL.revokeObjectURL = vi.fn();

    mockDialogRef = {
      close: vi.fn()
    } as unknown as MatDialogRef<CameraViewComponent>;

    TestBed.configureTestingModule({
      imports: [CameraViewComponent],
      providers: [
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: mockData }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CameraViewComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should not render the image before ngOnInit runs', () => {
    const img: HTMLImageElement | null = fixture.nativeElement.querySelector('img.parking-frame');
    expect(img).toBeFalsy();
  });

  it('should create an object URL from the provided file on init', () => {
    fixture.detectChanges();

    expect(URL.createObjectURL).toHaveBeenCalledWith(mockData.file);
    expect(component.imagePath()).toBeTruthy();
  });

  it('should render the image once the safe url is available', () => {
    fixture.detectChanges();

    const img: HTMLImageElement | null = fixture.nativeElement.querySelector('img.parking-frame');
    expect(img).toBeTruthy();
  });

  it('should close the dialog when the image is clicked', () => {
    fixture.detectChanges();

    const img: HTMLImageElement = fixture.nativeElement.querySelector('img.parking-frame');
    img.click();

    expect(mockDialogRef.close).toHaveBeenCalled();
  });

  it('should close the dialog when onClose is called directly', () => {
    component.onClose();

    expect(mockDialogRef.close).toHaveBeenCalled();
  });

  it('should revoke the object URL on destroy', () => {
    fixture.detectChanges();

    component.ngOnDestroy();

    expect(URL.revokeObjectURL).toHaveBeenCalledWith(fakeObjectUrl);
  });
});
