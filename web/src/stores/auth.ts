import { defineStore } from 'pinia';

export type Role = 'Admin' | 'LoanOfficer' | 'Auditor';

/**
 * Session state. The access token lives in memory only, not localStorage,
 * so it is gone on reload and cannot be read by other scripts from storage.
 */
export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: null as string | null,
    role: null as Role | null,
    username: null as string | null,
  }),
  getters: {
    isAuthenticated: (state): boolean => state.token !== null,
    hasAnyRole: (state) => (...roles: Role[]): boolean =>
      state.role !== null && roles.includes(state.role),
  },
  actions: {
    setSession(token: string, role: Role, username: string) {
      this.token = token;
      this.role = role;
      this.username = username;
    },
    clear() {
      this.token = null;
      this.role = null;
      this.username = null;
    },
  },
});
