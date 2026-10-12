import { Injectable, signal } from '@angular/core';
import { User } from '../../models/user.model';
import { UserManager, User as OidcUser } from 'oidc-client-ts';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly userManager: UserManager;
  private expiresAt = 0;
  readonly currentUser = signal<User | null>(null);

  constructor() {
    this.userManager = new UserManager(environment.oidc);
    const savedToken = sessionStorage.getItem('access_token');
    if (savedToken) this.loginWithToken(savedToken, sessionStorage.getItem('id_token') || undefined);
    this.userManager.getUser().then(user => {
      if (!this.currentUser() && user && !user.expired) this.setUser(user);
    });
    this.userManager.events.addUserLoaded(user => this.setUser(user));
    this.userManager.events.addUserUnloaded(() => this.clearSession());
  }

  login() { return this.userManager.signinRedirect(); }

  async completeLogin() {
    const user = await this.userManager.signinRedirectCallback();
    this.setUser(user);
    return user;
  }

  logout() {
    const idToken = sessionStorage.getItem('id_token') || undefined;
    this.clearSession();
    return this.userManager.signoutRedirect({ id_token_hint: idToken });
  }

  async invalidateSession(): Promise<void> {
    this.clearSession();
    await this.userManager.removeUser();
  }

  private clearSession(): void {
    sessionStorage.removeItem('access_token');
    sessionStorage.removeItem('id_token');
    this.currentUser.set(null);
    this.expiresAt = 0;
  }

  isLoggedIn(): boolean {
    if (!this.currentUser()) return false;
    if (this.expiresAt <= Date.now() / 1000) {
      this.clearSession();
      return false;
    }
    return true;
  }

  hasRole(role: string): boolean {
    if (!this.isLoggedIn()) return false;
    return this.currentUser()?.roles?.some(value => value.toLowerCase() === role.toLowerCase()) ?? false;
  }

  homePath(): string {
    if (this.hasRole('Admin')) return '/admin';
    if (this.hasRole('Candidate')) return '/candidate/home';
    return '/login';
  }

  async checkAuthAsync(): Promise<boolean> {
    if (this.isLoggedIn()) return true;
    try {
      const user = await this.userManager.getUser();
      if (user && !user.expired) {
        this.setUser(user);
        return this.isLoggedIn();
      }
    } catch (error) { console.error('Failed to restore login', error); }
    return false;
  }

  private setUser(oidcUser: OidcUser): void {
    this.setIdentity(oidcUser.profile, oidcUser.access_token, oidcUser.expires_at ?? 0);
  }

  private setIdentity(profile: Record<string, unknown>, accessToken: string, expiresAt: number): void {
    if (typeof profile['sub'] !== 'string' || !profile['sub'] || expiresAt <= Date.now() / 1000) {
      this.clearSession();
      return;
    }
    const claim = profile['role'];
    const roles = (Array.isArray(claim) ? claim : [claim]).filter((value): value is string => typeof value === 'string');
    const email = typeof profile['email'] === 'string' ? profile['email'] : undefined;
    this.expiresAt = expiresAt;
    this.currentUser.set({
      id: profile['sub'],
      username: typeof profile['preferred_username'] === 'string' ? profile['preferred_username'] : email || '',
      name: typeof profile['name'] === 'string' ? profile['name'] : '',
      email, role: roles[0] || 'user', roles, accessToken
    });
  }

  loginWithToken(accessToken: string, idToken?: string): boolean {
    try {
      // OpenIddict access tokens may be encrypted. The ID token carries the
      // display claims; API authorization still relies on validated access tokens.
      const encoded = (idToken || accessToken).split('.')[1];
      if (!encoded) throw new Error('Missing token payload');
      const base64 = encoded.replace(/-/g, '+').replace(/_/g, '/');
      const binary = window.atob(base64.padEnd(Math.ceil(base64.length / 4) * 4, '='));
      const payload: Record<string, unknown> = JSON.parse(new TextDecoder().decode(
        Uint8Array.from(binary, value => value.charCodeAt(0))));
      this.setIdentity(payload, accessToken, typeof payload['exp'] === 'number' ? payload['exp'] : 0);
      return this.isLoggedIn();
    } catch (error) {
      this.clearSession();
      console.error('Failed to parse login token', error);
      return false;
    }
  }
}
