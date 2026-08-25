import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { CameraView } from './CameraView';

describe('CameraView', () => {
  let fetchImage: ReturnType<typeof vi.fn<() => Promise<Blob>>>;
  let onClose: ReturnType<typeof vi.fn<() => void>>;
  const initialFile = new Blob(['initial'], { type: 'image/jpeg' });
  const refreshedFile = new Blob(['refreshed'], { type: 'image/jpeg' });
  let objectUrlCounter = 0;

  beforeEach(() => {
    objectUrlCounter = 0;
    URL.createObjectURL = vi.fn().mockImplementation(() => `blob:http://localhost/${++objectUrlCounter}`);
    URL.revokeObjectURL = vi.fn();

    onClose = vi.fn<() => void>();
    fetchImage = vi
      .fn<() => Promise<Blob>>()
      .mockResolvedValueOnce(initialFile)
      .mockResolvedValue(refreshedFile);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('shows a loading indicator until the initial image arrives', () => {
    fetchImage.mockReset().mockReturnValue(new Promise<Blob>(() => {}));
    render(<CameraView fetchImage={fetchImage} onClose={onClose} />);

    expect(screen.getByRole('status', { name: 'Загрузка' })).toBeInTheDocument();
    expect(screen.queryByAltText('Parking Stream')).not.toBeInTheDocument();
  });

  it('fetches the initial image on mount', async () => {
    render(<CameraView fetchImage={fetchImage} onClose={onClose} />);

    await waitFor(() => expect(URL.createObjectURL).toHaveBeenCalledWith(initialFile));
    expect(fetchImage).toHaveBeenCalledTimes(1);
  });

  it('renders the image once the object url is available', async () => {
    render(<CameraView fetchImage={fetchImage} onClose={onClose} />);

    expect(await screen.findByAltText('Parking Stream')).toBeInTheDocument();
  });

  it('closes on image click', async () => {
    render(<CameraView fetchImage={fetchImage} onClose={onClose} />);

    fireEvent.click(await screen.findByAltText('Parking Stream'));

    expect(onClose).toHaveBeenCalled();
  });

  it('revokes the object url on unmount', async () => {
    const { unmount } = render(<CameraView fetchImage={fetchImage} onClose={onClose} />);
    await screen.findByAltText('Parking Stream');

    unmount();

    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:http://localhost/1');
  });

  it('fetches a new image and replaces the current one when refresh is triggered', async () => {
    render(<CameraView fetchImage={fetchImage} onClose={onClose} />);
    await screen.findByAltText('Parking Stream');

    fireEvent.click(screen.getByRole('button', { name: 'Обновить изображение' }));

    await waitFor(() => expect(URL.createObjectURL).toHaveBeenCalledWith(refreshedFile));
    expect(fetchImage).toHaveBeenCalledTimes(2);
  });

  it('revokes the previous object url after a successful refresh', async () => {
    render(<CameraView fetchImage={fetchImage} onClose={onClose} />);
    await screen.findByAltText('Parking Stream');

    fireEvent.click(screen.getByRole('button', { name: 'Обновить изображение' }));

    await waitFor(() => expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:http://localhost/1'));
  });

  it('disables the refresh button while a refresh is in flight', async () => {
    render(<CameraView fetchImage={fetchImage} onClose={onClose} />);
    await screen.findByAltText('Parking Stream');
    fetchImage.mockReturnValue(new Promise<Blob>(() => {}));

    fireEvent.click(screen.getByRole('button', { name: 'Обновить изображение' }));

    expect(screen.getByRole('button', { name: 'Обновить изображение' })).toBeDisabled();
  });

  it('does not start a second refresh while one is already in flight', async () => {
    render(<CameraView fetchImage={fetchImage} onClose={onClose} />);
    await screen.findByAltText('Parking Stream');
    fetchImage.mockReturnValue(new Promise<Blob>(() => {}));

    const button = screen.getByRole('button', { name: 'Обновить изображение' });
    fireEvent.click(button);
    fireEvent.click(button);

    expect(fetchImage).toHaveBeenCalledTimes(2);
  });

  it('surfaces an error message and keeps the previous image if refresh fails', async () => {
    render(<CameraView fetchImage={fetchImage} onClose={onClose} />);
    const image = await screen.findByAltText<HTMLImageElement>('Parking Stream');
    const srcBeforeRefresh = image.src;
    fetchImage.mockRejectedValueOnce(new Error('network error'));

    fireEvent.click(screen.getByRole('button', { name: 'Обновить изображение' }));

    expect(await screen.findByText('Не удалось обновить изображение')).toBeInTheDocument();
    expect(image.src).toBe(srcBeforeRefresh);
    expect(screen.getByRole('button', { name: 'Обновить изображение' })).not.toBeDisabled();
  });

  it('surfaces an error and stops loading if the initial fetch fails', async () => {
    fetchImage.mockReset().mockRejectedValue(new Error('network error'));
    render(<CameraView fetchImage={fetchImage} onClose={onClose} />);

    expect(await screen.findByText('Не удалось загрузить изображение')).toBeInTheDocument();
    expect(screen.queryByAltText('Parking Stream')).not.toBeInTheDocument();
    expect(screen.queryByRole('status', { name: 'Загрузка' })).not.toBeInTheDocument();
  });
});
