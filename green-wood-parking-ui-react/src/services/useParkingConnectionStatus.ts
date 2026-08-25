import { useEffect, useState } from 'react';
import { parkingSignalRService } from './parkingSignalRService';

export function useParkingConnectionStatus(): boolean {
  const [isConnected, setIsConnected] = useState(parkingSignalRService.isConnected);

  useEffect(() => parkingSignalRService.onConnectedChange(setIsConnected), []);

  return isConnected;
}
