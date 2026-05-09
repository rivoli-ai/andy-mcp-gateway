export const environment = {
  production: false,
  apiUrl: 'http://localhost:5080',
  
  // Azure AD Authentication Configuration
  azureAd: {
    clientId: '5c286802-9e81-4aa6-abd3-f083ad57c5dc',
    tenantId: '1335991b-55a1-47b7-a4dd-177f429f0719',
    authority: 'https://login.microsoftonline.com/1335991b-55a1-47b7-a4dd-177f429f0719',
    redirectUri: 'http://localhost:4200',
    postLogoutRedirectUri: 'http://localhost:4200',
    
    // API Scopes
    scopes: {
      userRead: 'user.read',
      apiAccess: 'api://andy-back/Api.Access'
    },
    
    // API Audience (matches the actual token audience)
    apiAudience: '55bd6449-4514-4186-81d9-59002182bc7f',
    
    // Protected Resources
    protectedResources: {
      api: {
        endpoint: 'http://localhost:5080',
        scopes: ['api://andy-back/Api.Access']
      }
    }
  }
};