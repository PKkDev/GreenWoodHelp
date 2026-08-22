import { isDevMode } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import * as Sentry from '@sentry/angular';
import { App } from './app/app';
import { appConfig } from './app/app.config';

Sentry.init({
  dsn: 'https://d5ec3f18caa8a1a8685832155ccaad0e@o4511956458864640.ingest.de.sentry.io/4511956474069072',
  dataCollection: {
    // To disable sending user data and HTTP bodies, uncomment the lines below. For more info visit:
    // https://docs.sentry.io/platforms/javascript/guides/angular/configuration/options/#dataCollection
    // userInfo: false,
    // httpBodies: []
  },
  integrations: [
    Sentry.browserTracingIntegration(),
    Sentry.replayIntegration()
  ],
  // Tracing
  tracesSampleRate: 1.0, //  Capture 100% of the transactions
  // Set 'tracePropagationTargets' to control for which URLs distributed tracing should be enabled
  tracePropagationTargets: ["localhost", /^https:\/\/yourserver\.io\/api/],
  // Session Replay
  replaysSessionSampleRate: 0.1, // This sets the sample rate at 10%. You may want to change it to 100% while in development and then sample at a lower rate in production.
  replaysOnErrorSampleRate: 1.0, // If you're not already sampling the entire session, change the sample rate to 100% when sampling sessions where errors occur.,
  // Enable sending logs to Sentry
  enableLogs: true,

  environment: isDevMode() ? 'development' : 'production',
  enabled: !isDevMode(),
});

bootstrapApplication(App, appConfig).catch((err) => console.error(err));
