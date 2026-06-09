import { TestBed } from '@angular/core/testing';
import { AuthStore } from './core/auth/auth.store';
import { ActivityLevel } from './core/models';
import { emptyGridQuery, GridFilterOp } from './core/grid.models';
import { activityLabels, optionsFrom, formatMoney } from './core/labels';

/** Builds an unsigned JWT with the given payload (decode only reads the payload segment). */
function fakeJwt(payload: Record<string, unknown>): string {
  const b64 = (o: unknown) => btoa(JSON.stringify(o)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${b64({ alg: 'none' })}.${b64(payload)}.sig`;
}

describe('AuthStore', () => {
  let store: AuthStore;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    store = TestBed.inject(AuthStore);
  });

  it('starts unauthenticated', () => {
    expect(store.isAuthenticated()).toBe(false);
    expect(store.has('clients.read')).toBe(false);
  });

  it('decodes permissions and roles from the access token', () => {
    const exp = Math.floor(Date.now() / 1000) + 3600;
    store.set({
      accessToken: fakeJwt({ email: 'admin@alfaris.local', role: 'Owner', perm: ['clients.read', 'clients.write'], exp }),
      refreshToken: 'r', expiresIn: 900,
    });
    expect(store.isAuthenticated()).toBe(true);
    expect(store.email()).toBe('admin@alfaris.local');
    expect(store.roles()).toContain('Owner');
    expect(store.has('clients.read')).toBe(true);
    expect(store.has('clients.write')).toBe(true);
    expect(store.has('dashboard.charts.manage')).toBe(false);
    expect(store.has()).toBe(true); // no permission required
  });

  it('treats an expired token as unauthenticated', () => {
    store.set({ accessToken: fakeJwt({ perm: ['x'], exp: 1 }), refreshToken: 'r', expiresIn: 0 });
    expect(store.isAuthenticated()).toBe(false);
  });

  it('clears tokens on logout', () => {
    store.set({ accessToken: fakeJwt({ perm: [], exp: Math.floor(Date.now() / 1000) + 60 }), refreshToken: 'r', expiresIn: 60 });
    store.clear();
    expect(store.isAuthenticated()).toBe(false);
    expect(localStorage.getItem('alfaris.access')).toBeNull();
  });
});

describe('grid + label helpers', () => {
  it('produces a clamped default grid query', () => {
    const q = emptyGridQuery(25);
    expect(q.page).toBe(1);
    expect(q.pageSize).toBe(25);
    expect(q.sort).toEqual([]);
    expect(q.filters).toEqual([]);
  });

  it('maps enum filter ops numerically to match the API', () => {
    expect(GridFilterOp.Contains).toBe(2);
    expect(GridFilterOp.Eq).toBe(0);
  });

  it('builds Arabic enum options and money formatting', () => {
    const opts = optionsFrom(activityLabels);
    expect(opts).toContainEqual([String(ActivityLevel.Medium), 'متوسط']);
    expect(formatMoney(1500)).toContain('٥٠٠');
  });
});
