export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000',
  oidc: {
    authority: 'http://localhost:5000',
    client_id: 'ai-competency-ui',
    redirect_uri: 'http://localhost:4200/callback',
    post_logout_redirect_uri: 'http://localhost:4200/',
    response_type: 'code',
    scope: 'openid profile email roles', // Adjust scopes as needed
  }
};

