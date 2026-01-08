export const environment = {
  production: false,
  apiBaseUrl: 'https://localhost:7172',  // ✅ Use HTTPS port
  apiConfig: {
    basePath: 'https://localhost:7172',  // ✅ Use HTTPS port
    withCredentials: false,
  },
  auth: {
    clientId: 'angular-admin-app',
    clientSecret: '',
    tokenEndpoint: 'https://localhost:7172/connect/token',  // ✅ HTTPS
    authorizeEndpoint: 'https://localhost:7172/connect/authorize',
    logoutEndpoint: 'https://localhost:7172/connect/logout',
    scope: 'openid profile email roles',
    responseType: 'code',
    redirectUri: 'http://localhost:4200/auth/callback',
    postLogoutRedirectUri: 'http://localhost:4200',
    userinfoEndpoint: 'https://localhost:7172/connect/userinfo',  // ← ADD THIS
    authority: 'https://localhost:7172',
  },
  tokenStorageKey: 'bec_access_token',
  refreshTokenStorageKey: 'bec_refresh_token',
};

//
// // src/environments/environment.ts
// export const environment = {
//   production: false,
//   apiUrl: 'https://localhost:7172',
//   auth: {
//     authority: 'https://localhost:7172',
//     clientId: 'angular-admin-app',
//     clientSecret: '', // Add if required
//     scope: 'openid profile email roles offline_access',
//     tokenEndpoint: 'https://localhost:7172/connect/token',
//     userinfoEndpoint: 'https://localhost:7172/connect/userinfo',  // ← ADD THIS
//     redirectUri: 'http://localhost:4200/auth/callback',
//     responseType: 'code'
//   }
// };
