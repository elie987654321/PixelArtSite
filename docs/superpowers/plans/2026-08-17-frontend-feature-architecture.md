# Frontend Feature Architecture Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reorganise the Angular frontend from its current mixed folder layout into a `core/` + `shared/` + `features/` architecture with lazy-loaded feature routes, without changing any user-visible behaviour.

**Architecture:** Feature boundaries first, technical layers second. `core/` holds application-wide infrastructure that exists once (session state, HTTP config, guard, interceptor). `shared/` holds UI with no knowledge of the business domain. `features/` holds everything else, each feature owning its own `pages/`, `components/`, `data-access/`, `models/`, and a route file loaded with `loadChildren`.

**Tech Stack:** Angular 19.2, standalone components, signals, RxJS 7.8, Karma + Jasmine, TypeScript 5.7.

**Spec:** No separate spec document. The target architecture was supplied by the user and is reproduced in [Target structure](#target-structure) below. The audit that identified the defects fixed here is reproduced in [Findings](#findings).

## Global Constraints

- Angular **19.2** — standalone components only, no `NgModule`.
- **No code comments.** Existing comments in moved files are removed as part of the move.
- **No git write operations.** Every commit step in this plan produces command *text for the human to run*. An agent executing this plan must never run `git add`, `git commit`, `git mv`, or any other git write. Move files with the shell `mv` command, not `git mv`. See [CLAUDE.md](../../../CLAUDE.md).
- **Behaviour must not change.** Every URL, every displayed string, every guard, and every HTTP request shape stays identical. Task 7 is the single exception and states its change explicitly.
- All commands run from `frontend/`.
- Build check: `npm run build`. Test check: `npm test -- --watch=false --browsers=ChromeHeadless`.

---

## Findings

These are the defects the current layout carries. Each is fixed by a numbered task.

| # | Finding | Evidence | Fixed by |
|---|---|---|---|
| F1 | `app.component.spec.ts` does not compile. It asserts `app.title` and an `h1` reading `Hello, pixelart`; `AppComponent` has no `title` property and its template has no such heading. The whole Karma suite fails to build, so no test can run. | `app.component.spec.ts:20`, `app.component.ts:13-23` | Task 1 |
| F2 | Two classes named `AuthService`, both `providedIn: 'root'`, both writing `pixelart_token` and `pixelart_username`. `app/model/service/auth.service.ts` is a byte-for-byte copy of `app/auth/core/auth.service.ts` and is imported by nothing. | Both files; `grep` finds five importers, all pointing at `auth/core`. | Task 2 |
| F3 | API origin hardcoded in two files. No `src/environments/` folder exists. The app only functions against `localhost:5126`. | `auth.repository.ts:11`, `drawing.repository.ts:11` | Task 3 |
| F4 | Two competing folder conventions. Auth is feature-first (`app/auth/{core,repository,guard,main}`); drawings is type-first (`app/model/`, `app/core/service/`, `app/repository/`). | Directory listing | Tasks 4–6 |
| F5 | `DrawingService` forwards all four methods to `DrawingRepository` with no added behaviour. | `core/service/drawing.service.ts:10-24` | Task 5 |
| F6 | Every route is eagerly imported in `app.routes.ts`. No `loadComponent` or `loadChildren` anywhere. | `app.routes.ts:2-8` | Tasks 4–6 |
| F7 | The backend returns ProblemDetails carrying the exact validation message in `title`; components discard the body and display a fixed string. A user entering a 2-character username is told "Could not create the account. Please try again." | `UseCaseExceptionHandler.cs:47`, `register.component.ts:53-56` | Task 7 |

---

## Target structure

```
frontend/src/
├── environments/
│   ├── environment.ts                     production values
│   └── environment.development.ts         localhost values
└── app/
    ├── core/
    │   ├── auth/
    │   │   ├── auth.model.ts              request/response interfaces
    │   │   ├── auth.api.ts                HTTP calls to /api/auth
    │   │   ├── auth.service.ts            session signals + localStorage
    │   │   ├── auth.guard.ts              CanActivateFn
    │   │   └── auth.interceptor.ts        attaches the bearer token
    │   ├── config/
    │   │   └── api-config.ts              API_BASE_URL injection token
    │   └── http/
    │       └── api-error.ts               reads ProblemDetails.title
    ├── shared/
    │   └── ui/
    │       └── breadcrumb/                domain-agnostic navigation
    ├── features/
    │   ├── auth/
    │   │   ├── pages/login/
    │   │   ├── pages/register/
    │   │   └── auth.routes.ts             AUTH_ROUTES
    │   ├── home/
    │   │   └── pages/home/
    │   └── drawings/
    │       ├── models/drawing.model.ts
    │       ├── data-access/drawing.api.ts
    │       ├── components/drawing-view/
    │       ├── components/pixel-editor/   + tool.ts, cell-selection.ts, tools/
    │       ├── pages/gallery/
    │       ├── pages/drawing-editor/
    │       ├── pages/drawing-options/
    │       └── drawings.routes.ts         DRAWINGS_ROUTES
    ├── app.component.ts
    ├── app.config.ts
    └── app.routes.ts
```

### Placement decisions

**Auth is split across `core/` and `features/`.** The session service, guard, interceptor, and API client are used by the router and by every outgoing request, so they exist once for the whole application and belong in `core/auth/`. The login and register screens are route-level UI with no other consumer, so they belong in `features/auth/pages/`. Putting the whole slice in `features/` would force `app.config.ts` and the router to import from a feature; putting the pages in `core/` would violate the rule that core holds no feature-specific UI.

**The guard and interceptor live in `core/auth/`, not in `core/guards/` and `core/interceptors/`.** This is a deliberate deviation from the supplied structure. Both files exist only to read `AuthService`: `authGuard` calls `isLoggedIn()`, `authInterceptor` calls `token()`. Splitting them into sibling folders separates three files that always change together — adding a refresh-token flow edits all three — and yields folders holding one file each. Create `core/guards/` and `core/interceptors/` when a second, non-auth guard or interceptor exists.

**`shared/` gets only `ui/`.** The supplied structure also lists `directives/`, `pipes/`, and `utils/`. The application currently has no directive, no pipe, and no domain-agnostic utility, so those folders would be empty. Add each when its first occupant exists.

**`breadcrumb` goes to `shared/ui/`.** It reads `route.snapshot.routeConfig.data['breadcrumb']` and knows nothing about users or drawings.

**`DrawingService` is deleted, not moved.** It is a four-method pass-through (F5). `DrawingApi` in `data-access/` replaces both it and `DrawingRepository`. A `drawing.store.ts` alongside it is the correct place for shared drawing state if it is ever needed; nothing in the current application shares drawing state across components, so adding one now would be an unused indirection.

**Class names and folder names are preserved during moves.** `ExistingDrawingEditorWrapper` and `DrawingOptionsComponent` keep their names. Renaming them is worthwhile but would mix rename churn into move diffs and make review harder. See [Deliberately out of scope](#deliberately-out-of-scope).

### File disposition

| Current | Becomes |
|---|---|
| `app/auth/core/auth.model.ts` | `app/core/auth/auth.model.ts` |
| `app/auth/core/auth.service.ts` | `app/core/auth/auth.service.ts` |
| `app/auth/repository/auth.repository.ts` | `app/core/auth/auth.api.ts` (class `AuthRepository` → `AuthApi`) |
| `app/auth/repository/interceptor/auth.interceptor.ts` | `app/core/auth/auth.interceptor.ts` |
| `app/auth/guard/auth.guard.ts` | `app/core/auth/auth.guard.ts` |
| `app/auth/main/login/` | `app/features/auth/pages/login/` |
| `app/auth/main/register/` | `app/features/auth/pages/register/` |
| `app/home/` | `app/features/home/pages/home/` |
| `app/breadcrumb/` | `app/shared/ui/breadcrumb/` |
| `app/model/drawing.model.ts` | `app/features/drawings/models/drawing.model.ts` |
| `app/repository/drawing.repository.ts` | `app/features/drawings/data-access/drawing.api.ts` (class `DrawingRepository` → `DrawingApi`) |
| `app/main/gallery/` | `app/features/drawings/pages/gallery/` |
| `app/main/editor/drawing-editor/` | `app/features/drawings/pages/drawing-editor/` |
| `app/main/editor/drawing-options/` | `app/features/drawings/pages/drawing-options/` |
| `app/main/editor/drawing-view/` | `app/features/drawings/components/drawing-view/` |
| `app/main/editor/pixel-editor/` | `app/features/drawings/components/pixel-editor/` |
| `app/model/service/auth.service.ts` | **deleted** (duplicate, F2) |
| `app/core/service/drawing.service.ts` | **deleted** (pass-through, F5) |

---

## Task 1: Repair the test harness

Nothing else in this plan can be verified until Karma compiles. `app.component.spec.ts` references a property that does not exist, which is a TypeScript error, which fails the whole test build.

**Files:**
- Modify: `frontend/src/app/app.component.spec.ts`

**Interfaces:**
- Consumes: nothing.
- Produces: a green `npm test` baseline. Every later task depends on this.

- [ ] **Step 1: Confirm the suite is currently broken**

```bash
npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: FAIL. TypeScript error on `app.component.spec.ts` — `Property 'title' does not exist on type 'AppComponent'`.

If Chrome is not on the machine, install it or set `CHROME_BIN` to an existing Chromium binary before continuing. Karma cannot run without a browser.

- [ ] **Step 2: Replace the spec**

Replace the entire contents of `frontend/src/app/app.component.spec.ts` with Appendix A — Test 1.

`AppComponent` injects `Router` and `AuthService`, and its template uses `RouterOutlet` and `RouterLink`. The test module therefore needs `provideRouter([])` and `provideHttpClient()`. Without them `TestBed.createComponent` throws a NullInjectorError.

- [ ] **Step 3: Run the suite**

```bash
npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: PASS, 2 specs.

- [ ] **Step 4: Hand the commit to the human**

Report which files changed. Suggested command text — **do not run it**:

```bash
git add frontend/src/app/app.component.spec.ts
git commit -m "test: repair the frontend test harness"
```

---

## Task 2: Delete the duplicate AuthService

**Files:**
- Delete: `frontend/src/app/model/service/auth.service.ts`

**Interfaces:**
- Consumes: nothing.
- Produces: a single `AuthService`, at `app/auth/core/auth.service.ts` until Task 4 moves it.

- [ ] **Step 1: Prove nothing imports it**

```bash
grep -rn "model/service" frontend/src
```

Expected: no output. If any line is printed, stop — repoint that import at `app/auth/core/auth.service` first, then continue.

- [ ] **Step 2: Delete the file and its now-empty directory**

```bash
rm frontend/src/app/model/service/auth.service.ts
rmdir frontend/src/app/model/service
```

- [ ] **Step 3: Verify the build still compiles**

```bash
npm run build
```

Expected: success.

- [ ] **Step 4: Run the suite**

```bash
npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: PASS, 2 specs.

- [ ] **Step 5: Hand the commit to the human**

Suggested command text — **do not run it**:

```bash
git add -A frontend/src/app/model
git commit -m "refactor(frontend): delete duplicate AuthService"
```

---

## Task 3: Environment configuration

**Files:**
- Create: `frontend/src/environments/environment.ts`
- Create: `frontend/src/environments/environment.development.ts`
- Create: `frontend/src/app/core/config/api-config.ts`
- Modify: `frontend/angular.json` — the `build.configurations.development` block
- Modify: `frontend/src/app/auth/repository/auth.repository.ts`
- Modify: `frontend/src/app/repository/drawing.repository.ts`
- Test: `frontend/src/app/core/config/api-config.spec.ts`

**Interfaces:**
- Consumes: nothing.
- Produces: `API_BASE_URL: InjectionToken<string>` exported from `app/core/config/api-config.ts`, resolving to `http://localhost:5126/api` under `ng serve` and `/api` in a production build. Tasks 4 and 5 inject it.

- [ ] **Step 1: Write the failing test**

Create `frontend/src/app/core/config/api-config.spec.ts` with Appendix A — Test 2.

- [ ] **Step 2: Run it to confirm it fails**

```bash
npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: FAIL. `Cannot find module '../../../environments/environment'`.

- [ ] **Step 3: Create the environment files**

`frontend/src/environments/environment.ts` — the default, used by production builds and by Karma:

```ts
export const environment = {
  production: true,
  apiBaseUrl: '/api',
};
```

`frontend/src/environments/environment.development.ts`:

```ts
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5126/api',
};
```

The production value is a same-origin relative path. A deployed build served behind a reverse proxy that forwards `/api` needs no origin, and a relative path removes the CORS requirement in production.

- [ ] **Step 4: Create the injection token**

`frontend/src/app/core/config/api-config.ts`:

```ts
import { InjectionToken } from '@angular/core';
import { environment } from '../../../environments/environment';

export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => environment.apiBaseUrl,
});
```

A token rather than an exported constant, because a token can be overridden per-test with `{ provide: API_BASE_URL, useValue: '/api' }` without touching the file replacement machinery.

- [ ] **Step 5: Wire the file replacement**

In `frontend/angular.json`, replace the `projects.pixelart.architect.build.configurations.development` object with:

```json
"development": {
  "optimization": false,
  "extractLicenses": false,
  "sourceMap": true,
  "fileReplacements": [
    {
      "replace": "src/environments/environment.ts",
      "with": "src/environments/environment.development.ts"
    }
  ]
}
```

`serve.defaultConfiguration` is already `development`, so `npm start` picks up the localhost value. `build.defaultConfiguration` is `production`, so `npm run build` keeps `/api`. The Karma builder declares no file replacements, so tests resolve `environment.ts` and see `/api`.

- [ ] **Step 6: Run the test to confirm it passes**

```bash
npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: PASS, 4 specs.

