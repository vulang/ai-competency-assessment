export interface User {
  id: string;
  username: string;
  role: 'admin' | 'editor' | string; // Allow string to support other roles from backend
  roles?: readonly string[];
  name: string;
  email?: string;
  accessToken?: string;
}
