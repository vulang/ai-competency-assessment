import { Injectable, signal, inject } from '@angular/core';
import { Router } from '@angular/router';
import { User } from '../../models/user.model';
import { UserManager, User as OidcUser } from 'oidc-client-ts';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private userManager: UserManager;
  
  currentUser = signal<User | null>(null);

  constructor() {
    this.userManager = new UserManager(environment.oidc);
    
    // Synchronously restore ROPC login state so guards passing immediately on reload
    const savedToken = sessionStorage.getItem('access_token');
    const savedIdToken = sessionStorage.getItem('id_token');
    if (savedToken) {
      this.loginWithToken(savedToken, savedIdToken || undefined);
    }

    // Check if user is already logged in from OIDC storage
    this.userManager.getUser().then(user => {
      if (user && !user.expired) {
        this.setUser(user);
      }
    });

    this.userManager.events.addUserLoaded(user => this.setUser(user));
    this.userManager.events.addUserUnloaded(() => {
      this.currentUser.set(null);
    });
  }

  login() {
    return this.userManager.signinRedirect();
  }

  async completeLogin() {
    const user = await this.userManager.signinRedirectCallback();
    this.setUser(user);
    return user;
  }

  logout() {
    sessionStorage.removeItem('access_token');
    sessionStorage.removeItem('id_token');
    this.currentUser.set(null);
    this.userManager.signoutRedirect();
  }

  isLoggedIn() {
    return !!this.currentUser();
  }

  async checkAuthAsync(): Promise<boolean> {
    if (this.isLoggedIn()) return true;
    
    try {
      const user = await this.userManager.getUser();
      if (user && !user.expired) {
        this.setUser(user);
        return true;
      }
    } catch(e) {
      console.error(e);
    }
    return false;
  }

  private setUser(oidcUser: OidcUser) {
    const profile = oidcUser.profile;
    const user: User = {
      id: profile.sub,
      username: profile.preferred_username || profile.email || '', 
      role: (Array.isArray(profile['role']) ? profile['role'][0] : profile['role']) || 'user',
      name: profile.name || '',
      email: profile.email,
      accessToken: oidcUser.access_token
    } as unknown as User;
    
    this.currentUser.set(user);
  }

  loginWithToken(accessToken: string, idToken?: string) {
    try {
      const tokenToParse = idToken || accessToken;
      const base64Url = tokenToParse.split('.')[1];
      let base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      while (base64.length % 4) {
        base64 += '=';
      }
      
      const binaryString = window.atob(base64);
      const bytes = new Uint8Array(binaryString.length);
      for (let i = 0; i < binaryString.length; i++) {
        bytes[i] = binaryString.charCodeAt(i);
      }
      const jsonPayload = new TextDecoder('utf-8').decode(bytes);

      const payload = JSON.parse(jsonPayload);
      const user: User = {
        id: payload.sub,
        username: payload.preferred_username || payload.email || '',
        role: (Array.isArray(payload['role']) ? payload['role'][0] : payload['role']) || 'user',
        name: payload.name || '',
        email: payload.email,
        accessToken: accessToken
      } as unknown as User;
      this.currentUser.set(user);
    } catch (e) {
      console.error('Failed to parse token', e);
    }
  }
}