- [ ] **Step 7: Consume the token in both repositories**

Replace the whole of `frontend/src/app/auth/repository/auth.repository.ts`:

```ts
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/config/api-config';
import { AuthResponse, LoginRequest, RegisterRequest } from '../core/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthRepository {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/auth`;

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, credentials);
  }

  register(credentials: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/register`, credentials);
  }
}
```

Replace the whole of `frontend/src/app/repository/drawing.repository.ts`:

```ts
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../core/config/api-config';
import { Drawing, DrawingInput } from '../model/drawing.model';

@Injectable({ providedIn: 'root' })
export class DrawingRepository {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/drawings`;

  getAll(): Observable<Drawing[]> {
    return this.http.get<Drawing[]>(this.baseUrl);
  }

  getById(id: number): Observable<Drawing> {
    return this.http.get<Drawing>(`${this.baseUrl}/${id}`);
  }

  create(drawing: DrawingInput): Observable<Drawing> {
    return this.http.post<Drawing>(this.baseUrl, drawing);
  }

  update(id: number, drawing: DrawingInput): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, drawing);
  }
}
```

`inject()` is called in a field initializer, which runs inside the injection context Angular establishes when constructing the service. Calling `inject()` anywhere else in these classes throws `NG0203`.

- [ ] **Step 8: Verify against the running backend**

```bash
npm start
```

Open `http://localhost:4200`, sign in, and open the gallery. Confirm in the browser devtools Network tab that requests go to `http://localhost:5126/api/auth/login` and `http://localhost:5126/api/drawings` — identical to before this task.

