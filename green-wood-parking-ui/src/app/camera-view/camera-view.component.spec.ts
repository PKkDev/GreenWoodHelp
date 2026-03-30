import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { CameraViewComponent } from './camera-view.component';

describe('CameraViewComponent', () => {
  let component: CameraViewComponent;
  let fixture: ComponentFixture<CameraViewComponent>;
  let mockDialogRef: MatDialogRef<CameraViewComponent>;
  let mockSanitizer: DomSanitizer;
  const mockData = { file: new Blob(['test'], { type: 'image/jpeg' }) };

  beforeEach(async () => {
    mockDialogRef = {
      close: vi.fn()
    } as unknown as MatDialogRef<CameraViewComponent>;

    mockSanitizer = {
      bypassSecurityTrustUrl: vi.fn()
    } as unknown as DomSanitizer;

    await TestBed.configureTestingModule({
      imports: [CameraViewComponent],
      providers: [
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: mockData },
        { provide: DomSanitizer, useValue: mockSanitizer }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CameraViewComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('onClose', () => {
    it('should call dialogRef.close', () => {
      component.onClose();
      expect(mockDialogRef.close).toHaveBeenCalled();
    });
  });

  describe('ngOnInit', () => {
    it('should create safe URL from file data', () => {
      const unsafeUrl = 'blob:http://localhost:1234/test-uuid';
      const safeUrl = {} as SafeUrl;
      const createObjectURLSpy = vi.spyOn(URL, 'createObjectURL').mockReturnValue(unsafeUrl);
      vi.mocked(mockSanitizer.bypassSecurityTrustUrl).mockReturnValue(safeUrl);

      component.ngOnInit();

      expect(createObjectURLSpy).toHaveBeenCalledWith(mockData.file);
      expect(mockSanitizer.bypassSecurityTrustUrl).toHaveBeenCalledWith(unsafeUrl);
      expect(component.imagePath).toBe(safeUrl);
    });
  });

  describe('ngOnDestroy', () => {
    it('should revoke object URL when imagePath exists', () => {
      const mockUrl = 'blob:http://localhost:1234/test-uuid';
      component.imagePath = mockUrl as unknown as SafeUrl;
      const revokeObjectURLSpy = vi.spyOn(URL, 'revokeObjectURL');

      component.ngOnDestroy();

      expect(revokeObjectURLSpy).toHaveBeenCalledWith(mockUrl);
    });

    it('should not revoke object URL when imagePath is undefined', () => {
      component.imagePath = undefined;
      const revokeObjectURLSpy = vi.spyOn(URL, 'revokeObjectURL');

      component.ngOnDestroy();

      expect(revokeObjectURLSpy).not.toHaveBeenCalled();
    });
  });
});
