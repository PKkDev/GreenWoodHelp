import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { DomSanitizer } from '@angular/platform-browser';
import { CameraViewComponent } from './camera-view.component';

describe('CameraViewComponent', () => {
  let component: CameraViewComponent;
  let fixture: ComponentFixture<CameraViewComponent>;
  let mockDialogRef: MatDialogRef<CameraViewComponent>;
  let mockSanitizer: DomSanitizer;
  const mockData = { file: new Blob(['test'], { type: 'image/jpeg' }) };

  beforeEach(() => {
    mockDialogRef = {
      close: vi.fn()
    } as unknown as MatDialogRef<CameraViewComponent>;

    mockSanitizer = {
      bypassSecurityTrustUrl: vi.fn()
    } as unknown as DomSanitizer;

    TestBed.configureTestingModule({
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
});