- [ ] **Step 9: Hand the commit to the human**

Suggested command text — **do not run it**:

```bash
git add frontend/src/environments frontend/src/app/core/config frontend/angular.json \
        frontend/src/app/auth/repository/auth.repository.ts \
        frontend/src/app/repository/drawing.repository.ts
git commit -m "refactor(frontend): move the API origin into environment files"
```

---

## Task 4: Move auth into core/ and features/

**Files:**
- Create: `frontend/src/app/core/auth/auth.model.ts`, `auth.api.ts`, `auth.service.ts`, `auth.guard.ts`, `auth.interceptor.ts`
- Create: `frontend/src/app/features/auth/auth.routes.ts`
- Move: `frontend/src/app/auth/main/login/` → `frontend/src/app/features/auth/pages/login/`
- Move: `frontend/src/app/auth/main/register/` → `frontend/src/app/features/auth/pages/register/`
- Delete: `frontend/src/app/auth/`
- Modify: `frontend/src/app/app.config.ts`, `frontend/src/app/app.component.ts`, `frontend/src/app/app.routes.ts`
- Test: `frontend/src/app/core/auth/auth.service.spec.ts`

**Interfaces:**
- Consumes: `API_BASE_URL` from `app/core/config/api-config` (Task 3).
- Produces:
  - `AuthApi` with `login(credentials: LoginRequest): Observable<AuthResponse>` and `register(credentials: RegisterRequest): Observable<AuthResponse>`
  - `AuthService` with `username: Signal<string | null>`, `isLoggedIn: Signal<boolean>`, `token(): string | null`, `login(credentials: LoginRequest): Observable<AuthResponse>`, `register(credentials: RegisterRequest): Observable<AuthResponse>`, `logout(): void`
  - `authGuard: CanActivateFn`, `authInterceptor: HttpInterceptorFn`
  - `AUTH_ROUTES: Routes` from `app/features/auth/auth.routes`

- [ ] **Step 1: Write the failing test**

Create `frontend/src/app/core/auth/auth.service.spec.ts` with Appendix A — Test 3.

- [ ] **Step 2: Run it to confirm it fails**

```bash
npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: FAIL. `Cannot find module './auth.service'`.

- [ ] **Step 3: Create `core/auth/auth.model.ts`**

```ts
export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  password: string;
}

