export const environment = {
  production: true,
  // Same-origin: the nginx container serves the SPA and reverse-proxies /api and /hubs
  // to the API container, so the browser uses relative paths (no hard-coded host).
  apiBaseUrl: '',
  signalRHubUrl: '/hubs',
  appName: 'Bengal TEX ERP',
  version: '1.0.0',
  defaultPageSize: 25,
  fingerprintJs: {
    apiKey: ''
  }
};