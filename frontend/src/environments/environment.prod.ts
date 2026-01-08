export const environment = {
  production: true,
  apiBaseUrl: 'https://api.yourdomain.com',
  apiConfig: {
    basePath: 'https://api.yourdomain.com',
    withCredentials: true,
  },
  auth: {
    clientId: 'angular-admin-app',
    clientSecret: '',
    tokenEndpoint: 'https://api.yourdomain.com/connect/token',
    authorizeEndpoint: 'https://api.yourdomain.com/connect/authorize',
    logoutEndpoint: 'https://api.yourdomain.com/connect/logout',
    scope: 'openid profile email roles',
    responseType: 'code',
    redirectUri: 'https://admin.yourdomain.com/auth/callback',
    postLogoutRedirectUri: 'https://admin.yourdomain.com',
  },
  tokenStorageKey: 'bec_access_token',
  refreshTokenStorageKey: 'bec_refresh_token',
};