export interface AuthResponse {
  username: string;
  token: string;
}
```

- [ ] **Step 4: Create `core/auth/auth.api.ts`**

```ts
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api-config';
import { AuthResponse, LoginRequest, RegisterRequest } from './auth.model';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/auth`;

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, credentials);
  }

  register(credentials: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/register`, credentials);
  }
}
```

- [ ] **Step 5: Create `core/auth/auth.service.ts`**

```ts
import { computed, inject, Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { AuthApi } from './auth.api';
import { AuthResponse, LoginRequest, RegisterRequest } from './auth.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private static readonly TOKEN_KEY = 'pixelart_token';
  private static readonly USERNAME_KEY = 'pixelart_username';

  private readonly api = inject(AuthApi);

  private readonly _token = signal<string | null>(
    localStorage.getItem(AuthService.TOKEN_KEY),
  );
  private readonly _username = signal<string | null>(
    localStorage.getItem(AuthService.USERNAME_KEY),
  );

  readonly username = this._username.asReadonly();
  readonly isLoggedIn = computed(() => this._token() !== null);

  token(): string | null {
    return this._token();
  }

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.api.login(credentials).pipe(tap((res) => this.storeSession(res)));
  }

  register(credentials: RegisterRequest): Observable<AuthResponse> {
    return this.api.register(credentials).pipe(tap((res) => this.storeSession(res)));
  }

  logout(): void {
    localStorage.removeItem(AuthService.TOKEN_KEY);
    localStorage.removeItem(AuthService.USERNAME_KEY);
    this._token.set(null);
    this._username.set(null);
  }

  private storeSession(res: AuthResponse): void {
    localStorage.setItem(AuthService.TOKEN_KEY, res.token);
    localStorage.setItem(AuthService.USERNAME_KEY, res.username);
    this._token.set(res.token);
    this._username.set(res.username);
  }
}
```

The `localStorage` reads stay in field initializers, so a page refresh restores the session before any component renders. The storage keys are unchanged, so an already-signed-in browser stays signed in across this refactor.

- [ ] **Step 6: Create `core/auth/auth.guard.ts`**

```ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isLoggedIn()) {
    return true;
  }
  return router.createUrlTree(['/login']);
};
```

- [ ] **Step 7: Create `core/auth/auth.interceptor.ts`**

```ts
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).token();
  if (!token) {
    return next(req);
  }

  const authReq = req.clone({
    setHeaders: { Authorization: `Bearer ${token}` },
  });
  return next(authReq);
};
```

- [ ] **Step 8: Move the two page folders**

```bash
mkdir -p frontend/src/app/features/auth/pages
mv frontend/src/app/auth/main/login    frontend/src/app/features/auth/pages/login
mv frontend/src/app/auth/main/register frontend/src/app/features/auth/pages/register
```

`mv`, not `git mv` — see Global Constraints.

- [ ] **Step 9: Repoint the page imports**

In both `frontend/src/app/features/auth/pages/login/login.component.ts` and `.../register/register.component.ts`, change the `AuthService` import line to:

```ts
import { AuthService } from '../../../../core/auth/auth.service';
```

Depth check: from `app/features/auth/pages/login/`, four `../` reach `app/`, then `core/auth/auth.service`.

- [ ] **Step 10: Create `features/auth/auth.routes.ts`**

```ts
import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./pages/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./pages/register/register.component').then((m) => m.RegisterComponent),
  },
];
```

- [ ] **Step 11: Delete the old auth tree**

```bash
rm -r frontend/src/app/auth
```

- [ ] **Step 12: Repoint `app.config.ts`**

```ts
import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
  ],
};
```

- [ ] **Step 13: Repoint `app.component.ts`**

Change line 3 only:

```ts
import { AuthService } from './core/auth/auth.service';
```

- [ ] **Step 14: Update `app.routes.ts` for the auth half**

Replace the whole file. The drawings half still points at old paths and is corrected in Task 5.

```ts
import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { GalleryComponent } from './main/gallery/gallery.component';
import { DrawingOptionsComponent } from './main/editor/drawing-options/drawing-options.component';
import { ExistingDrawingEditorWrapper } from './main/editor/drawing-editor/drawing-editor.component';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', component: HomeComponent, canActivate: [authGuard] },
  {
    path: '',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },
  {
    path: 'drawings',
    canActivate: [authGuard],
    data: { breadcrumb: 'Gallery' },
    children: [
      { path: '', component: GalleryComponent },
      {
        path: ':id',
        component: ExistingDrawingEditorWrapper,
        data: { breadcrumb: 'Drawing' },
      },
    ],
  },
  {
    path: 'create',
    component: DrawingOptionsComponent,
    data: { breadcrumb: 'New drawing' },
  },
  { path: '**', redirectTo: '' },
];
```

Two entries declare `path: ''`. The home entry carries `pathMatch: 'full'`, so it matches only the exact empty URL. `/login` fails that match and falls through to the second empty-path entry, which is a prefix match, loads `AUTH_ROUTES`, and matches the `login` child. Without `pathMatch: 'full'` the home route would swallow `/login` and `/register`.

The `create` route keeps no guard, exactly as before. That is a pre-existing inconsistency — the page calls `POST /api/drawings`, which the API rejects with 401 for anonymous callers. Changing it is out of scope for a refactor; see [Deliberately out of scope](#deliberately-out-of-scope).

- [ ] **Step 15: Run the suite**

```bash
npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: PASS, 8 specs.

- [ ] **Step 16: Verify the routes in a browser**

```bash
npm start
```

Confirm all four by hand:
1. `http://localhost:4200/login` renders the sign-in form.
2. `http://localhost:4200/register` renders the create-account form.
3. Signing in navigates to `/` and shows the home page.
4. Signing out, then opening `http://localhost:4200/drawings`, redirects to `/login`.

Check the Network tab on step 1: navigating to `/login` fetches a separate lazy chunk.

- [ ] **Step 17: Hand the commit to the human**

Suggested command text — **do not run it**:

```bash
git add -A frontend/src/app
git commit -m "refactor(frontend): move auth into core/ and features/auth"
```

---

## Task 5: Move drawings into features/drawings

