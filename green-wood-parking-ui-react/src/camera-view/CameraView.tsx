import { useEffect, useRef, useState } from 'react';
import { Button } from '../components/Button/Button';
import { Icon } from '../components/Icon/Icon';
import { Modal } from '../components/Modal/Modal';
import { Spinner } from '../components/Spinner/Spinner';
import styles from './CameraView.module.css';

export interface CameraViewProps {
  /** Запрашивает кадр (используется при открытии диалога и кнопкой обновления). */
  fetchImage: () => Promise<Blob>;
  onClose: () => void;
}

export function CameraView({ fetchImage, onClose }: CameraViewProps) {
  const [imageUrl, setImageUrl] = useState<string | undefined>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const objectUrlRef = useRef<string | undefined>(undefined);

  useEffect(() => {
    let cancelled = false;

    fetchImage()
      .then((blob) => {
        if (cancelled) {
          return;
        }
        setImage(blob);
        setIsLoading(false);
      })
      .catch((err: unknown) => {
        if (cancelled) {
          return;
        }
        console.error(err);
        setError('Не удалось загрузить изображение');
        setIsLoading(false);
      });

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- fetch only the initial frame once on mount
  }, []);

  useEffect(() => {
    return () => {
      if (objectUrlRef.current) {
        URL.revokeObjectURL(objectUrlRef.current);
      }
    };
  }, []);

  function setImage(blob: Blob): void {
    const previousUrl = objectUrlRef.current;
    const nextUrl = URL.createObjectURL(blob);
    objectUrlRef.current = nextUrl;
    setImageUrl(nextUrl);

    if (previousUrl) {
      URL.revokeObjectURL(previousUrl);
    }
  }

  function handleRefresh(): void {
    if (isRefreshing || isLoading) {
      return;
    }

    setIsRefreshing(true);
    setError(null);

    fetchImage()
      .then((blob) => {
        setImage(blob);
        setIsRefreshing(false);
      })
      .catch((err: unknown) => {
        console.error(err);
        setError('Не удалось обновить изображение');
        setIsRefreshing(false);
      });
  }

  return (
    <Modal onClose={onClose} panelClassName={styles.panel}>
      <div className={styles.viewer}>
        {imageUrl && (
          <img
            className={styles.frame}
            src={imageUrl}
            alt="Parking Stream"
            role="button"
            tabIndex={0}
            onClick={onClose}
            onKeyDown={(event) => {
              if (event.key === 'Enter' || event.key === ' ') {
                onClose();
              }
            }}
          />
        )}

        {isLoading && (
          <div className={styles.loadingOverlay}>
            <Spinner diameter={48} />
          </div>
        )}

        <Button
          variant="icon"
          className={styles.refreshButton}
          disabled={isRefreshing || isLoading}
          onClick={(event) => {
            event.stopPropagation();
            handleRefresh();
          }}
          aria-label="Обновить изображение"
        >
          {isRefreshing ? <Spinner diameter={24} /> : <Icon name="refresh" />}
        </Button>

        {error && <div className={styles.errorBanner}>{error}</div>}
      </div>
    </Modal>
  );
}
