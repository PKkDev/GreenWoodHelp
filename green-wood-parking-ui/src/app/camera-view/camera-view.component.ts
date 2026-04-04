import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';

@Component({
  selector: 'app-camera-view',
  templateUrl: './camera-view.component.html',
  styleUrls: ['./camera-view.component.scss'],
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, MatDialogModule, MatButtonModule]
})
export class CameraViewComponent implements OnInit, OnDestroy {

  private readonly dialogRef = inject(MatDialogRef<CameraViewComponent>);
  private readonly data: { file: Blob } = inject(MAT_DIALOG_DATA);

  private sanitizer = inject(DomSanitizer);
  public imagePath = signal<SafeUrl | undefined>(undefined);

  public ngOnInit(): void {
    const unsafeUrl = URL.createObjectURL(this.data.file);
    this.imagePath.set(this.sanitizer.bypassSecurityTrustUrl(unsafeUrl));
  }

  public ngOnDestroy(): void {
    if (this.imagePath) {
      URL.revokeObjectURL(this.imagePath() as string);
    }
  }

  public onClose(): void {
    this.dialogRef.close();
  }

}