**Files:**
- Create: `frontend/src/app/features/drawings/models/drawing.model.ts`
- Create: `frontend/src/app/features/drawings/data-access/drawing.api.ts`
- Create: `frontend/src/app/features/drawings/drawings.routes.ts`
- Move: `app/main/gallery/` → `app/features/drawings/pages/gallery/`
- Move: `app/main/editor/drawing-editor/` → `app/features/drawings/pages/drawing-editor/`
- Move: `app/main/editor/drawing-options/` → `app/features/drawings/pages/drawing-options/`
- Move: `app/main/editor/drawing-view/` → `app/features/drawings/components/drawing-view/`
- Move: `app/main/editor/pixel-editor/` → `app/features/drawings/components/pixel-editor/`
- Delete: `app/main/`, `app/model/`, `app/repository/`, `app/core/service/`
- Modify: `frontend/src/app/app.routes.ts`
- Test: `frontend/src/app/features/drawings/data-access/drawing.api.spec.ts`

**Interfaces:**
- Consumes: `API_BASE_URL` from `app/core/config/api-config` (Task 3); `authGuard` from `app/core/auth/auth.guard` (Task 4).
- Produces:
  - `DrawingApi` with `getAll(): Observable<Drawing[]>`, `getById(id: number): Observable<Drawing>`, `create(drawing: DrawingInput): Observable<Drawing>`, `update(id: number, drawing: DrawingInput): Observable<void>`
  - `DRAWINGS_ROUTES: Routes` from `app/features/drawings/drawings.routes`

- [ ] **Step 1: Write the failing test**

Create `frontend/src/app/features/drawings/data-access/drawing.api.spec.ts` with Appendix A — Test 4.

- [ ] **Step 2: Run it to confirm it fails**

```bash
npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: FAIL. `Cannot find module './drawing.api'`.

- [ ] **Step 3: Create the model**

`frontend/src/app/features/drawings/models/drawing.model.ts`:

```ts
export interface Drawing {
  id: number;
  name: string;
  width: number;
  height: number;
  pixels: string[][];
  createdAt: string;
}

export interface DrawingInput {
  name: string;
  width: number;
  height: number;
  pixels: string[][];
}
```

- [ ] **Step 4: Create the data-access client**

`frontend/src/app/features/drawings/data-access/drawing.api.ts`:

```ts
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../core/config/api-config';
import { Drawing, DrawingInput } from '../models/drawing.model';

@Injectable({ providedIn: 'root' })
export class DrawingApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/drawings`;

  getAll(): Observable<Drawing[]> {
    return this.http.get<Drawing[]>(this.baseUrl);
  }

  getById(id: number): Observable<Drawing> {
    return this.http.get<Drawing>(`${this.baseUrl}/${id}`);
  }

  create(drawing: DrawingInput): Observable<Drawing> {
    return this.http.post<Drawing>(this.baseUrl, drawing);
  }

  update(id: number, drawing: DrawingInput): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, drawing);
  }
}
```

This class absorbs both `DrawingRepository` and `DrawingService`. The method signatures are unchanged, so the three consuming components only need their import line and injected type renamed.

- [ ] **Step 5: Run the test to confirm it passes**

