import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthService } from './auth.service';

function createJwt(payload: object): string {
  const header = { alg: 'none', typ: 'JWT' };

  const encode = (value: object) =>
    btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');

  return `${encode(header)}.${encode(payload)}.signature`;
}

describe('AuthService', () => {
  let service: AuthService;
  let router: Router;
  let storage: Map<string, string>;

  beforeEach(() => {
    storage = new Map<string, string>();

    vi.stubGlobal('localStorage', {
      getItem: (key: string) => storage.get(key) ?? null,
      setItem: (key: string, value: string) => {
        storage.set(key, value);
      },
      removeItem: (key: string) => {
        storage.delete(key);
      }
    });

    TestBed.configureTestingModule({
      providers: [provideRouter([])]
    });

    service = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it('logs out to the landing page', () => {
    const token = createJwt({
      userId: 'user-1',
      email: 'user@example.com',
      name: 'Test User',
      avatarUrl: 'https://example.com/avatar.png',
      githubUsername: 'test-user',
      exp: Math.floor(Date.now() / 1000) + 60 * 60
    });

    service.handleToken(token);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    service.logout();

    expect(localStorage.getItem('etf_auth_token')).toBeNull();
    expect(service.user()).toBeNull();
    expect(navigateSpy).toHaveBeenCalledWith(['/']);
  });
});

