import * as Sentry from '@sentry/react';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import App from './App.tsx';
import './index.css';

Sentry.init({
  dsn: '<SENTRY_DSN>',
  integrations: [Sentry.browserTracingIntegration(), Sentry.replayIntegration()],
  tracesSampleRate: 1.0,
  tracePropagationTargets: ['localhost', /^https:\/\/custplace\.ru\/api/],
  replaysSessionSampleRate: 0.1,
  replaysOnErrorSampleRate: 1.0,
  enableLogs: true,
  environment: import.meta.env.DEV ? 'development' : 'production',
  enabled: !import.meta.env.DEV,
});

const rootElement = document.getElementById('root');
if (!rootElement) {
  throw new Error('Root element not found');
}

createRoot(rootElement).render(
  <StrictMode>
    <Sentry.ErrorBoundary fallback={<p>Что-то пошло не так. Попробуйте обновить страницу.</p>}>
      <App />
    </Sentry.ErrorBoundary>
  </StrictMode>,
);