```bash
npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: PASS, 12 specs.

- [ ] **Step 6: Move the five component folders**

```bash
mkdir -p frontend/src/app/features/drawings/pages
mkdir -p frontend/src/app/features/drawings/components
mv frontend/src/app/main/gallery                 frontend/src/app/features/drawings/pages/gallery
mv frontend/src/app/main/editor/drawing-editor   frontend/src/app/features/drawings/pages/drawing-editor
mv frontend/src/app/main/editor/drawing-options  frontend/src/app/features/drawings/pages/drawing-options
mv frontend/src/app/main/editor/drawing-view     frontend/src/app/features/drawings/components/drawing-view
mv frontend/src/app/main/editor/pixel-editor     frontend/src/app/features/drawings/components/pixel-editor
```

`pixel-editor/` moves whole, carrying `tool.ts`, `cell-selection.ts`, and `tools/`. Its internal imports are all relative and sibling-level, so none of them change.

- [ ] **Step 7: Repoint `gallery.component.ts`**

Replace lines 1–5 of `frontend/src/app/features/drawings/pages/gallery/gallery.component.ts`:

```ts
import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DrawingApi } from '../../data-access/drawing.api';
import { Drawing } from '../../models/drawing.model';
import { DrawingViewComponent } from '../../components/drawing-view/drawing-view.component';
```

Then rename the injected field in the constructor and its one use in `ngOnInit`:

```ts
  constructor(private readonly drawingApi: DrawingApi) {}

  ngOnInit(): void {
    this.drawingApi.getAll().subscribe({
```

- [ ] **Step 8: Repoint `drawing-editor.component.ts`**

Replace lines 1–5 of `frontend/src/app/features/drawings/pages/drawing-editor/drawing-editor.component.ts`:

```ts
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PixelEditorComponent } from '../../components/pixel-editor/pixel-editor.component';
import { DrawingApi } from '../../data-access/drawing.api';
import { Drawing, DrawingInput } from '../../models/drawing.model';
```

Rename the injected field and its two uses:

```ts
  constructor(
    private readonly route: ActivatedRoute,
    private readonly drawingApi: DrawingApi,
  ) {}
```

`this.drawingService.getById(id)` becomes `this.drawingApi.getById(id)`; `this.drawingService.update(...)` becomes `this.drawingApi.update(...)`.

- [ ] **Step 9: Repoint `drawing-options.component.ts`**

Replace lines 1–5 of `frontend/src/app/features/drawings/pages/drawing-options/drawing-options.component.ts`:

```ts
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PixelEditorComponent } from '../../components/pixel-editor/pixel-editor.component';
import { DrawingApi } from '../../data-access/drawing.api';
import { DrawingInput } from '../../models/drawing.model';
```

Rename the injected field and its two uses:

```ts
  constructor(private readonly drawingApi: DrawingApi) {}
```

`this.drawingService.create(payload)` becomes `this.drawingApi.create(payload)`; `this.drawingService.update(this.savedId, payload)` becomes `this.drawingApi.update(this.savedId, payload)`.

- [ ] **Step 10: Repoint `drawing-view.component.ts`**

Change line 2 only:

```ts
import { Drawing } from '../../models/drawing.model';
```

- [ ] **Step 11: Delete the old trees**

```bash
rm -r frontend/src/app/main
rm -r frontend/src/app/model
rm -r frontend/src/app/repository
rm -r frontend/src/app/core/service
```

`app/core/` survives — it now holds `auth/` and `config/`.

- [ ] **Step 12: Create `features/drawings/drawings.routes.ts`**

```ts
import { Routes } from '@angular/router';

export const DRAWINGS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/gallery/gallery.component').then((m) => m.GalleryComponent),
  },
  {
    path: ':id',
    data: { breadcrumb: 'Drawing' },
    loadComponent: () =>
      import('./pages/drawing-editor/drawing-editor.component').then(
        (m) => m.ExistingDrawingEditorWrapper,
      ),
  },
];
```

- [ ] **Step 13: Update `app.routes.ts`**

Replace the whole file:

```ts
import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', component: HomeComponent, canActivate: [authGuard] },
  {
    path: '',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },
  {
    path: 'drawings',
    canActivate: [authGuard],
    data: { breadcrumb: 'Gallery' },
    loadChildren: () =>
      import('./features/drawings/drawings.routes').then((m) => m.DRAWINGS_ROUTES),
  },
  {
    path: 'create',
    data: { breadcrumb: 'New drawing' },
    loadComponent: () =>
      import('./features/drawings/pages/drawing-options/drawing-options.component').then(
        (m) => m.DrawingOptionsComponent,
      ),
  },
  { path: '**', redirectTo: '' },
];
```

`HomeComponent` stays eagerly imported here; Task 6 moves it and makes it lazy.

- [ ] **Step 14: Run the suite and build**

```bash
npm test -- --watch=false --browsers=ChromeHeadless
npm run build
```

Expected: PASS, 12 specs; build succeeds.

- [ ] **Step 15: Verify the drawings routes in a browser**

Start the backend containers first, then `npm start`. Confirm by hand:
1. `/drawings` lists existing drawings and each thumbnail renders.
2. Clicking a drawing opens the editor with its pixels loaded.
3. Drawing on the canvas and saving returns to a saved state with no console error.
4. `/create` accepts a name, width, and height, then saves.
5. The breadcrumb on `/drawings/1` reads `Home / Gallery / Drawing`.

Item 5 is the one at risk. `BreadcrumbComponent` walks `route.snapshot.routeConfig?.data?.['breadcrumb']` down the activated tree; `drawings.routes.ts` must keep `data: { breadcrumb: 'Drawing' }` on the `:id` entry for the third crumb to appear. If the crumb is missing, the `data` moved to the wrong route object.

- [ ] **Step 16: Hand the commit to the human**

Suggested command text — **do not run it**:

```bash
git add -A frontend/src/app
git commit -m "refactor(frontend): move drawings into features/drawings"
```

---

## Task 6: Move breadcrumb to shared/ and home to features/

**Files:**
- Move: `app/breadcrumb/` → `app/shared/ui/breadcrumb/`
- Move: `app/home/` → `app/features/home/pages/home/`
- Modify: `frontend/src/app/app.component.ts`, `frontend/src/app/app.routes.ts`

**Interfaces:**
- Consumes: nothing beyond Tasks 4 and 5.
- Produces: `BreadcrumbComponent` at `app/shared/ui/breadcrumb/breadcrumb.component`; `HomeComponent` at `app/features/home/pages/home/home.component`.

- [ ] **Step 1: Move both folders**

```bash
mkdir -p frontend/src/app/shared/ui
mkdir -p frontend/src/app/features/home/pages
mv frontend/src/app/breadcrumb frontend/src/app/shared/ui/breadcrumb
mv frontend/src/app/home       frontend/src/app/features/home/pages/home
```

`BreadcrumbComponent` imports only `@angular/router` and `rxjs`, so no import inside it changes. `HomeComponent` imports only `@angular/core` and `@angular/router`, so it does not change either.

- [ ] **Step 2: Repoint `app.component.ts`**

Replace lines 1–4:

```ts
import { Component } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth/auth.service';
import { BreadcrumbComponent } from './shared/ui/breadcrumb/breadcrumb.component';
```

- [ ] **Step 3: Make the home route lazy**

In `frontend/src/app/app.routes.ts`, delete the `HomeComponent` import line and replace the first route entry:

```ts
  {
    path: '',
    pathMatch: 'full',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/home/pages/home/home.component').then((m) => m.HomeComponent),
  },
```

`app.routes.ts` now imports only `Routes` and `authGuard`. Every route component in the application is lazy.

- [ ] **Step 4: Confirm the old directories are gone**

```bash
ls frontend/src/app
```

Expected exactly: `app.component.css  app.component.html  app.component.spec.ts  app.component.ts  app.config.ts  app.routes.ts  core  features  shared`

If `main`, `model`, `repository`, `auth`, `home`, or `breadcrumb` still appears, a move in Task 4, 5, or 6 left a directory behind.

- [ ] **Step 5: Run the suite and build**

```bash
npm test -- --watch=false --browsers=ChromeHeadless
npm run build
```

Expected: PASS, 12 specs; build succeeds. Check the build output lists several lazy chunks rather than one initial bundle.

- [ ] **Step 6: Verify in a browser**

`npm start`, then confirm the breadcrumb still renders on `/drawings` and `/drawings/1`, and that `/` renders the home page for a signed-in user.

- [ ] **Step 7: Hand the commit to the human**

Suggested command text — **do not run it**:

```bash
git add -A frontend/src/app
git commit -m "refactor(frontend): move breadcrumb to shared/ui and home to features/home"
```

---

## Task 7: Surface API error messages

The only task in this plan that changes behaviour. The backend already returns the precise failure reason; the components discard it (F7).

**Files:**
- Create: `frontend/src/app/core/http/api-error.ts`
- Modify: `frontend/src/app/features/auth/pages/login/login.component.ts`
- Modify: `frontend/src/app/features/auth/pages/register/register.component.ts`
- Modify: `frontend/src/app/features/drawings/pages/drawing-editor/drawing-editor.component.ts`
- Modify: `frontend/src/app/features/drawings/pages/drawing-options/drawing-options.component.ts`
- Test: `frontend/src/app/core/http/api-error.spec.ts`

**Interfaces:**
- Consumes: nothing.
- Produces: `apiErrorMessage(error: unknown, fallback: string): string` from `app/core/http/api-error`.

- [ ] **Step 1: Write the failing test**

Create `frontend/src/app/core/http/api-error.spec.ts` with Appendix A — Test 5.

- [ ] **Step 2: Run it to confirm it fails**

```bash
npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: FAIL. `Cannot find module './api-error'`.

