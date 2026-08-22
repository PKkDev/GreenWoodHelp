import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { Observable } from 'rxjs';

export interface CameraViewData {
  /** Запрашивает кадр (используется при открытии диалога и кнопкой обновления). */
  fetchImage: () => Observable<Blob>;
}

@Component({
  selector: 'app-camera-view',
  templateUrl: './camera-view.component.html',
  styleUrls: ['./camera-view.component.scss'],
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule]
})
export class CameraViewComponent implements OnInit, OnDestroy {

  private readonly dialogRef = inject(MatDialogRef<CameraViewComponent>);
  private readonly data: CameraViewData = inject(MAT_DIALOG_DATA);
  private readonly sanitizer = inject(DomSanitizer);

  public imagePath = signal<SafeUrl | undefined>(undefined);
  public isLoading = signal(true);
  public isRefreshing = signal(false);
  public error = signal<string | null>(null);

  private objectUrl: string | undefined;

  public ngOnInit(): void {
    this.data.fetchImage().subscribe({
      next: (blob) => {
        this.setImage(blob);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.error.set('Не удалось загрузить изображение');
        this.isLoading.set(false);
      }
    });
  }

  public ngOnDestroy(): void {
    this.revokeCurrentUrl();
  }

  public onClose(): void {
    this.dialogRef.close();
  }

  public onRefresh(): void {
    if (this.isRefreshing() || this.isLoading()) {
      return;
    }

    this.isRefreshing.set(true);
    this.error.set(null);

    this.data.fetchImage().subscribe({
      next: (blob) => {
        this.setImage(blob);
        this.isRefreshing.set(false);
      },
      error: (err) => {
        console.error(err);
        this.error.set('Не удалось обновить изображение');
        this.isRefreshing.set(false);
      }
    });
  }

  private setImage(file: Blob): void {
    const previousUrl = this.objectUrl;

    this.objectUrl = URL.createObjectURL(file);
    this.imagePath.set(this.sanitizer.bypassSecurityTrustUrl(this.objectUrl));

    if (previousUrl) {
      URL.revokeObjectURL(previousUrl);
    }
  }

  private revokeCurrentUrl(): void {
    if (this.objectUrl) {
      URL.revokeObjectURL(this.objectUrl);
      this.objectUrl = undefined;
    }
  }

}
