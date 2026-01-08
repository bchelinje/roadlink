// Production environment for Vercel deployment
// Update the API_BASE_URL in Vercel environment variables
export const environment = {
  production: true,
  // Will be replaced by Vercel's environment variable
  apiBaseUrl: 'RAILWAY_API_URL_PLACEHOLDER',
  apiConfig: {
    basePath: 'RAILWAY_API_URL_PLACEHOLDER',
    withCredentials: true,
  },
  auth: {
    clientId: 'angular-admin-app',
    clientSecret: '',
    tokenEndpoint: 'RAILWAY_API_URL_PLACEHOLDER/connect/token',
    authorizeEndpoint: 'RAILWAY_API_URL_PLACEHOLDER/connect/authorize',
    logoutEndpoint: 'RAILWAY_API_URL_PLACEHOLDER/connect/logout',
    scope: 'openid profile email roles',
    responseType: 'code',
    redirectUri: 'VERCEL_URL_PLACEHOLDER/auth/callback',
    postLogoutRedirectUri: 'VERCEL_URL_PLACEHOLDER',
    userinfoEndpoint: 'RAILWAY_API_URL_PLACEHOLDER/connect/userinfo',
    authority: 'RAILWAY_API_URL_PLACEHOLDER',
  },
  tokenStorageKey: 'bec_access_token',
  refreshTokenStorageKey: 'bec_refresh_token',
};