- [ ] **Step 3: Create the helper**

`frontend/src/app/core/http/api-error.ts`:

```ts
import { HttpErrorResponse } from '@angular/common/http';

export function apiErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as { title?: unknown } | null;
    if (typeof body?.title === 'string' && body.title.trim().length > 0) {
      return body.title;
    }
  }
  return fallback;
}
```

`UseCaseExceptionHandler` sets `ProblemDetails.Title` to the exception message, so `title` carries text such as `Username must be at least 3 characters.` A network failure produces an `HttpErrorResponse` whose `error` is a `ProgressEvent` with no `title`, so the fallback is returned; the `typeof` guard is what distinguishes the two.

- [ ] **Step 4: Run the test to confirm it passes**

```bash
npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: PASS, 17 specs.

- [ ] **Step 5: Use it in `register.component.ts`**

Add the import:

```ts
import { apiErrorMessage } from '../../../../core/http/api-error';
```

Replace the `error` callback body:

```ts
        error: (err) => {
          console.error(err);
          this.submitting = false;
          this.error = apiErrorMessage(
            err,
            'Could not create the account. Please try again.',
          );
        },
```

The `409` branch is removed. The API returns `Username is already taken.` as the `title` of the 409, so the message the user sees remains accurate while the special case disappears.

- [ ] **Step 6: Use it in `login.component.ts`**

Add the import:

```ts
import { apiErrorMessage } from '../../../../core/http/api-error';
```

Replace the `error` callback body:

```ts
      error: (err) => {
        console.error(err);
        this.submitting = false;
        this.error = apiErrorMessage(err, 'Could not sign in. Please try again.');
      },
```

The 401 `title` is `Invalid username or password.`, matching the string this replaces.

- [ ] **Step 7: Use it in `drawing-editor.component.ts`**

Add the import:

```ts
import { apiErrorMessage } from '../../../../core/http/api-error';
```

Replace both `error` callback bodies:

```ts
      error: (err) => {
        console.error(err);
        this.loadError = apiErrorMessage(err, 'Could not load the drawing.');
        this.loading = false;
      },
```

```ts
      error: (err) => {
        console.error(err);
        this.saving = false;
        this.saveError = apiErrorMessage(err, 'Could not save the drawing.');
      },
```

This is where the change earns the most: `DrawingPolicy` rejects a save with messages such as `The drawing must contain exactly 16 rows.`, none of which currently reach the screen.

- [ ] **Step 8: Use it in `drawing-options.component.ts`**

Add the import:

```ts
import { apiErrorMessage } from '../../../../core/http/api-error';
```

Replace the shared `onError` handler:

```ts
    const onError = (err: unknown) => {
      console.error(err);
      this.saving = false;
      this.saveError = apiErrorMessage(err, 'Could not save the drawing.');
    };
```

- [ ] **Step 9: Run the suite and build**

```bash
npm test -- --watch=false --browsers=ChromeHeadless
npm run build
```

Expected: PASS, 17 specs; build succeeds.

- [ ] **Step 10: Verify against the running backend**

With the containers up and `npm start` running, submit each of these on `/register` and confirm the exact message appears:

| Input | Expected message |
|---|---|
| username `ab` | `Username must be at least 3 characters.` |
| username `a b` | `Username cannot contain whitespace.` |
| username of 51 characters | `Username must be at most 50 characters.` |
| password `short` | `Password must be at least 8 characters.` |
| an existing username | `Username is already taken.` |

Then stop the API container and submit again. Expected: `Could not create the account. Please try again.` — the fallback, because a connection failure carries no `title`.

- [ ] **Step 11: Hand the commit to the human**

Suggested command text — **do not run it**:

```bash
git add -A frontend/src/app
git commit -m "feat(frontend): display the API validation message on failure"
```

---

## Deliberately out of scope

Each is a real gap, excluded to keep this plan a behaviour-preserving refactor.

| Item | Why excluded |
|---|---|
| `DELETE /api/drawings/{id}` is unused by the frontend | Adding it is a new feature, not a move. It needs a `delete` method on `DrawingApi`, a confirmation flow, and gallery markup changes — the anchor in `gallery.component.html:19` wraps the whole card, so a delete button cannot be nested inside it and the markup needs restructuring. |
| `/create` has no `authGuard` | Pre-existing. Adding the guard changes who can reach the page. Worth doing, but as its own change with its own review. |
| `ExistingDrawingEditorWrapper` and `DrawingOptionsComponent` names | Renaming to `DrawingEditorPage` and `NewDrawingPage` would mix rename churn into move diffs. Do it as a follow-up once the moves have landed. |
| Frontend username validation | `register.component.ts` trims the username while the backend rejects whitespace outright. Task 7 makes the backend's message visible, which is the smaller fix. Mirroring the length and whitespace rules client-side is a separate change. |
| `drawing.store.ts` | No state is shared between drawing components today. A store with one consumer is an indirection with no payoff. |

---

## Appendix A — Tests

Written in TDD order: each is created and run before the implementation it covers. All use Karma + Jasmine, which are already configured in `angular.json` under the `test` target.

The Karma builder declares no `fileReplacements`, so tests resolve `src/environments/environment.ts` and see `apiBaseUrl === '/api'`. Tests assert against `environment.apiBaseUrl` rather than a literal, so they keep passing if the production value changes.

### Test 1 — `app.component.spec.ts` (Task 1)

```ts
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { AppComponent } from './app.component';

