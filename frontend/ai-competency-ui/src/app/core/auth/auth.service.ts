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
    this.userManager.signoutRedirect();
  }

  isLoggedIn() {
    return !!this.currentUser();
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
}

