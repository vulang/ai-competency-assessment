export interface User {
  id: string;
  username: string;
  role: 'admin' | 'editor' | string; // Allow string to support other roles from backend
  name: string;
  email?: string;
  accessToken?: string;
}