describe('AppComponent', () => {
  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [provideRouter([]), provideHttpClient()],
    }).compileComponents();
  });

  it('creates the component', () => {
    const fixture = TestBed.createComponent(AppComponent);

    expect(fixture.componentInstance).toBeTruthy();
  });

  it('reports a signed-out session when no token is stored', () => {
    const fixture = TestBed.createComponent(AppComponent);

    expect(fixture.componentInstance.authService.isLoggedIn()).toBeFalse();
  });
});
```

`localStorage.clear()` runs before `configureTestingModule`, so the `AuthService` field initializers read empty storage.

### Test 2 — `core/config/api-config.spec.ts` (Task 3)

```ts
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from './api-config';
import { environment } from '../../../environments/environment';

describe('API_BASE_URL', () => {
  it('resolves to the configured API base URL', () => {
    TestBed.configureTestingModule({});

    expect(TestBed.inject(API_BASE_URL)).toBe(environment.apiBaseUrl);
  });

  it('is overridable for tests', () => {
    TestBed.configureTestingModule({
      providers: [{ provide: API_BASE_URL, useValue: 'https://example.test/api' }],
    });

    expect(TestBed.inject(API_BASE_URL)).toBe('https://example.test/api');
  });
});
```

The second spec is what makes the token worth having over an exported constant.

### Test 3 — `core/auth/auth.service.spec.ts` (Task 4)

```ts
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { API_BASE_URL } from '../config/api-config';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: '/api' },
      ],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('starts signed out when storage is empty', () => {
    expect(service.isLoggedIn()).toBeFalse();
    expect(service.username()).toBeNull();
    expect(service.token()).toBeNull();
  });

  it('posts credentials to the login endpoint', () => {
    service.login({ username: 'smoketest', password: 'correct-horse' }).subscribe();

    const req = http.expectOne('/api/auth/login');

    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ username: 'smoketest', password: 'correct-horse' });
    req.flush({ username: 'smoketest', token: 'jwt-value' });
  });

  it('stores the session after a successful login', () => {
    service.login({ username: 'smoketest', password: 'correct-horse' }).subscribe();
    http.expectOne('/api/auth/login').flush({ username: 'smoketest', token: 'jwt-value' });

    expect(service.isLoggedIn()).toBeTrue();
    expect(service.username()).toBe('smoketest');
    expect(service.token()).toBe('jwt-value');
    expect(localStorage.getItem('pixelart_token')).toBe('jwt-value');
  });

  it('clears storage and signals on logout', () => {
    service.register({ username: 'smoketest', password: 'correct-horse' }).subscribe();
    http.expectOne('/api/auth/register').flush({ username: 'smoketest', token: 'jwt-value' });

    service.logout();

    expect(service.isLoggedIn()).toBeFalse();
    expect(localStorage.getItem('pixelart_token')).toBeNull();
    expect(localStorage.getItem('pixelart_username')).toBeNull();
  });
});
```

`http.verify()` in `afterEach` fails the spec if a request was issued that no expectation consumed, which catches an accidental extra call.

### Test 4 — `features/drawings/data-access/drawing.api.spec.ts` (Task 5)

```ts
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { API_BASE_URL } from '../../../core/config/api-config';
import { DrawingApi } from './drawing.api';
import { Drawing, DrawingInput } from '../models/drawing.model';

describe('DrawingApi', () => {
  let api: DrawingApi;
  let http: HttpTestingController;

  const input: DrawingInput = {
    name: 'checkerboard',
    width: 2,
    height: 2,
    pixels: [
      ['#000000', '#ffffff'],
      ['#ffffff', '#000000'],
    ],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: '/api' },
      ],
    });
    api = TestBed.inject(DrawingApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('requests the collection', () => {
    let received: Drawing[] | undefined;
    api.getAll().subscribe((drawings) => (received = drawings));

    const req = http.expectOne('/api/drawings');
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 1, ...input, createdAt: '2026-08-17T00:00:00Z' }]);

    expect(received?.length).toBe(1);
    expect(received?.[0].name).toBe('checkerboard');
  });

  it('requests one drawing by id', () => {
    api.getById(7).subscribe();

    const req = http.expectOne('/api/drawings/7');
    expect(req.request.method).toBe('GET');
    req.flush({ id: 7, ...input, createdAt: '2026-08-17T00:00:00Z' });
  });

  it('posts a new drawing', () => {
    api.create(input).subscribe();

    const req = http.expectOne('/api/drawings');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(input);
    req.flush({ id: 3, ...input, createdAt: '2026-08-17T00:00:00Z' });
  });

  it('puts an updated drawing', () => {
    api.update(3, input).subscribe();

    const req = http.expectOne('/api/drawings/3');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(input);
    req.flush(null);
  });
});
```

### Test 5 — `core/http/api-error.spec.ts` (Task 7)

```ts
import { HttpErrorResponse } from '@angular/common/http';
import { apiErrorMessage } from './api-error';

describe('apiErrorMessage', () => {
  const fallback = 'Could not save. Please try again.';

  it('returns the ProblemDetails title when present', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { status: 400, title: 'Username must be at least 3 characters.' },
    });

    expect(apiErrorMessage(error, fallback)).toBe('Username must be at least 3 characters.');
  });

  it('falls back when the body carries no title', () => {
    const error = new HttpErrorResponse({ status: 500, error: { status: 500 } });

    expect(apiErrorMessage(error, fallback)).toBe(fallback);
  });

  it('falls back when the title is blank', () => {
    const error = new HttpErrorResponse({ status: 400, error: { title: '   ' } });

    expect(apiErrorMessage(error, fallback)).toBe(fallback);
  });

  it('falls back when the body is not an object', () => {
    const error = new HttpErrorResponse({ status: 0, error: new ProgressEvent('error') });

    expect(apiErrorMessage(error, fallback)).toBe(fallback);
  });

  it('falls back for a non-HTTP error', () => {
    expect(apiErrorMessage(new Error('boom'), fallback)).toBe(fallback);
  });
});
```

The last two specs cover the cases that make the `instanceof` and `typeof` guards necessary: a transport failure where `error` is a `ProgressEvent`, and a thrown `Error` that never reached the network.

### Expected spec counts

| After task | Specs |
|---|---|
| 1 | 2 |
| 2 | 2 |
| 3 | 4 |
| 4 | 8 |
| 5 | 12 |
| 6 | 12 |
| 7 | 17 |
