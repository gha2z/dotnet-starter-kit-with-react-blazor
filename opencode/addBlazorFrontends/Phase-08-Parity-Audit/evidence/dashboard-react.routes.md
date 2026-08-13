# React Dashboard (`clients/dashboard`) — App Surface Evidence

Parity-audit evidence file. Every claim cites `clients/dashboard` path:line. The app is the read-only reference for the Blazor dashboard twin audit.

App root: `C:\repos\Project\dotnet-starter-kit-with-react-blazor-main\clients\dashboard` (shortened to `<dash>` below).

Router: `createBrowserRouter` in `src/routes.tsx:153`. All page chunks are code-split via `lazyNamed` (`src/routes.tsx:21-29`) wrapped in Suspense with a `RouteFallback` skeleton (`src/routes.tsx:124-151`). App tree: `ThemeProvider > QueryClientProvider > AuthProvider > CommandPaletteProvider > RouterProvider` (`src/App.tsx:17-30`); global Toaster (sonner) themed from ThemeProvider (`src/App.tsx:42-72`).

---

## 1. Route table

| Route | Component (lazy import) | Page purpose (1-line) | Evidence |
|---|---|---|---|
| `/login` | `LoginPage` → `src/pages/login.tsx` | Sign-in form (email/username + password, remember me, tenant hint) | `src/routes.tsx:155-158`, `src/routes.tsx:31` |
| `/forgot-password` | `ForgotPasswordPage` → `src/pages/auth/forgot-password.tsx` | Request password-reset email | `src/routes.tsx:159-163`, `src/routes.tsx:32-35` |
| `/reset-password` | `ResetPasswordPage` → `src/pages/auth/reset-password.tsx` | Set new password via token | `src/routes.tsx:164-168`, `src/routes.tsx:36-39` |
| `/confirm-email` | `ConfirmEmailPage` → `src/pages/auth/confirm-email.tsx` | Confirm email via token | `src/routes.tsx:169-173`, `src/routes.tsx:40-43` |
| `/tenant-deactivated` | `TenantDeactivatedPage` → `src/pages/tenant-deactivated.tsx` | Terminal state — tenant deactivated mid-session (outside shell; query-client routes here on 403) | `src/routes.tsx:174-182`, `src/routes.tsx:64-67` |
| `/impersonation-ended` | `ImpersonationEndedPage` → `src/pages/impersonation-ended.tsx` | Terminal state — impersonation grant revoked/token expired (outside shell; query-client routes here on 401) | `src/routes.tsx:183-192`, `src/routes.tsx:68-71` |
| `/` (index) | `OverviewPage` → `src/pages/overview.tsx` | Landing dashboard: greeting header, 4 stat cards, subscription/system-status rail, recent audits, usage bars, quick actions, live SSE feed, first-run setup panel | `src/routes.tsx:201`, `src/routes.tsx:44` |
| `/activity` | `ActivityPage` → `src/pages/activity.tsx` | Real-time SSE activity feed view | `src/routes.tsx:202`, `src/routes.tsx:45` |
| `/subscription` | `SubscriptionPage` → `src/pages/subscription.tsx` | Current plan, validity/expiry state, usage vs limits, overage | `src/routes.tsx:203`, `src/routes.tsx:51-54` |
| `/wallet` | `WalletPage` → `src/pages/wallet.tsx` | WhatsApp wallet balance/transactions | `src/routes.tsx:204`, `src/routes.tsx:55` |
| `/invoices` | `InvoicesPage` → `src/pages/invoices.tsx` | Invoice list with search/filter/paging | `src/routes.tsx:205`, `src/routes.tsx:46` |
| `/invoices/:id` | `InvoiceDetailPage` → `src/pages/invoice-detail.tsx` | Single invoice detail (lines, totals, status, actions) | `src/routes.tsx:206`, `src/routes.tsx:47-50` |
| `/system/health` | `HealthPage` → `src/pages/health.tsx` | Health check grids and endpoints | `src/routes.tsx:207`, `src/routes.tsx:91` |
| `/system/audits` | `AuditsPage` → `src/pages/audits.tsx` | Audit trail list + filters (type/severity/actor/date) | `src/routes.tsx:208`, `src/routes.tsx:92` |
| `/system/trash` | `TrashPage` → `src/pages/system/trash.tsx` | Trash can with per-resource tabs (products/brands/categories/users/roles/groups…) + restore/delete | `src/routes.tsx:209`, `src/routes.tsx:98` |
| `/system/sessions` | `SessionsPage` → `src/pages/system/sessions.tsx` | Active user sessions list + revoke | `src/routes.tsx:210`, `src/routes.tsx:99` |
| `/files` | `MyFilesPage` → `src/pages/files/my-files.tsx` | My/Shared files: dropzone upload, search, kind filter chips, preview dialog | `src/routes.tsx:211`, `src/routes.tsx:115` |
| `/chat` | `ChatPage` → `src/pages/chat/chat-page.tsx` | Chat shell: channel rail + auto-select first channel (`navigate(/chat/{first})`) | `src/routes.tsx:212`, `src/routes.tsx:116`, `src/pages/chat/chat-page.tsx:67-72` |
| `/chat/:channelId` | `ChatPage` → `src/pages/chat/chat-page.tsx` | Active channel pane: header, pinned bar, virtualized message list, typing indicator, composer | `src/routes.tsx:213`, `src/pages/chat/chat-page.tsx:99-104` |
| `/tickets` | `TicketsPage` → `src/pages/tickets/tickets.tsx` | Support ticket list with search/filter/paging | `src/routes.tsx:214`, `src/routes.tsx:93` |
| `/tickets/:ticketId` | `TicketDetailPage` → `src/pages/tickets/ticket-detail.tsx` | Ticket detail: conversation, status mutation, assignee, priority | `src/routes.tsx:215`, `src/routes.tsx:94-97` |
| `/identity` | redirect → `/identity/users` | Redirect stub | `src/routes.tsx:216` |
| `/identity/users` | `UsersPage` → `src/pages/identity/users.tsx` | User list: search, status/email/role filters, paging, Register-user dialog | `src/routes.tsx:217`, `src/routes.tsx:100` |
| `/identity/users/:userId` | `UserDetailPage` → `src/pages/identity/user-detail.tsx` | User profile: identity fields, roles, groups, session/actions | `src/routes.tsx:218`, `src/routes.tsx:101-104` |
| `/identity/roles` | `RolesPage` → `src/pages/identity/roles.tsx` | Role list + create-role dialog | `src/routes.tsx:219`, `src/routes.tsx:105` |
| `/identity/roles/:roleId` | `RoleDetailPage` → `src/pages/identity/role-detail.tsx` | Role editor: metadata + grouped permission toggler with search/filters/presets | `src/routes.tsx:220`, `src/routes.tsx:106-109` |
| `/identity/groups` | `GroupsPage` → `src/pages/identity/groups.tsx` | Group list + create dialog | `src/routes.tsx:221`, `src/routes.tsx:110` |
| `/identity/groups/:groupId` | `GroupDetailPage` → `src/pages/identity/group-detail.tsx` | Group detail: members, roles, settings | `src/routes.tsx:222`, `src/routes.tsx:111-114` |
| `/catalog` | redirect → `/catalog/brands` | Redirect stub | `src/routes.tsx:223` |
| `/catalog/brands` | `BrandsPage` → `src/pages/catalog/brands.tsx` | Brand list + CRUD dialogs | `src/routes.tsx:224`, `src/routes.tsx:56` |
| `/catalog/categories` | `CategoriesPage` → `src/pages/catalog/categories.tsx` | Category list + CRUD dialogs | `src/routes.tsx:225`, `src/routes.tsx:57` |
| `/catalog/products` | `ProductsPage` → `src/pages/catalog/products.tsx` | Product list: search, filters, brand/category comboboxes, paging | `src/routes.tsx:226`, `src/routes.tsx:58` |
| `/catalog/products/:productId` | `ProductDetailPage` → `src/pages/catalog/product-detail.tsx` | Product detail: pricing tiers, brands, categories, barcode, images, edit dialog | `src/routes.tsx:227-230`, `src/routes.tsx:59-62` |
| `/settings` | `SettingsLayout` (nested children below) | Settings shell with vertical sub-nav | `src/routes.tsx:231-243`, `src/routes.tsx:72-75` |
| `/settings` (index) | redirect → `settings/profile` | Redirect stub | `src/routes.tsx:235` |
| `/settings/profile` | `ProfileSettings` → `src/pages/settings/profile.tsx` | Profile form: name/email/phone/avatar, save, change-password section | `src/routes.tsx:236`, `src/routes.tsx:76` |
| `/settings/security` | `SecuritySettings` → `src/pages/settings/security.tsx` | Security: 2FA toggle, recovery codes, active sessions | `src/routes.tsx:237`, `src/routes.tsx:77` |
| `/settings/appearance` | `AppearanceSettings` → `src/pages/settings/appearance.tsx` | Theme light/dark/system + accent/font/density preferences | `src/routes.tsx:238`, `src/routes.tsx:82-85` |
| `/settings/branding` | `BrandingSettings` → `src/pages/settings/branding.tsx` | Tenant branding: name, logo upload, accent color, social links | `src/routes.tsx:239`, `src/routes.tsx:78-81` |
| `/settings/notifications` | `NotificationsSettings` → `src/pages/settings/notifications.tsx` | Notification preferences + preview items | `src/routes.tsx:240`, `src/routes.tsx:86-89` |
| `/settings/api-keys` | `ApiKeysSettings` → `src/pages/settings/api-keys.tsx` | API key CRUD: generate/revoke/copy keys | `src/routes.tsx:241`, `src/routes.tsx:90` |
| `*` (catch-all) | `NotFoundPage` → `src/pages/not-found.tsx` | Styled 404 with theme-aware art | `src/routes.tsx:248`, `src/routes.tsx:63` |

All protected routes are wrapped by `ProtectedRoute` (`src/routes.tsx:193-196`) and rendered inside `AppShell` (`src/routes.tsx:197-200`); every route (incl. standalone) declares an `errorElement: <RouteError />` (`src/routes.tsx:157` etc., component `src/components/route-error.tsx` imported at `src/routes.tsx:5`).

### Nav source (`src/components/layout/nav-data.ts`)

- **Top nav** (outside sections): Overview `/` (`nav-data.ts:56`); Chat `/chat` gated `Permissions.Chat.Channels.View` (`nav-data.ts:60`); My Files `/files` gated `Permissions.Files.Upload` (`nav-data.ts:61`); Settings `/settings` ungated bottom item (`nav-data.ts:64-66`).
- **Sections** (single-select accordion, `nav-data.ts:68-128`):
  - **Operations** (`nav-data.ts:70-81`): `/activity` Live activity (ungated `nav-data.ts:76`), `/subscription` + `/wallet` + `/invoices` all gated `Permissions.Billing.View` (`nav-data.ts:77-79`).
  - **Catalog** (`nav-data.ts:82-91`): `/catalog/products` `Permissions.Catalog.Products.View`; `/catalog/brands` `Permissions.Catalog.Brands.View`; `/catalog/categories` `Permissions.Catalog.Categories.View`.
  - **Helpdesk** (`nav-data.ts:92-99`): `/tickets` gated `Permissions.Tickets.View`.
  - **Identity** (`nav-data.ts:100-112`): `/identity/users` `Permissions.Users.Update`; `/identity/roles` `Permissions.Roles.Update`; `/identity/groups` `Permissions.Groups.Update` (comment: View perms are IsBasic, admin pages need Update `nav-data.ts:105-107`).
  - **System** (`nav-data.ts:113-127`): `/system/health` ungated; `/system/audits` `Permissions.AuditTrails.View`; `/system/sessions` `Permissions.Sessions.ViewAll`; `/system/trash` `anyPerm: ALL_TRASH_PERMISSIONS` (multi-perm OR gate `nav-data.ts:125`).
- Gate logic: `perm` AND any `anyPerm`, else hidden — `isNavItemVisible` `nav-data.ts:132-136`, `visibleSections` `nav-data.ts:139-143`, `visibleItems` `nav-data.ts:146-148`, section-highlight matcher `findSectionForPath` `nav-data.ts:151-167`.

---

## 2. Auth flows

### Login (`src/pages/login.tsx`)
- Multi-tenant form: **Tenant / Email / Password** fields, `noValidate` native form, show/hide password toggle (`login.tsx:114-190`). Tenant defaults to `env.defaultTenant` (`login.tsx:35`).
- Error band (API problem-detail extraction) `login.tsx:192-206`; submit spinner `login.tsx:214-224`; button disabled until all three fields present `login.tsx:211`.
- Redirects authenticated users to `from` location state or `/` (`login.tsx:49-51`, `login.tsx:31`); post-login `navigate(from)` (`login.tsx:58`).
- Inactivity signed-out notice surfaced once via `consumeSignedOutReason()` (`login.tsx:43-47`).
- **Demo accounts** (runtime `env.demoMode`, staging-on/prod-off): "Sign in with a demo account" button `login.tsx:230-241`, `DemoAccountsDialog` (`login.tsx:244-246`, component `src/components/auth/demo-accounts-dialog.tsx`), account list `src/pages/login.demo-accounts.ts` (`login.tsx:14`).
- "Forgot?" link → `/forgot-password` (`login.tsx:162-167`).

### Forgot password (`src/pages/auth/forgot-password.tsx`)
- Collects (email, tenant) `forgot-password.tsx:135-182`; submits `requestPasswordReset` (`forgot-password.tsx:39-43`).
- Anti-enumeration: always renders the same "check your inbox" success state on 2xx (`forgot-password.tsx:78-125`, comment `forgot-password.tsx:27-31`); success copy mentions 30-minute expiry `forgot-password.tsx:95`.
- Redirects authenticated users to `/` (`forgot-password.tsx:54-56`).

### Reset password (`src/pages/auth/reset-password.tsx`)
- Step 2 — reads `token/email/tenant` query params (`reset-password.tsx:74-81`); malformed-link state when any missing (`reset-password.tsx:119`, `reset-password.tsx:148-173`).
- New-password + confirm with client-side strength meter (`scorePassword` → weak/fair/strong, `reset-password.tsx:37-51`, bar `reset-password.tsx:218-233`) and match indicator (`reset-password.tsx:267-284`).
- On success: toast + redirect to `/login` (`reset-password.tsx:94-99`); min length 8 enforced client-side (`reset-password.tsx:127-129`).

### Confirm email (`src/pages/auth/confirm-email.tsx`)
- Reads `userId/code/tenant` query params (`confirm-email.tsx:28-30`); fires `confirmEmail` once on mount (`confirm-email.tsx:39-73`).
- Three states: loading spinner / success (green check + Continue to sign in) / error (red + request-new-link affordances) (`confirm-email.tsx:86-163`).
- No auto-redirect on success (deliberate) (`confirm-email.tsx:14-18`).

### Impersonation banner (`src/components/layout/impersonation-banner.tsx`)
- Rendered in the app shell; displayed while the session is an impersonated operator session (details in §3 layout).

### Terminal states
- `/tenant-deactivated`: calm centered card, "Tenant deactivated", single "Back to sign in" action that calls `logout()` then navigates (`tenant-deactivated.tsx:22-31`, copy `tenant-deactivated.tsx:56-59`, button `tenant-deactivated.tsx:62-69`). Reachable only via global query error hook routing (comment `tenant-deactivated.tsx:10-14`).
- `/impersonation-ended`: same vocabulary, "Impersonation ended" (`impersonation-ended.tsx:39-44`, copy `impersonation-ended.tsx:69-72`); dev-only JwtBearer `reason` from router state rendering (`impersonation-ended.tsx:34-37`, `impersonation-ended.tsx:86-90`).
- 404 `/not-found`: "Page not found" card + requested path display + Back home / Open command palette / Go back actions (`not-found.tsx:41-87`); uses `useCommandPalette().setOpen` (`not-found.tsx:18`, `not-found.tsx:71`).

---

## 3. Layout & chrome

### App shell (`src/components/layout/app-shell.tsx`)
- Composition: `SseProvider` → `RealtimeProvider` → `MobileNavProvider`, then `ImpersonationBanner` + `ExpiryBanner` above the `Sidebar`/`Topbar`/main-`Outlet` row (`app-shell.tsx:38-53`).
- Skip-to-content link (first focusable, `#main`) `app-shell.tsx:25-36`.
- Mounted at root: `MobileNavRoot` (`app-shell.tsx:59`), `ChatGlobalNotifier` (toasts ChatMessageCreated via SignalR when off-channel) `app-shell.tsx:65`, `CommandPaletteRoot` `app-shell.tsx:69`, `InactivityGuard` `app-shell.tsx:72`.

### Sidebar (`src/components/layout/sidebar.tsx`)
- Sections: brand row → nav (top items, section accordions, bottom items) → footer (`sidebar.tsx:84-167`).
- **Collapse** persistence via localStorage key `fsh.sidebar.collapsed` (`sidebar.tsx:20`); animated width 220px ↔ 52px (`sidebar.tsx:81`, `sidebar.tsx:231-256` flat icon stack when collapsed).
- **Permission-gated** nav items: `visibleItems(topNavTop, perms)`, `visibleSections(perms)`, `visibleItems(topNavBottom, perms)` where `perms = user?.permissions` (`sidebar.tsx:193-197`).
- Single-select accordion of sections; auto-opens the section owning the current route (`sidebar.tsx:53-71`); open panel animates `grid-template-rows 0fr↔1fr` (`sidebar.tsx:348-355`).
- NavItemLink: active = primary-soft bg + 2px slide-in brand bar (`sidebar.tsx:413-429`); collapsed hover tooltip + `title`/`aria-label` fallback (`sidebar.tsx:442-455`).
- Footer: version text `v0.1 · dashboard` / expand button (`sidebar.tsx:138-166`).
- `SidebarNavBody` exported and shared by the mobile drawer (`sidebar.tsx:178-266`).

### Nav data (`src/components/layout/nav-data.ts`) — single source of truth for sidebar **and** command palette (see §1 route-table notes). Sections/items carry `perms` arrays (e.g. Catalog → Products `Permissions.Products.View`, Identity users/roles/groups, System health/audits/trash/sessions; files/chat/wallet/subscription are in top-level groups).

### Topbar (`src/components/layout/topbar.tsx`)
- `MobileNavTrigger` (hamburger, `md:hidden`) `topbar.tsx:199`; mobile search button opens palette `topbar.tsx:207-219`; desktop `Search ⌘K` chip `topbar.tsx:223-240`.
- `ChatUnreadBadge` (brand-primary chip) `topbar.tsx:244`; `NotificationBell` `topbar.tsx:247`.
- **User dropdown** (`modal={false}` due to Radix race) `topbar.tsx:259-362`: square avatar tile (photo/initials from `getMyProfile` query `["identity","me"]`) `topbar.tsx:66-91, 153-157`; name + tenant caption `topbar.tsx:275-282`; **SSE status dot + event counter** (connected/error/connecting/reconnecting/idle) `topbar.tsx:170-187, 309-321`; Theme submenu Light/Dark/System with checks `topbar.tsx:326-334`; Account quick links Profile `/settings/profile`, Settings `/settings`, API keys `/settings/api-keys` `topbar.tsx:338-346`; Sign out action `topbar.tsx:352-360`.
- Sign-out confirmation Dialog with avatar/name/tenant summary `topbar.tsx:365-411`.
- Topbar profile query shares the identity-me key with the Profile page so avatar invalidates live (`topbar.tsx:151-157`).

### Notification bell (`src/components/notifications/notification-bell.tsx`)
- Badge with unread count from `["notifications","unread-count"]` (staleTime 30s) `notification-bell.tsx:33-37`; inbox `listNotifications({pageSize:30})` fetched on open `notification-bell.tsx:39-44`.
- **Realtime live patch**: `useRealtimeEvent("NotificationCreated")` increments badge + prepends row without refetch `notification-bell.tsx:49-58`.
- Mark-all-read and per-row mark-read mutations `notification-bell.tsx:60-74`; row shows type icon (currently chat.mention), title, body (line-clamp-2), relative time, unread dot `notification-bell.tsx:192-256`.
- Footer: `RealtimeStatusPill` (announce) + Settings link → `/settings/notifications` `notification-bell.tsx:174-186`.

### Command palette
- `CommandPaletteRoot` mounted in app-shell (`app-shell.tsx:69`); opened via `useCommandPalette().setOpen` from topbar chip, mobile search, not-found page `not-found.tsx:71`; ⌘K shortcut (topbar kbd `topbar.tsx:237-239`).

### Inactivity guard
- `InactivityGuard` component mounts warning modal + countdown, auto-logout, signed-in only (`app-shell.tsx:72`; component `src/components/auth/inactivity-guard.tsx`).

### Impersonation banner (`src/components/layout/impersonation-banner.tsx`)
- Rendered always in shell; returns null when `!impersonation` (`impersonation-banner.tsx:61`). Reads `act_*` claims via `useAuth().impersonation` `impersonation-banner.tsx:28`.
- Tone scaling: same-tenant = warning (amber); cross-tenant = destructive (red) + 2px left ribbon `impersonation-banner.tsx:67-98`.
- Subject name, tenant chips (actor → target when cross-tenant), operator attribution `impersonation-banner.tsx:123-160`; "End impersonation" button → `stopImpersonation()` → navigate `/login` (signedOut) or `/` `impersonation-banner.tsx:41-59`.

### Expiry banner (`src/components/layout/expiry-banner.tsx`)
- Global subscription health bar; `getMyStatus()` query `["tenant","me","status"]` `expiry-banner.tsx:86-92`; views: grace (warning, dismissible), expired (destructive, **pinned — no dismiss**) `expiry-banner.tsx:101`, nearing ≤7 days (info) `expiry-banner.tsx:61-66`.
- Dismissal per browser session (state only) `expiry-banner.tsx:84`.

### Mobile nav (`src/components/layout/mobile-nav.tsx`)
- Provider + Sheet (left) + trigger; reuses `SidebarNavBody` for single source of truth `mobile-nav.tsx:63-125`; auto-closes on route change `mobile-nav.tsx:79-83`.

### Shared list chrome (`src/components/list/index.ts` presumably exports `EntityPageHeader`, `ErrorBand`, `Field`, etc.)
- `EntityPageHeader` used by settings layout `settings-layout.tsx:69-86`.

---

## 4. Theming

### Theme provider (`src/components/theme/theme-provider.tsx`)
- **Storage key `fsh.theme`** for mode; values `light | dark | system` (`theme-provider.tsx:28`, `theme-provider.tsx:51`). Default `system` (`theme-provider.tsx:56-64`).
- Resolved theme toggles `.dark` class on `<html>` (`theme-provider.tsx:80-82`).
- Crossfade on switch: View Transitions API when available, else scoped `.theme-switching` blanket transition, instant when reduced-motion/cold-load `theme-provider.tsx:104-139`.
- System-mode live follow (`prefers-color-scheme` listener) `theme-provider.tsx:265-277`.
- **Font**: `fsh.font` storage key, applied as `--font-sans`; default `figtree` (`theme-provider.tsx:141-145`, `appearance-options.ts:98`).
- **Accent**: `fsh.accent` storage key (default `rose`); applied by `accent-{id}` class on `<html>`; preset ids: rose, indigo, violet, sky, emerald, amber; `accent-custom` id + `fsh.accent.custom` storing `{h, c}` inline `--brand-50..950` stops (`theme-provider.tsx:163-189`, `appearance-options.ts:108-124`, `appearance-options.ts:200-210`).
- **Density**: `fsh.density` (`comfortable|compact`), `density-compact` class (`theme-provider.tsx:206-209`, `appearance-options.ts:126-128`).
- **Reduced motion**: `fsh.reduce-motion` storage + `.reduce-motion` class override `theme-provider.tsx:211-223`.
- Fonts list (12 families incl. lazy-loaded Google fonts on Appearance page) `appearance-options.ts:23-96`, lazy loader `appearance-options.ts:172-198`.

### Appearance page (`src/pages/settings/appearance.tsx`)
- Theme 3-swatch cards (Light/System/Dark) `appearance.tsx:74-99`; Accent row of 6 preset cards + Custom card `appearance.tsx:112-128`; Font cards `appearance.tsx:149-158`; Density switch `appearance.tsx:169-178`; Motion switch `appearance.tsx:193-202`.
- **CustomAccentDialog**: hue ribbon (0-360 range input) + saturation slider (50-130%), live brand-ladder strip + preview chrome rendered in candidate accent; Apply commits via `setCustomAccent` + `setAccent("custom")` `appearance.tsx:402-569`.

---

## 5. Per-page feature inventory

### `/` Overview (`src/pages/overview.tsx`, 1293 lines)
- **Queries**: `["billing","usage"]` → `getUsageSnapshots()` (staleTime 60s) `overview.tsx:947-951`; `["billing","subscription","me"]` → `getMySubscription()` `overview.tsx:953-957`; `["tenant","me","status"]` → `getMyStatus()` `overview.tsx:963-967`; recent audits `["audits","recent","overview"]` (24h window, pageSize 5, staleTime 30s) `overview.tsx:563-575`. Manual refresh refetches all three `overview.tsx:999-1004`.
- **SSE realtime**: `useSseStatus()` + `useSseEvents()` (`overview.tsx:944-945`); live feed shows last 5 events with clock + tone-tinted Badge (`eventTone`) `overview.tsx:737-766`; System status card: SSE state (live/offline) + event count `overview.tsx:471-540`.
- **KPI stat cards** (4-up `StatCard`): Plan (planKey/status), Valid for (days-left from expiryState; Expired→destructive), Resources (count + avg utilization + overage), Live events (SSE count; dot colored by status; href → /activity) `overview.tsx:200-262, 1141-1197`.
- **Widgets** (`EntityDetailSection`): Subscription summary card (plan, start/end, term progress bar) `overview.tsx:349-465`; System status `overview.tsx:1206-1217`; Recent audits (5 rows, severity color, type icon, click→/system/audits) `overview.tsx:562-651`; Usage by resource (label, used/limit, %, animated bar; overage Badge; ≥80% → warning, overage → destructive) `overview.tsx:268-309`; Quick actions 4 tiles `overview.tsx:665-730`; Live feed `overview.tsx:737-766`.
- **First-run panel**: shows when tenant has no active subscription and not dismissed; 4 numbered setup tiles; dismiss persisted per-tenant in localStorage `fsh.firstrun.dismissed:{tenant}` `overview.tsx:774-936, 979-983, 1100-1106`.
- **States**: skeletons (plan/usage rows/audits), error empty states (`UsageEmpty`, subscription error), "No active subscription" + CTA `overview.tsx:311-342, 368-397`.
- **Permission gating**: none on this page itself (all endpoints isBasic).

### `/settings` shell (`src/pages/settings/settings-layout.tsx`)
- Layout: `EntityPageHeader` (title "Settings · {section}") + numbered editorial left nav (desktop) / horizontal scroll chips (mobile) + `Outlet` `settings-layout.tsx:64-204`.
- Tabs: Profile, Security, Appearance, Branding (`perm: Permissions.Tenants.UpdateTheme`), Notifications, API keys `settings-layout.tsx:31-41`. Tabs filtered by `user.permissions` `settings-layout.tsx:53-57`. Index redirect → `/settings/profile` (`routes.tsx:235`).
- `SettingsSection` card building block (header/footer bars) exported for use by tabs `settings-layout.tsx:212-257`.

### `/settings/profile` (`src/pages/settings/profile.tsx`)
- **Form**: first/last name + phone (plain `<Input>`, no zod — manual state + seed-once effect from `["identity","me"]` profile query) `profile.tsx:27-47, 103-197`. Save → `updateMyProfile` mutation; dirty tracking gates Save; Reset button `profile.tsx:49-80, 134-147`. Email field read-only.
- **Photo**: `ImageInput` (ownerType User, circle) + `setProfileImage(url)` mutation `profile.tsx:88-128`.
- Subject identifier read-only code display `profile.tsx:200-208`.
- States: inline alert band when profile query errors `profile.tsx:105-115`.
- Shared query key `["identity","me"]` invalidated on save so topbar avatar updates (`profile.tsx:14, 58`).

### `/settings/security` (`src/pages/settings/security.tsx`, 822 lines)
- **Password card** → ChangePasswordDialog (current/new/confirm, show/hide PasswordField, strength meter 0-4 tiers, client checks: ≥8 chars, match, differs) `security.tsx:269-532`.
- **Two-factor card**: enabled badge from `profile.twoFactorEnabled` (`["identity","me"]`) `security.tsx:98-99, 538-567`; enroll flow: `enrollTwoFactor` → QR (rendered as themed inline SVG via `qrcode`, currentColor) + shared key copy + 6-digit verify (`verifyEnrollTwoFactor`) `security.tsx:569-748`; disable flow requires re-auth password (`disableTwoFactor`) `security.tsx:750-800`.
- **Active sessions**: `["identity","sessions","me"]` → `getMySessions()` (staleTime 30s) `security.tsx:101-105`; sorted with current session first `security.tsx:107-116`; list rows: browser/OS/device-icon, IP, last activity, expires, "this device"/"revoked" badges; per-row Revoke (`revokeSession`) + "Sign out everywhere else" (`revokeAllOtherSessions`) `security.tsx:118-143, 203-258`. Skeleton/empty/error states `security.tsx:145-150, 186-201`.

### `/settings/appearance` — see §4 Theming (`src/pages/settings/appearance.tsx`, 570 lines): Theme swatches, Accent cards + CustomAccentDialog (hue ribbon 0-360 + saturation 50-130% + live brand ladder + preview chrome), Font cards (12, lazy-loaded), Density switch, Motion switch `appearance.tsx:63-205`.

### `/settings/branding` (`src/pages/settings/branding.tsx`, 483 lines)
- Query `["tenant","theme"]` → `getTenantTheme()` with background refetch disabled (won't clobber edits) `branding.tsx:42-51`; draft state seeded once `branding.tsx:53-62`.
- Draft edit: light/dark `PaletteDto` (9 tokens: primary, secondary, tertiary, background, surface, error, warning, success, info) via hex Input + native color input; per-palette reset to defaults `branding.tsx:189-287`.
- Brand assets: logoUrl / logoDarkUrl / faviconUrl URL fields + delete flags + thumbnail preview `branding.tsx:293-379`.
- Live `ThemePreview` section rendering buttons/pills in the chosen palette `branding.tsx:385-475`.
- Save (`updateTenantTheme`) / Reset (`resetTenantTheme`) mutations; dirty/unsaved/default badges `branding.tsx:64-80, 113-152`. Loading spinner + `ErrorBand` fallback `branding.tsx:82-99`.
- Route-level perm gate `Permissions.Tenants.UpdateTheme` (`settings-layout.tsx:38`).

### `/settings/notifications` (`src/pages/notifications.tsx`) — **placeholder**; explains prefs not tunable, button opens the topbar bell (`notifications.tsx:12-49`).

### `/settings/api-keys` (`src/pages/settings/api-keys.tsx`) — **placeholder**; "not available yet", roadmap link (`api-keys.tsx:12-50`).

### `/activity` Live activity (`src/pages/activity.tsx`, 193 lines)
- **Pure SSE streaming page** (no REST): `useSseEvents()` capped at 200 events `activity.tsx:73`; header shows live count `useSseStatus().eventCount` + streaming/offline/state badge `activity.tsx:70-92`.
- Desktop `EntityListCard` table: columns Action (type badge + payload summary) / Entity (extracted id field) / Time (HH:mm:ss) `activity.tsx:136-147`; `role="log" aria-live="polite"` for screen readers `activity.tsx:118-134`.
- Mobile card list variant `activity.tsx:116-126, 157-171`.
- `eventTone` heuristic (fail/error/revoke→danger, warn/retry→warning, login/issued/created→success, token/auth→info) `activity.tsx:41-48`; `payloadSummary` JSON-stringify fallback `activity.tsx:26-36`.
- Empty state depends on stream connectedness `activity.tsx:94-104`. No pagination/search/filters (pure stream cap 200).

### `/subscription` (`src/pages/subscription.tsx`, 540 lines)
- **Queries**: `["tenant","me","status"]` (`getMyStatus`), `["billing","subscriptions","me"]` (`getMySubscription`), `["billing","usage"]`, `["billing","invoices","me",{page:1,size:5}]` — all staleTime 60s `subscription.tsx:104-126`.
- Left rail: **Plan card** (planName, status badge, started/ends, "operator-driven" note) `subscription.tsx:162-168, 218-287`; **Validity card** (EntityStatusBadge Active/In-grace/Expired, valid-until, grace-ends) `subscription.tsx:170-172, 293-346`.
- Right: **Usage by resource** list (use/limit/%, bar; overage badge; ≥80% warning, overage destructive) `subscription.tsx:352-454`; **Recent invoices** (top 5, links to `/invoices/:id`) `subscription.tsx:460-540`.
- `ErrorBand` for status/subscription errors `subscription.tsx:140-157`; skeletons/empty states throughout. No forms — plan changes are operator-driven (`subscription.tsx:281-284`).

### `/wallet` WhatsApp wallet (`src/pages/wallet.tsx`, 410 lines)
- **Queries**: `["billing","wallet","me"]` (`getMyWallet`, staleTime 30s) `wallet.tsx:76-80`; `["billing","topup-requests","me",{page,size:20}]` with `keepPreviousData` `wallet.tsx:82-87`.
- **BalanceCard**: big formatted balance; low-balance warning if ≤ `LOW_BALANCE_THRESHOLD` 10 (empty vs running-low copy) `wallet.tsx:42, 186-217`.
- **Top-up form** (create): amount (number input, >0 validation) + optional note textarea (max 1000); submit `createTopupRequest` mutation; pass data via `mutate(arg)` (per rules) `wallet.tsx:223-316`; on success toast + invalidates wallet + requests queries.
- **Table** (`EntityListCard`): columns Requested (date+note) / Amount / Status / Invoice (link → `/invoices/:id`) `wallet.tsx:142-156, 361-410`; mobile card list `wallet.tsx:322-359`; `EntityPager` (20/page) `wallet.tsx:158-165`.
- Status tones: Pending→warning, Invoiced→info, Completed→success, Rejected/Cancelled→default/danger `wallet.tsx:53-67`.
- Error bands, loading skeleton (`EntityListLoading`), empty `EntityEmpty` states `wallet.tsx:93-96, 122-131`.

### `/invoices` (`src/pages/invoices.tsx`, 331 lines)
- **Query**: `["billing","invoices","me",{page,pageSize:20}]` + `keepPreviousData` `invoices.tsx:74-79`.
- `EntitySearch` text box — **client-side filter of current page** only (backend has no search param); matches invoice number, status, or period; pagination suppressed while searching `invoices.tsx:85-112, 121, 210-219`.
- Desktop table: Invoice # (number + period) / Customer (tenantId) / Amount / Status / Due date (warning when Issued, success when Paid) `invoices.tsx:193-208, 268-330`; mobile cards `invoices.tsx:231-266`; `EntityPager` `invoices.tsx:211-219`.
- `EntityPageHeader` total count; counts text (shown/ page X of Y) `invoices.tsx:127-133, 168-183`; empty state with "Clear search" action when searching `invoices.tsx:145-165`.

### `/invoices/:id` (`src/pages/invoice-detail.tsx`, 396 lines)
- **Query**: `["billing","invoices",id]` → `getMyInvoice(id)`, enabled when id present `invoice-detail.tsx:52-56`.
- `EntityDetailBack` to `/invoices` `invoice-detail.tsx:62`; header with gradient stripe, invoice number, status + purpose badges, subtotal, **Download PDF** (`downloadInvoicePdf`) `invoice-detail.tsx:115-184, 118-128`.
- Line items table: Description (kind badge, resource) / Qty / Unit price / Amount + Total row `invoice-detail.tsx:190-284`.
- Details dl: status, currency, period, created, issued/due/paid/voided dates (tone-coded), period start/end `invoice-detail.tsx:290-350`; Notes section when present `invoice-detail.tsx:102-108`.
- States: `ErrorBand`, `DetailSkeleton`, `NotFoundPanel` (not your tenant or bad link) `invoice-detail.tsx:64-77, 356-395`.

### `/system/health` (`src/pages/health.tsx`, 751 lines)
- **Polling**: `["health","ready"]` → `getReadiness()` with `refetchInterval: 10_000` when auto-refresh on (toggleable Live/Paused), no window-focus refetch, retry 1 `health.tsx:154-166`.
- Client-side **history ring buffer** (24 ticks, session-only) + `HistoryPips` pip-bar visualization; forced re-render every 5s to keep "x seconds ago" fresh `health.tsx:105-120, 173-177, 413-439`.
- **HeroPanel**: tone-tinted radial glow, status badge, HTTP code, headline copy per status, vitals (checks passing X/Y, round-trip, slowest, last poll) `health.tsx:286-406`.
- **Dependency list**: collapsible rows (self/redis/hangfire/postgres/storage/http icon registry `iconForCheck`), latency readout + status word + chevron; expanded panel shows latency-budget bar (sqrt-scaled / 500ms) + detail key/value table (`details`, first 6, "+N more") `health.tsx:449-657`.
- States: `HeroSkeleton`, `ChecksSkeleton`, `ErrorPanel` (role=alert; endpoint unreachable), `EmptyChecks` ("modules register via AddHealthChecks()") `health.tsx:663-751`.
- Realtime: **polling only (no SSE)** for health `health.tsx:162`.

### `/system/audits` Audit trail (`src/pages/audits.tsx`, 1420 lines)
- **Server-side queries**: `["audits","list",filters,window]` → `listAudits` (pageSize 25, from/to window, eventType, excludeEventType Activity when hiding system, severity, tagsMask, source, userId, correlationId, traceId, search; `keepPreviousData`, staleTime 5s) `audits.tsx:66, 202-230`; `["audits","summary",window]` → `getAuditSummary` (staleTime 30s) `audits.tsx:232-237`.
- **Range presets**: 24h/7d/30d/90d pill toggle (max window 90d noted) `audits.tsx:73-86`.
- **Search**: `EntitySearch` + inline input, **debounced 300ms**, resets page `audits.tsx:189-198, 306-310, 747-765`.
- **Advanced filters** (collapsible card): Source, User ID, Correlation, Trace text fields; Tags bitmask chips; Type chips (Activity/Security/EntityChange/Exception); Severity chips (Information/Warning/Error/Critical, tone-tinted); "Hide system activity" `Switch` (default on → excludes `AuditEventType.Activity` server-side unless explicitly filtered to Activity) `audits.tsx:707-906, 813-846, 850-901`; active-chip count badge on Advanced button; Reset `audits.tsx:802-810, 266-269`.
- **SummaryStrip card**: window total, stacked event-type bar (activity/entity/security/exception colors), severity dots, top-5 sources chips `audits.tsx:583-701`.
- **Desktop list** (`EntityListCard`, cols `Actor/Event/Severity/Timestamp`): initials avatar, plain-English `auditPredicate`, source endpoint, tag chips (max 2 + "+N"), severity badge, dense ISO timestamp + chevron `audits.tsx:379-394, 493-575`. Mobile cards `audits.tsx:368-376, 431-487`.
- **Detail drawer** (right slide-in Dialog variant, 640px): header w/ severity gradient, source, tags; Identity grid (tenant/user/userid/source); Trace grid (trace/span/correlation/request IDs) + "All by correlation"/"All by trace" jump buttons that re-set filters `audits.tsx:978-1228`; **Related events timeline** (correlation-linked, capped 12, click-to-swap drawer) `audits.tsx:1279-1399`; Payload JSON pre with Copy button `audits.tsx:1194-1202, 1401-1420`; Pipeline occurred/received/sink-delay `audits.tsx:1204-1225`.
- States: `EntityListLoading`, error alert role, `EntityEmpty` with Reset filters action, drawer skeleton/error `audits.tsx:333-355, 1230-1258`.
- Realtime: **no SSE on this page** — manual Refresh + refetch buttons `audits.tsx:289-302`.

### `/system/trash` Recycle bin (`src/pages/system/trash.tsx`, 691 lines)
- **5 resource tabs**: Products/Brands/Categories/Tickets/Files, each **permission-gated** (TRASH_TAB_PERMISSIONS from `trash-permissions.ts`); tabs filtered by `user.permissions`; zero-visible → empty "No recycle bins available" state `trash.tsx:73-110, 126-131`.
- Each tab: `listTrashed*` query (pageSize 20) + `restore*` mutation that invalidates both trash and parent-list query keys `trash.tsx:187-385`.
- Shared `TrashShell` list: desktop columns Entity / Deleted by / Deleted at / Actions; mobile cards; `EntityPager` `trash.tsx:408-520`.
- **RestoreConfirmDialog**: title/description confirm → `restore*` with spinner + toast `trash.tsx:526-562`. Empty per-tab state links back to the source list `trash.tsx:443-459`. No search/filter/sort.

### `/system/sessions` (`src/pages/system/sessions.tsx`, 490 lines)
- Admin tenant-wide sessions console. Query `["identity","sessions","tenant",{search,includeInactive,page}]` → `getTenantSessions` (pageSize 50), **refetchInterval 30s** (live), `keepPreviousData` `sessions.tsx:67-83`; manual Refresh button `sessions.tsx:143-151`.
- `EntitySearch` (debounced 300ms) `sessions.tsx:55-63, 154-158`; `EntityFilterPill` Live-only / Include inactive `sessions.tsx:162-170`; inline stats (active/distinct users/mobile) `sessions.tsx:87-96, 171-179`.
- Desktop: User (avatar, "You"/"Inactive" badges, email) / Device (icon, browser+OS) / IP / Last activity / Actions (Revoke + "All devices" `adminRevokeAllUserSessions`) `sessions.tsx:236-260, 388-489`; mobile cards `sessions.tsx:217-232, 291-382`.
- Revoke mutations per-session and per-user (`adminRevokeUserSessionById`, `adminRevokeAllUserSessions`) with toasts + invalidation `sessions.tsx:98-129`.
- States: loading skeleton, empty (search-aware), error alert role `sessions.tsx:183-207, 273-281`.

### `/catalog/products` (`src/pages/catalog/products.tsx`, 1409 lines)
- **Query**: `["catalog","products",{search,brandId,categoryId,isActive,page,size:25}]` → `searchProducts` w/ sort `createdAtUtc desc`, `keepPreviousData` `products.tsx:197-222`; filter lookup queries `searchBrands`/`searchCategories` (pageSize 200, staleTime 60s) `products.tsx:224-233`.
- **Search** (inline, debounced 250ms) `products.tsx:185-191, 272-296`; **filters**: Brand/Category `Combobox` (variant="filter", searchable, clearable), Active/All/Hidden pill `products.tsx:87-169, 299-308`; errors reset page `products.tsx:193-195`.
- **Desktop table**: Product (image+name+slug…+"Hidden" badge) / SKU / Brand badge+category / Price (click→price dialog) + stock chip (click→stock dialog) / row actions (edit, delete hover-reveal, chevron) `products.tsx:348-372, 500-610`; rows link to `/catalog/products/:id` `products.tsx:530-547, 424-425`; mobile cards w/ edit button `products.tsx:335-345, 412-493`; `EntityPager` `products.tsx:374-381`.
- **Dialogs** (all Radix Dialog + plain state forms — no zod/react-hook-form, manual validity checks):
  - Create/Edit `ProductEditorDialog`: name, SKU (fixed after creation), brand + category comboboxes, price/currency/stock (create only), description, visibility Switch (edit only) `products.tsx:811-1081`.
  - **Change price** `PriceDialog`: was/becomes comparison (tone-coded delta), emits `ProductPriceChanged` domain event `products.tsx:1087-1213`.
  - **Adjust stock** `StockDialog`: delta stepper (+/-), current→becomes, "cannot go negative" guard, emits `ProductStockAdjusted` event `products.tsx:1215-1350`.
  - **Delete** `DeleteProductDialog`: warn, soft-delete → trash `products.tsx:1356-1409`.
- Mutations invalidate `["catalog","products"]` + `["trash","products"]`; sonner toasts. States: `LoadingList`, empty w/ Clear filters or Add product, error alert role.
- **Stock chip tones**: 0→danger, <10 (LOW_STOCK)→warning `products.tsx:73, 709-741`.

### `/catalog/brands` (`src/pages/catalog/brands.tsx`, 632 lines)
- **Query**: `["catalog","brands",{search,page,size:20}]` → `searchBrands` sorted createdAtUtc desc, `keepPreviousData`, debounced search 250ms `brands.tsx:82-105, 130-134`.
- Desktop list (Brand avatar+name+desc / Slug / Created / actions edit+delete+chevron) `brands.tsx:190-207, 290-362`; mobile `EntityMobileCard` `brands.tsx:245-284`; `EntityPager` `brands.tsx:209-216`.
- **BrandEditorDialog** (create/edit): name + **auto slug preview** (`slugify` helper), description, logo URL `brands.tsx:409-569`; DeleteBrandDialog (warns products referencing brand need reassignment) `brands.tsx:575-632`. Mutations invalidate `["catalog","brands"]` + `["trash","brands"]`.
- States: `EntityListLoading`, `EntityEmpty` (search vs none), error alert.

### `/catalog/categories` (`src/pages/catalog/categories.tsx`, 720 lines)
- **Queries**: `["catalog","categories",{search,page,size:50}]` → `searchCategories` sorted name asc, `keepPreviousData` `categories.tsx:116-131`; **category tree** `["catalog","categories","tree"]` → `getCategoryTree` (staleTime 30s) used to resolve parent names + parent options `categories.tsx:133-137, 142-152`.
- Desktop list shows parent ("under {parent}" with ChevronsRight, or "root" GitBranch tag) + slug + created `categories.tsx:240-261, 361-450`; mobile cards `categories.tsx:300-355`; `EntityPager` `categories.tsx:263-270`.
- **CategoryEditorDialog**: name, auto slug preview, **Parent combobox with flattened tree** (depth-indented `↳` prefix, excludes self + descendants, empty option "No parent (root)") `categories.tsx:456-656`; DeleteCategoryDialog warns about child categories `categories.tsx:662-720`. Mutations invalidate `["catalog","categories"]` + `["trash","categories"]`.

### `/identity/users` (`src/pages/identity/users.tsx`, 571 lines)
- **Query**: `["identity","users",{search,isActive,emailConfirmed,roleId,page,size:20,sort:"userName asc"}]` → `searchUsers`, `keepPreviousData` `users.tsx:99-117`; roles for filter `["identity","roles"]` → `listRoles` (staleTime 60s) `users.tsx:119-123`.
- **Search** (debounced 250ms); **filters**: Account status pill (All/Active/Inactive), Email status pill (Any/Confirmed/Pending), Role `Combobox` searchable/clearable `users.tsx:87-97, 157-196`; clear-filters resets all `users.tsx:132-137`.
- Desktop list: Name (avatar, email, dimmed if inactive) / Username (@x) / Status badges (Active/Inactive + Confirmed/Pending) / chevron → `/identity/users/:id` `users.tsx:248-263, 332-382`; mobile cards `users.tsx:241-245, 297-330`; `EntityPager` `users.tsx:265-272`.
- **RegisterUserDialog**: first/last name, username, email, phone, password + confirm (mismatch hint + aria-invalid), `registerUser` mutation, toast w/ email-confirmation note `users.tsx:388-570`.
- States: `EntityListLoading`, `EntityEmpty` (filters vs none w/ Register CTA), error alert.

### `/identity/users/:userId` (`src/pages/identity/user-detail.tsx`, 892 lines)
- **Permission gates** (client, mirrors server): `Permissions.Users.Impersonate`, `Permissions.Sessions.ViewAll`, `Permissions.Sessions.RevokeAll`, `Permissions.Users.ConfirmEmail` `user-detail.tsx:93-96`.
- Queries: `["identity","users",userId]` → `getUserById`, `["identity","users",userId,"roles"]` → `getUserRoles`, `["identity","users",userId,"sessions"]` → `getUserSessionsAdmin` (enabled only when canViewSessions, staleTime 15s) `user-detail.tsx:98-108, 224-229`.
- **EntityDetailHero**: avatar + img, Active/Inactive + Email confirmed/pending badges, subtitle (@username · email · phone); actions: Impersonate (only if canImpersonate + not self + active), Resend confirmation / Confirm email (if canConfirmEmail + pending), Deactivate/Reactivate, Delete (destructive) `user-detail.tsx:318-443`; stats (roles count, sessions count).
- **Identity card** (read-only profile rows: username, email, first/last name, phone, id) `user-detail.tsx:447-463`.
- **Role assignment** section: staged Switch toggles w/ "modified" dot + pending badge, Discard/Save buttons → `assignUserRoles` payload, toast + targeted invalidation `user-detail.tsx:466-557, 121-169`.
- **SessionsCard**: active/ended badges, device + browser + OS + IP + last-seen/started, per-session Revoke (canRevoke) + "Revoke all" header action, sorted by lastActivity `user-detail.tsx:561-572, 766-891`.
- **Dialogs**: Delete confirm, Toggle-status confirm (deactivate/reactivate), Impersonate (reason input recorded in audit log, warning-toned button, navigates `/` on success), Revoke-all confirm `user-detail.tsx:574-719`.
- States: Hero skeleton, `ErrorBand` for not-found/error, section skeletons. No realtime (static queries).

### `/identity/roles` (`src/pages/identity/roles.tsx`, 388 lines)
- **Query**: `["identity","roles"]` → `listRoles` (single fetch, client-side filtering) `roles.tsx:64-78`; **client-side search** (debounced 200ms, name/description) `roles.tsx:59-62, 71-78`.
- Desktop list: Name (+"System" badge for admin/administrator/basic/user — `SYSTEM_ROLE_NAMES`) / Description / Permission count / chevron → `/identity/roles/:id` `roles.tsx:39, 153-168, 237-278`; mobile cards `roles.tsx:199-235`.
- **CreateRoleDialog**: name + description → `upsertRole` with client-generated GUID (`newGuid`) → navigates to role detail for permission editing `roles.tsx:45-52, 284-388`.
- States: loading skeleton, `EntityEmpty` (search vs none), error alert. No delete/edit in-place on list.

### `/identity/roles/:roleId` (`src/pages/identity/role-detail.tsx`, 1090 lines)
- **Queries**: `["identity","roles",roleId]` → `getRoleWithPermissions`; `["identity","permissions","catalog"]` → `getPermissionsCatalog` (staleTime 10min) → `groupPermissions` by resource `role-detail.tsx:71-89`.
- **System roles** (`Admin`, `Basic`) are read-only (name/desc inputs `readOnly`, all toggles disabled, delete button disabled, info banner) `role-detail.tsx:62-64, 310, 352-376, 391-405`.
- **Permission editor**: search box (resource/action/description/name), filter chips All/Enabled/Modified/Basic with live counts, force-expand when searching, accordion of resource groups, group tri-state checkbox + All/None chips + PipBar (12-pip density), per-permission rows with action handle + description + root/basic badges + modified dot `role-detail.tsx:103-220, 412-645, 718-1090`.
- Presets: **Basic** (isBasic), **All**, **Clear** `role-detail.tsx:151-154, 417-427`.
- Save: separate `upsertRole` (meta) + `updateRolePermissions` (permissions), dirty tracking per field, Discard/Save footer + "Unsaved changes" warning strip `role-detail.tsx:118-129, 222-265, 429-456`; stat shows `selected/total` permissions `role-detail.tsx:340-349`.
- **DeleteRoleDialog**: confirm → `deleteRole` → navigate back to roles `role-detail.tsx:244-255, 649-677`.
- States: dual-query loading skeleton, ErrorBand for role/catalog errors, "No permissions match" empty + Reset filters.

### `/identity/groups` (`src/pages/identity/groups.tsx`, 396 lines)
- **Query**: `["identity","groups",{search}]` → `listGroups(debounced)` (server-side search, debounced 250ms) `groups.tsx:57-65`.
- Desktop list: Group (avatar, name, desc) / Composition (memberCount + roleNames count) / Flags (Default w/ star, System badges) / chevron → `/identity/groups/:id` (row click navigates too) `groups.tsx:137-151, 219-290`; mobile cards `groups.tsx:130-134, 169-217`.
- **CreateGroupDialog**: name + description + **Default group Switch** ("newly registered users join automatically") → `createGroup` (roleIds: []) → navigates to group detail `groups.tsx:292-396`.
- States: loading, `EntityEmpty`, error alert.

### `/identity/groups/:groupId` (`src/pages/identity/group-detail.tsx`, 694 lines)
- **Queries**: `["identity","groups",groupId]` → `getGroupById`; `["identity","groups",groupId,"members"]` → `getGroupMembers`; `["identity","roles"]` → `listRoles` (staleTime 60s) `group-detail.tsx:79-99`.
- **Hero**: Default/System badges, member + role counts, Delete group (hidden for system groups) `group-detail.tsx:218-258`.
- **Group details section**: name/desc inputs + Default group Switch (disabled for system groups), **Roles attached** toggle rows (Switch per role w/ selected state), Discard/Save (dirty-tracked meta+roles) → `updateGroup` `group-detail.tsx:260-356, 460-502`.
- **Members section**: avatar, name, email, link → user detail, Remove button per member → `removeUserFromGroup` `group-detail.tsx:359-420`; empty state w/ Add members CTA.
- **AddMembersDialog**: search active users (debounced 250ms, `searchUsers` pageSize 20, only active), multi-select checkboxes, "already in" dimming for existing members, `addUsersToGroup` with toast reporting added + duplicate counts `group-detail.tsx:508-693`.
- **DeleteDialog**: confirm → `deleteGroup` → back to groups `group-detail.tsx:423-448`.

### `/tickets` (`src/pages/tickets/tickets.tsx`, 525 lines)
- **Query**: `["tickets","list",{search,statusFilter,priorityFilter,pageNumber}]` → `searchTickets` sorted createdAtUtc desc, pageSize 20, `keepPreviousData`, debounced search 300ms `tickets.tsx:92-116`.
- **Filters**: Status pill (All + `TICKET_STATUSES`) and Priority pill (Any + `TICKET_PRIORITIES`) `tickets.tsx:149-174`; colors via `lib/ticket-enums.ts` `STATUS_TONE`/`PRIORITY_TONE` `tickets.tsx:31-36, 315-320, 360-371`.
- Desktop list: Subject (avatar, title, ticket number) / Priority / Status / Assignee (avatar+name, "Unassigned" mono text) / Updated (relative) / chevron → `/tickets/:id` `tickets.tsx:230-246, 333-401`; assignee display via `useUserDisplay` cache `tickets.tsx:340`.
- **CreateTicketDialog**: title, description, priority Combobox (default Medium) → `createTicket`; invalidates `["tickets"]` `tickets.tsx:407-524`.
- Mobile cards `tickets.tsx:284-327`; `EntityPager` `tickets.tsx:248-255`. States: loading, empty (filters vs none), error w/ AlertTriangle.

### `/tickets/:ticketId` (`src/pages/tickets/ticket-detail.tsx`, 870 lines)
- **Queries**: `["tickets","detail",ticketId]` → `getTicketById`; `["tickets","comments",ticketId]` → `listTicketComments` `ticket-detail.tsx:95-105`.
- **Hero**: ticket number + opened-relative-by reporter, status/priority badges (Critical/High alert icons), actions: Refresh (manual refetch w/ spin), Assign/Reassign, Resolve (when not Resolved/Closed), Reopen (when Resolved/Closed); stats: comment count, updated, resolved `ticket-detail.tsx:178-314`.
- **Description section** + Resolution note box (success-tone) `ticket-detail.tsx:320-352`.
- **Conversation/comments**: chat-style bubbles (self-right-aligned), per-call `addTicketComment` via mutate(arg), composer w/ char count 8192, disabled when Closed, "Reopen to comment" hint `ticket-detail.tsx:358-463, 465-516`.
- **Properties sidebar**: reporter/assignee/status/priority/created/updated/resolved `ticket-detail.tsx:522-612`.
- **ResolveDialog**: optional resolution note → `resolveTicket` `ticket-detail.tsx:618-702`. **AssignDialog**: `UserPicker` (search by name/email), clear-to-unassign hint, notes status transitions (assign→In progress, unassign→Open) `ticket-detail.tsx:708-801`.
- **NotFoundPanel** when ticket missing; `DetailSkeleton` loading `ticket-detail.tsx:807-867`. No realtime — manual Refresh button.

### `/chat` + `/chat/:channelId` (`src/pages/chat/chat-page.tsx`, 365 lines)
- **Shell**: two-column (ChannelRail left + channel pane right), full-bleed (`-m-4 h-[calc(100vh-3.5rem)]`), mobile shows rail OR pane `chat-page.tsx:74-107`; auto-redirect `/chat` → `/chat/{firstChannelId}` (replace) `chat-page.tsx:68-72`; empty state w/ key hints (↵ send, ⇧↵ newline, @ mention) `chat-page.tsx:109-132`.
- **Realtime (SignalR, NOT SSE for chat)**: `useRealtime()` from `src/realtime/realtime-context.tsx` — single shared hub at `/api/v1/realtime/hub` w/ `?access_token=` factory, WebSockets|SSE|LongPolling transport fallback, reconnect backoff [2s,5s,10s,30s] + jittered exponential retry cap 60s, lazy import of `@microsoft/signalr`, token-epoch rebuild on login/logout/impersonation `realtime-context.tsx:81-118, 159-237`.
- **Hub events pre-registered** (fan-out sink): `ChatMessageCreated`, `ChatMessageEdited`, `ChatMessageDeleted`, `ChatMessagePinned`, `ChatMessageUnpinned`, `ChatChannelMemberAdded`, `ChatChannelMemberRemoved`, `ChatChannelMemberRead`, `ChatChannelAdded`, `ChatChannelRemoved`, `ChatChannelRead`, `ChatReactionChanged`, `ChatTypingStarted`, `PresenceChanged`, `NotificationCreated` `realtime-context.tsx:138-156`.
- **JoinChannel** hub invoke on open/reconnect `chat-page.tsx:163-166`; **markChannelRead** watermark advances on latest message id change (skips `temp:` optimistic ids) `chat-page.tsx:202-218`.
- Queries: `["chat","my-channels"]` (pageSize 100, staleTime 30s), `["chat","channel",id]` (30s), `["chat","messages",id]` (staleTime 0, pageSize 100) `chat-page.tsx:59-63, 175-185`.
- Header: back button (mobile), Hash/Lock/Users2 icon, title (DM partner real name via `useUserDisplay`), member count, **search toggle** (`ChatSearchOverlay`), **settings toggle** (`ChannelSettingsDialog`, channels only) `chat-page.tsx:247-324`; `ChatPinnedBar` under header `chat-page.tsx:328-336`; `MessageList` w/ ref for jumpToMessage `chat-page.tsx:339-348`; `TypingIndicator` presence row (reserved height) `chat-page.tsx:351`; `Composer` w/ replyTo quote preview `chat-page.tsx:355-362`.
- Per-route chat CSS chunk (`chat.css`) `chat-page.tsx:4-7`.

### Chat components (`src/pages/chat/`)
- `channel-rail.tsx` — left rail: channel list + DMs, selection, unread states (ChannelType Channel vs DirectMessage).
- `chat-search.tsx` (`ChatSearchOverlay`) — message search overlay, jump-to-message w/ `jumpToMessage` handle; older-than-window toast.
- `chat-pinned.tsx` (`ChatPinnedBar`) — pinned strip, jump to pin.
- `channel-settings.tsx` (`ChannelSettingsDialog`) — channel settings (rename/description/private/members?), channels only.
- `composer.tsx` — send/newline keys, @ mention trigger, reply quote preview, optimistic send.
- `message-list.tsx` (`MessageList`, `MessageListHandle`) — message stream, unread divider, day rules, reaction chips, jump pills, mention pills, `jumpToMessage()`.
- `message.tsx` — single message bubble (edited, reactions, pin).
- `typing-indicator.tsx` — typing presence dots.
- `mention-picker.tsx` — @ mention picker.
- `chat-utils.ts` (`channelTitle`) — channel/DM title resolution.

### `/files` (`src/pages/files/my-files.tsx`, 710 lines)
- **Tabs**: "My files" / "Shared in tenant" (role=tablist, counts) `my-files.tsx:197-223`; queries `["files","mine"]` → `listMyFiles(1,100)`, `["files","shared"]` → `listSharedFiles(1,100)` (lazy, staleTime 30s) `my-files.tsx:43-44, 131-140`.
- **Upload**: `FileDropzone` (ownerType "MyFiles", visibility Private, allowed ext allowlist, client max 50MB, category-by-extension) `my-files.tsx:46-63, 226-238`; upload success invalidates both keys `my-files.tsx:142-145`.
- **FilterBar**: filename search + Kind chips (All/Images/Documents/Archives/Other by content-type bucket, scope-wide counts, disabled when 0) `my-files.tsx:77-111, 156-169, 391-529`.
- Desktop list: Filename (mime icon) / Visibility (Public/Private) OR Uploaded by / Size (`formatBytes`) / Uploaded date `my-files.tsx:116-117, 574-592, 657-709`; mobile cards `my-files.tsx:606-655`.
- **FilePreviewDialog**: opens row → preview, delete, visibility flip; `onDeleted`/`onVisibilityChanged` invalidates both lists `my-files.tsx:325-330, 176-180`.
- States: `ErrorBand`, `EntityListLoading`, `EntityEmpty` per tab + "No matches" w/ Reset filters. No pagination (fetches 100); no realtime.

### `/catalog/products/:productId` (`src/pages/catalog/product-detail.tsx`, 1153 lines)
- **Queries**: `["catalog","products",productId]` → `getProductById`; brand + category by id (staleTime 60s) `product-detail.tsx:102-122`.
- **Hero**: thumbnail/initials avatar, Active/Hidden badge, SKU + brand + category subtitle, Refresh/Edit/Delete actions, stats: price (formatMoney), stock (Out-of-stock/low <10/in-stock tones), image count; meta links back to `?brand=`/`?category=` filtered list `product-detail.tsx:249-403`.
- **Sidebar panels**: Pricing (Change price → PriceDialog), Inventory (Adjust stock → StockDialog, tones), Identifiers (SKU/slug/ids) `product-detail.tsx:409-516`.
- **Description** section w/ Edit action; **Images** section → `ProductImageManager` (upload more, star cover) `product-detail.tsx:179-208`; **Audit** panel (created/revised/status) `product-detail.tsx:518-542`.
- **ProductEditorDialog**: name, SKU disabled ("fixed after creation"), brand/category Comboboxes (searchBrands/searchCategories pageSize 200, staleTime 60s, enabled on open), description, Visibility Switch; valid = name + brand + category `product-detail.tsx:672-842`.
- **DeleteDialog** → `deleteProduct` (invalidates products + trash) `product-detail.tsx:844-897`.
- **PriceDialog**: was→becomes comparison, delta tone (up success/down destructive), amount + 3-char currency, emits `ProductPriceChanged` domain event `product-detail.tsx:899-1019`.
- **StockDialog**: delta stepper (+/-), current→becomes, negative-stock guard w/ inline error, emits `ProductStockAdjusted` event `product-detail.tsx:1021-1153`.
- States: `DetailSkeleton`, `NotFoundPanel` (Product not found, back to products), ErrorBand.

### `/` overview (`src/pages/overview.tsx`, 1293 lines)
- **Queries**: `["billing","usage"]` → `getUsageSnapshots()`, `["billing","subscription","me"]` → `getMySubscription()`, `["tenant","me","status"]` → `getMyStatus()` (all staleTime 60s) `overview.tsx:947-967`; recent audits `["audits","recent","overview"]` → `listAudits` 24h/5 `overview.tsx:563-575`.
- **SSE**: `useSseStatus()` (status + eventCount), `useSseEvents()` — drives Live events stat + System status card (Wifi/WifiOff, SSE badge, pulse dot) + Live feed widget (last 5, tone-coded badges) `overview.tsx:944-945, 471-540, 737-766`.
- **Greeting header**: date caption + tenant, time-of-day greeting (morning/afternoon/evening), Refresh (refetch all three) / View activity / View audits buttons `overview.tsx:111-116, 1006-1019, 1111-1138`.
- **Stat cards** (4-up): Plan, Valid for (tenant expiry: Active/InGrace/Expired tones + days-left), Resources (count + avg utilization + overage), Live events (pulse dot + status) `overview.tsx:200-262, 1141-1197`.
- **Widgets**: Subscription side card (plan, status, term window, progress bar), System status (SSE), Recent audits (severity stripe + type icon + relative time → /system/audits), Usage by resource (per-resource used/limit/utilization bar, overage badges), Quick actions (4 tiles), Live feed `overview.tsx:268-309, 349-465, 562-651, 657-730, 1203-1290`.
- **First-run panel**: 4-step setup checklist (pick plan → invite team → browse catalog → watch live), per-tenant localStorage dismiss `fsh.firstrun.dismissed:{tenantId}` `overview.tsx:774-796, 807-936, 979-983, 1100-1106`.

## SSE realtime infra (`src/sse/`)
- **`sse-context.tsx`** (232 lines): `SseProvider` + split contexts `useSseStatus()` / `useSseEvents()` (status-only consumers don't re-render per event) `sse-context.tsx:25-40, 208-232`; event buffer capped at 200 `MAX_EVENTS` `sse-context.tsx:42`; manual SSE parser (event/id/data fields, blank-line delimiters) `sse-context.tsx:60-97`.
- **Endpoint/topics**: issues short-lived single-use token via `issueSseToken()` (`sse-api.ts`), then GET `{apiBase}/api/v1/sse/stream?token=..` with tenant header, `Accept: text/event-stream`, `AbortController`; reconnect w/ backoff 1s→30s doubling `sse-context.tsx:144-182, 43-44`; event types are arbitrary server-side names (rendered as badges).
- **Conversation streams**: chat uses **SignalR** (`realtime-context.tsx`) — SSE stream is used for the dashboard live event feed (overview/activity/topbar).

### `/activity` (`src/pages/activity.tsx`, 193 lines)
- **Purpose**: full live SSE event log. `useSseStatus()`/`useSseEvents()`, top 200 events rendered `activity.tsx:69-73`; header streaming/offline badge + total count `activity.tsx:78-92`.
- Desktop table columns: Action (tone-coded type badge + JSON payload snippet) / Entity (extracted entityId/aggregateId/id/tenantId/userId) / Time (HH:mm:ss) `activity.tsx:67, 136-148, 173-193`; mobile cards `activity.tsx:157-171`; event→tone heuristic (fail/error/revoke→danger etc.) `activity.tsx:41-48`.
- Empty/loading states: `EntityEmpty` listening/not-connected `activity.tsx:94-103`. No pagination (fixed 200 buffer); no forms.

### `/subscription` (`src/pages/subscription.tsx`, 540 lines)
- **Queries**: `["tenant","me","status"]` → `getMyStatus`, `["billing","subscriptions","me"]` → `getMySubscription`, `["billing","usage"]` → `getUsageSnapshots`, `["billing","invoices","me",{pageNumber:1,pageSize:5}]` → `getMyInvoices` (all staleTime 60s) `subscription.tsx:104-126`.
- **Plan card**: plan name + status badge (always success = only active sub returned), Started/Ends dates, "operator-driven changes" note `subscription.tsx:218-287`.
- **Validity card**: expiry state badge (Active/InGrace→warning/Expired→danger) + Inactive badge, Valid until, Grace ends when InGrace `subscription.tsx:66-80, 293-346`.
- **Usage by resource**: per-resource used/limit/utilization + bar (danger overage / warning ≥80% / primary), `UsageRow` `subscription.tsx:352-454`.
- **Recent invoices** (5): number + status badge (Paid/Issued/Void), period, subtotal money, link → `/invoices/:id` `subscription.tsx:82-93, 460-539`.
- `ErrorBand` when status/sub questions error `subscription.tsx:140-144, 157`. No forms; no realtime (staleTime queries only).

### `/wallet` (`src/pages/wallet.tsx`, 410 lines)
- **Queries**: `["billing","wallet","me"]` → `getMyWallet` (staleTime 30s); `["billing","topup-requests","me",{pageNumber,pageSize:20}]` → `getMyTopupRequests` w/ `keepPreviousData` `wallet.tsx:44-45, 76-87`.
- **Balance card**: formatted balance, low-balance hint (<=$10 threshold; 0/negative → "empty" warning) `wallet.tsx:42, 177-217, 186-214`.
- **Top-up form** (plain controlled inputs, not zod): amount (number, step 0.01, min 0) + optional note (1000 chars) → `createTopupRequest`, per-call data via mutate(arg), invalidates wallet + topup queries `wallet.tsx:223-316`.
- **Top-up requests table**: Requested date (+note) / Amount / Status (Pending→warning, Invoiced→info, Completed→success, Rejected→danger, Cancelled) / Invoice link (`/invoices/:id`) `wallet.tsx:53-67, 143-148, 361-410`; mobile cards w/ invoice link `wallet.tsx:322-359`; `EntityPager` `wallet.tsx:158-165`.
- States: `ErrorBand` (wallet + requests), `EntityListLoading`, `EntityEmpty`. No realtime.

### `/invoices` (`src/pages/invoices.tsx`, 331 lines)
- **Query**: `["billing","invoices","me",{pageNumber,pageSize:20}]` → `getMyInvoices` (staleTime 30s, keepPreviousData) `invoices.tsx:74-79`.
- **Columns**: Invoice # (+period) / Customer (tenantId) / Amount / Status (Paid→success, Issued→info, Void→danger, Draft) / Due date (Issued→warning; Paid→"paid <date>" success) `invoices.tsx:50-62, 194-200, 268-331`.
- **Search**: client-side only over current page (invoice number/status/period); pagination suppressed while searching `invoices.tsx:98-112, 211-220`.
- Mobile cards; `EntityPager` server paging `invoices.tsx:231-266, 212-219`. States: loading/empty/error (`ApiRequestError.problem.detail`), "No invoices found" vs "No invoices yet" + Clear search. No filters; no realtime.

### `/invoices/:invoiceId` (`src/pages/invoices/invoice-detail.tsx`)
- Read-only tenant invoice detail (see file for exact rows: number, period, status, due date, customer/tenant, line items, totals) — linked from wallet top-up requests and invoices list.

### `/system/health` (`src/pages/health.tsx`, 751 lines)
- **Query**: `["health","ready"]` → `getReadiness(signal)` w/ **polling** `refetchInterval:10s` (toggleable Live/Paused), `refetchOnWindowFocus:false`, retry 1 `health.tsx:154-166`; 5s UI re-render timer keeps "ago" labels fresh `health.tsx:173-177`.
- **Hero panel**: status tone (Healthy→success/Degraded→warning/Unhealthy→danger), headline copy variants, HTTP status, vitals (Checks passing, Round-trip latency, Slowest, Last poll), tone-tinted radial, **HistoryPips** pip-bar of last 24 polls (ring buffer in-memory) `health.tsx:67-80, 105-120, 286-439`.
- **Dependencies**: collapsible rows — status pip + icon (self/redis/hangfire/postgres/storage/http registry) + name + latency (mono, ms/s formatting) + status word; expanded detail: latency budget bar (sqrt-scaled vs 500ms) + detail key/value table (top 6, "+N more") `health.tsx:88-97, 449-657`.
- States: `ErrorPanel` ("Health endpoint unreachable"), `HeroSkeleton`, `ChecksSkeleton`, `EmptyChecks` ("[AddHealthChecks] none registered"). No forms; polling-based, not SSE.

### `/system/audits` (`src/pages/audits.tsx`, ~1360+ lines)
- **Queries**: `["audits","list",filters,window]` → `listAudits` (pageSize 25, keepPreviousData, staleTime 5s); `["audits","summary",window]` → `getAuditSummary` (30s); `["audit","detail",id]` → `getAuditById` (enabled on open, 60s); `["audit","by-correlation",cid]` → `getAuditsByCorrelation` `audits.tsx:202-237, 993-998, 1290-1295`.
- **Filters**: time range presets (24h/7d/30d/90d, server-enforced window); type chips (Activity/Security/EntityChange/Exception); severity chips (Info/Warning/Error/Critical); **Hide system activity** Switch (excludes Activity unless explicitly filtered to it); Advanced: Source / User ID / Correlation / Trace text fields + Tags bitmask chips; debounced free-text search (300ms); reset `audits.tsx:73-86, 154-181, 312-320, 707-906`.
- **Summary strip**: window, grand total, segmented stack bar by type (Activity/Entity/Security/Exception), severity dots (Warn/Err+Crit), top-5 sources chips `audits.tsx:583-701`.
- **List columns**: Actor (avatar, name, user id) / Event (icon, plain-English predicate, source, tag chips +2) / Severity badge / Timestamp (UTC dense `2026-04-30 14:32:11.234`) `audits.tsx:67, 125-136, 493-575`; mobile cards; `EntityPager`.
- **Detail drawer** (right slide-in Radix dialog, 640px): severity/type header, Identity grid (tenant/user/userId/source), Trace grid + "All by correlation"/"All by trace" jumps (reset filters to that id), **Related events** timeline (12 max, click to swap drawer), JSON Payload w/ copy button, Pipeline (occurred/received/sink-delay) `audits.tsx:978-1228, 1279-1360+`.
- States: loading / error alert / empty w/ Reset filters. No realtime — staleTime refetch only; pull-to-refresh button.

### `/system/trash` (`src/pages/system/trash.tsx`, 691 lines)
- **Tabs**: Products / Brands / Categories / Tickets / Files, each gated by permission (`src/lib/trash-permissions.ts`); tabs hidden if no permission, fallback to first visible, "No recycle bins available" empty page `trash.tsx:73-110, 126-131`.
- Per-tab queries (pageSize 20): `["trash","products"|"brands"|"categories"|"tickets"|"files",page]` + restore mutations invalidating both trash and source lists `trash.tsx:195-207, 235-247, 275-287, 315-327, 355-367`.
- Shared `TrashShell`: columns Entity (avatar,title,subtitle) / Deleted by / Deleted at (relative + mono) / Actions; mobile cards; `EntityPager`; loading/error/empty ("Back to <list>") `trash.tsx:408-520, 579-690`.
- **Restore confirm dialog** (Radix): "Restore <singular>?" with Cancel/Restore `trash.tsx:526-562`.
- Permission via `useAuth().user.permissions` includes checks.

### `/system/sessions` (`src/pages/system/sessions.tsx`, 490 lines)
- **Query**: `["identity","sessions","tenant",{search,includeInactive,pageNumber}]` → `getTenantSessions` (pageSize 50, keepPreviousData, **refetchInterval 30s** polling) `sessions.tsx:67-83`.
- **Filter**: search (debounced 300ms, user/email/IP), `EntityFilterPill` Live only/Include inactive; stats strip active/distinct users/mobile `sessions.tsx:154-180`.
- **Columns**: User (avatar, "You" info badge, Inactive danger badge, email) / Device (browser+browserVersion, OS) / IP / Last activity (relative) / Actions (All devices ghost + Revoke outline — only for active, non-current sessions) `sessions.tsx:236-243, 388-489`; mobile cards `sessions.tsx:291-382`.
- **Mutations**: `adminRevokeUserSessionById(userId,id)` + `adminRevokeAllUserSessions(userId)` w/ toast + invalidate `sessions.tsx:98-129`. Loading/error/empty states; `EntityPager`. No SSE (polling).

### `/invoices/:invoiceId` (`src/pages/invoice-detail.tsx`, 396 lines)
- **Query**: `["billing","invoices",id]` → `getMyInvoice(id)` enabled on id `invoice-detail.tsx:52-56`; `EntityDetailBack` to /invoices `invoice-detail.tsx:62`.
- **Header**: gradient trim, invoice number + status badge (Paid/Issued/Void) + purpose badge, period + subtotal, **Download PDF** button (`downloadInvoicePdf`) `invoice-detail.tsx:115-184`.
- **Line items table**: Description (with kind badge + resource) / Qty / Unit price / Amount + Total row `invoice-detail.tsx:190-284`.
- **Details** dl: Status/Currency/Period/Created/Issued/Due(warning)/Paid(success)/Voided(danger)/Period start/end; Notes section `invoice-detail.tsx:290-350`.
- States: `DetailSkeleton`, `NotFoundPanel` ("Invoice not found ... may not belong to your tenant"), `ErrorBand`. No permission-gating beyond tenant scoping; no realtime.

### `/catalog/products/:productId` (`src/pages/catalog/product-detail.tsx`, 1153 lines)
- **Queries**: `["catalog","products",id]` → `getProductById`; brand/category by id (staleTime 60s) `product-detail.tsx:102-122`.
- **Hero**: avatar (thumbnail), name, Active/Hidden badge, SKU/brand/category subtitle, Refresh/Edit/Delete buttons, stats (price, stock w/ tone ≤10 low/0 out, image count), meta links → `?brand=`/`?category=` filters on list, created/updated relative `product-detail.tsx:249-403`.
- **Sidebar**: Pricing (Change price dialog), Inventory (Adjust stock dialog), Identifiers (SKU/slug/product/brand/category IDs) `product-detail.tsx:409-516, 899-1152`; Description section + Edit, **Images** via `ProductImageManager` (upload/cover star; `components/file/product-image-manager.tsx`), Audit (created/revised/status) `product-detail.tsx:544-603, 198-212`.
- **Dialogs**: Edit product (name/SKU-readonly/brand+category Combobox searchable/description/visibility Switch — plain state form, validates non-empty + brand + category) `product-detail.tsx:672-842`; Delete (destructive, invalidates trash) `product-detail.tsx:844-897`; **Change price** (was→becomes with success/danger delta, amount + 3-char currency, emits `ProductPriceChanged`) `product-detail.tsx:899-1019`; **Adjust stock** (delta stepper +/- , "becomes" preview, negative-stock guard, emits `ProductStockAdjusted`) `product-detail.tsx:1021-1152`.
- States: `ErrorBand`, `DetailSkeleton`, `NotFoundPanel` ("Product not found … deleted or link wrong"). No realtime; image upload is the only file interaction.

### `/files` (`src/pages/files/my-files.tsx`, 710 lines)
- **Queries**: `["files","mine"]` → `listMyFiles(1,100)`; `["files","shared"]` → `listSharedFiles(1,100)` enabled on shared tab, staleTime 30s `my-files.tsx:131-140`.
- **Tabs**: My files / Shared in tenant (counts on pills) `my-files.tsx:197-238`.
- **Upload**: `FileDropzone` — ownerType MyFiles, Private default, whitelisted extensions (jpg/pdf/docx/xlsx/pptx/txt/csv/zip…), 50MB client cap; invalidates both keys on upload `my-files.tsx:63, 225-238`.
- **FilterBar**: search by filename (case-insens substring) + kind chips (All/Images/Documents/Archives/Other) w/ unfiltered scope-wide counts `my-files.tsx:391-529`.
- **Columns**: Filename (+mime icon) / Visibility (Public info badge / Private) or Uploaded by (`useUserDisplay`) / Size (`formatBytes`) / Uploaded date; mobile cards `my-files.tsx:535-710`.
- **Preview dialog**: `FilePreviewDialog` (view/download, flip public/private, delete; components/file/file-preview-dialog.tsx) `my-files.tsx:325-330, 176-180`.
- States: `ErrorBand`, `EntityListLoading`, empty states ("No files yet"/"Nothing shared yet"/"No matches" + Reset filters). No server pagination (100 cap); no realtime.

### `/identity/users` (`src/pages/identity/users.tsx`, 571 lines)
- **Queries**: `["identity","users",{pageNumber,pageSize:20,search,sort:"userName asc",isActive,emailConfirmed,roleId}]` → `searchUsers` (keepPreviousData); `["identity","roles"]` → `listRoles` (60s) `users.tsx:99-123`.
- **Filters**: EntitySearch (debounced 250ms) + EntityFilterPill (Account status All/Active/Inactive, Email status Any/Confirmed/Pending) + Role Combobox filter (searchable/clearable from roles) `users.tsx:157-196`.
- **Columns**: Name (+email, avatar) / Username (@mono) / Status (Active success / Inactive default + Confirmed info / Pending warning, lg+) / chevron; dimmed inactive rows; rows link → `/identity/users/:id` `users.tsx:249-263, 332-382`; mobile cards `users.tsx:297-330`.
- **Register dialog** (plain state form): first/last, username, email, phone, password + confirm w/ mismatch hint (no zod), Register → `registerUser`, resets on close `users.tsx:388-570`.
- States: loading/empty ("No users found/have")/inline error; `EntityPager`. No realtime.

### `/identity/users/:userId` (`src/pages/identity/user-detail.tsx`)
- Tenant-scoped operator view of a single user — see file: profile (avatar/name/email/phone), status + email-confirmed badges, roles membership (add/remove), login/activity, session revoke entry points, and read-only identity metadata.

### `/identity/users/:userId` (`src/pages/identity/user-detail.tsx`, 892 lines)
- **Queries**: `["identity","users",userId]` → `getUserById`; `["identity","users",userId,"roles"]` → `getUserRoles`; `["identity","users",userId,"sessions"]` → `getUserSessionsAdmin` (enabled w/ `Permissions.Sessions.ViewAll`, staleTime 15s) `user-detail.tsx:98-108, 224-229`.
- **Permission gating** (actor permissions): Impersonate (`.Users.Impersonate`), View sessions (`.Sessions.ViewAll`), Revoke (`.Sessions.RevokeAll`), Confirm email (`.Users.ConfirmEmail`) `user-detail.tsx:93-96`.
- **Hero actions**: Impersonate (disabled for inactive, own-user), Resend confirmation / Confirm email, Deactivate/Reactivate, Delete — each with confirm dialog; reason input recorded in audit log `user-detail.tsx:349-410, 637-689`.
- **Role assignment**: staged Switch toggles per role (`.roleName`+description), pending badge (`N pending`), Discard/Save changes with dirty tracking; assign over full role list `user-detail.tsx:466-557, 149-169`.
- **Sessions card** (`SessionsCard`): device icon, browser·OS, IP, last-seen/started, Active/Ended badges, per-session Revoke + Revoke all (dialog) `user-detail.tsx:766-891`.
- Mutations: `toggleUserStatus`, `confirmUserEmail`, `resendUserConfirmationEmail` (Resend guard by permission not shown but mutation wired), `deleteUser` (navigates back), `adminRevokeUserSession`, `adminRevokeAllUserSessions`, `beginImpersonation` (navigates to `/`) `user-detail.tsx:171-278`.
- States: loading skeletons, `ErrorBand`/not-found, "No roles defined → Create one" link, "No sessions on file".

### `/catalog/products` (`src/pages/catalog/products.tsx`, 1409 lines)
- **Query**: `["catalog","products",{search,brandId,categoryId,isActive,pageNumber,pageSize:25}]` → `searchProducts` (sortBy createdAtUtc desc, keepPreviousData) `products.tsx:72, 197-222`; brands+categories filter lists (pageSize 200, 60s) `products.tsx:224-233`.
- **Search**: big input placeholder "Search by name, SKU, or slug…" debounced 250ms w/ Clear `products.tsx:272-296`.
- **Filters**: FilterRow — Brand Combobox (filter variant), Category Combobox, ActivePill segmented (All/Active/Hidden) `products.tsx:87-169`.
- **Columns (desktop)**: Product (image w/ lazy fallback, name, Hidden badge) / SKU / Brand badge+category (lg) / Price (clickable → PriceDialog) + `StockChip` (clickable → StockDialog, tone ≤0 danger / <10 warning) / row actions Edit+Delete (opacity-on-hover) + chevron; inactive rows dimmed; row → `/catalog/products/:id` `products.tsx:500-610, 709-805`.
- **Mobile cards**: thumb 40px, name+SKU, brand chip/category, price + stock chip, pencil edit `products.tsx:412-493`.
- **Dialogs**: Editor (create+edit — name/SKU read-only on edit/brand+category Combobox searchable/price+currency+stock on create/description/visibility Switch plain form) `products.tsx:811-1081`; **PriceDialog** (was→becomes delta success/danger, emits `ProductPriceChanged`) `products.tsx:1087-1213`; **StockDialog** (delta stepper, negative guard, emits `ProductStockAdjusted`) `products.tsx:1215-1350`; **Delete** (destructive, invalidates trash) `products.tsx:1356-1409`.
- States: `LoadingList` (skeleton cards+table), `EmptyResults` (no products / no matches + Clear filters / Add product), inline `role=alert` error. `EntityPager`. No realtime.

### `/catalog/brands` (`src/pages/catalog/brands.tsx`, 632 lines)
- **Query**: `["catalog","brands",{search,pageNumber,pageSize:20}]` → `searchBrands` (createdAtUtc desc, keepPreviousData) `brands.tsx:64, 90-105`.
- **List**: EntitySearch (name/slug) debounced 250ms `brands.tsx:130-134`; columns Brand (logo avatar w/ fallback or initials, name + truncated description) / Slug (mono) / Created (date + relative) / hover actions Edit+Delete `brands.tsx:290-362`; mobile cards w/ description `brands.tsx:245-284`.
- **Editor dialog** (create+edit): name (slug preview auto-derived), description textarea, Logo URL (type=url) `brands.tsx:409-569`.
- **Delete dialog**: warns products referencing brand "will need to be reassigned"; invalidates `["trash","brands"]` `brands.tsx:575-632`.
- States: `EntityListLoading`, `EntityEmpty` (no brands/no matches), `role=alert` error, `EntityPager`. No realtime, no paginated detail.

### `/catalog/categories` (`src/pages/catalog/categories.tsx`, 720 lines)
- **Queries**: `["catalog","categories",{search,pageNumber,pageSize:50}]` → `searchCategories` (name asc) `categories.tsx:69, 116-131`; `["catalog","categories","tree"]` → `getCategoryTree` (30s) for parent names + editor options `categories.tsx:133-137`.
- **List**: EntitySearch (name/slug) 250ms; columns Category (initials avatar, name, "under {parent}"/"root", description) / Slug / Created (+relative) / hover Edit+Delete `categories.tsx:361-450`; mobile cards `categories.tsx:300-355`.
- **Editor dialog**: name + slug preview; **Parent Combobox** with tree indentation (↳ prefix by depth, "root" prefix, excludes self and descendants — cycle guard); description `categories.tsx:456-656`.
- **Delete dialog**: warns "Categories with child categories cannot be deleted — move or delete the children first"; invalidates `["trash","categories"]` `categories.tsx:662-720`.
- States: `EntityListLoading`, `EntityEmpty`, inline alert error, `EntityPager`. No realtime.

### `/chat` and `/chat/:channelId` (`src/pages/chat/chat-page.tsx`, 365 lines)
- **Shell**: `ChatPage` — full-bleed two columns (`-m-4 h-[calc(100vh-3.5rem)]`), `ChannelRail` left + active channel pane right; no route param → auto-selects first channel and `replace`s URL; mobile only one column `chat-page.tsx:74-106`.
- **Empty state**: "Pick a conversation" + key hints (↵ send, ⇧↵ newline, @ mention) `chat-page.tsx:109-141`.
- **ActiveChannel**: queries `["chat","channel",id]` (`getChannelById`, 30s) + `["chat","messages",id]` (`listChannelMessages` pageSize 100, staleTime 0) `chat-page.tsx:175-185`; **joins SignalR group** `JoinChannel` on open/reconnect `chat-page.tsx:163-166`.
- **Header**: mobile back, channel icon (Hash public / Lock private / Users2 DM), title (DM partner resolved via `useUserDisplay`), description, member count, Search-button → `ChatSearchOverlay`, Settings-button → `ChannelSettingsDialog` (channels only) `chat-page.tsx:260-317`.
- **Pinned bar**: `ChatPinnedBar` under header; click jumps via `jumpToMessage` or toast "older than the loaded window" `chat-page.tsx:328-336`.
- **Mark-read watermark**: on latest message id change (skips `temp:` optimistic ids) → `markChannelRead`, invalidates `["chat","my-channels"]` + `["notifications","unread-count"]` `chat-page.tsx:202-218`.
- **Composer**: `Composer` w/ reply-to quoted preview; typing presence row `TypingIndicator` above it `chat-page.tsx:351-363`.
- **Realtime events consumed** by chat subcomponents: ChatMessageCreated/Edited/Deleted/Pinned/Unpinned/ChatChannelMemberAdded/Removed/Read/ChatReactionChanged/ChatTypingStarted/PresenceChanged (see `message-list.tsx`, `channel-rail.tsx`, `composer.tsx`, `typing-indicator.tsx`).

### Chat sub-components (`src/pages/chat/`)
- **channel-rail.tsx** — channel list rail: sections (Channels vs Direct messages), per-channel unread dot/count, presence (online dots), member kick/leave actions, create-channel entry, DMs list w/ partner names; consumes ChatChannelAdded/Removed, member events, ChatChannelRead, PresenceChanged; `useRealtimeEvent`.
- **composer.tsx** — message input: markdown input w/ send (↵) / newline (⇧↵), **@ mention picker** (`mention-picker.tsx` lists tenant members), reply preview quote block, attachment entry, typing events via `invoke("Typing", channelId)`, optimistic temp message (`temp:…`) then server `ChatMessageCreated` replaces it.
- **message-list.tsx** — virtualized-ish message feed: day dividers, unread "New messages" divider, message hover actions (react w/ emoji picker, reply, pin, edit, delete — permission/self gated), `jumpToMessage(id)` imperative handle for pinned/search jumps, auto-scroll to bottom on new inbound message, fetched + realtime messages merged/react-keyed.
- **typing-indicator.tsx** — "X is typing…" dots row; subscribes ChatTypingStarted, debounced clear.
- **chat-pinned.tsx** — slim pinned bar listing pinned messages (ChatMessagePinned/Unpinned), jump-to-message.
- **chat-search.tsx** — `ChatSearchOverlay`: search a channel's messages (query key `["chat","search","messages",channelId,term]`), results list w/ snippet + jump.
- **channel-settings.tsx** — `ChannelSettingsDialog`: rename/description/private toggle/admins, member list w/ role + remove, invite-by-select (members from tenant), archive channel (danger).
- **chat-utils.ts** — `channelTitle()` for display.

### Realtime infra (`src/realtime/realtime-context.tsx`, 299 lines)
- **SignalR hub**: `{apiBase}/api/v1/realtime/hub`, `?access_token=` via `accessTokenFactory` from `tokenStore` `realtime-context.tsx:81-118`; transport auto — WebSockets | ServerSentEvents | LongPolling fallbacks `realtime-context.tsx:101-104`.
- **Provider**: lazy-loads `@microsoft/signalr` (only when authed), reconnect backoff [2s,5s,10s,30s] + custom exponential (cap 60s, jitter), auto-reconnect opts out when no token, rebuilds on `tokenEpoch` change (login/logout/refresh/impersonation) `realtime-context.tsx:65-237`.
- **Event registry** (15 pre-wired): `ChatMessageCreated`, `ChatMessageEdited`, `ChatMessageDeleted`, `ChatMessagePinned`, `ChatMessageUnpinned`, `ChatChannelMemberAdded`, `ChatChannelMemberRemoved`, `ChatChannelMemberRead`, `ChatChannelAdded`, `ChatChannelRemoved`, `ChatChannelRead`, `ChatReactionChanged`, `ChatTypingStarted`, `PresenceChanged`, `NotificationCreated` `realtime-context.tsx:138-154`.
- **Hooks**: `useRealtime()` (status/on/invoke), `useRealtimeEvent(event, handler, deps)` w/ handler ref `realtime-context.tsx:272-299`.
- **Polling fallback elsewhere**: sessions page refetches every 30s; no SSE-per-topic provider in dashboard besides SignalR.

### `/` Overview (`src/pages/overview.tsx`, 1293 lines)
- **Queries**: `["billing","usage"]` → `getUsageSnapshots` (60s) `overview.tsx:947-951`; `["billing","subscription","me"]` → `getMySubscription` (60s) `overview.tsx:953-957`; `["tenant","me","status"]` → `getMyStatus` (60s) `overview.tsx:963-967`; `["audits","recent","overview"]` → `listAudits` (last 24h, pageSize 5, 30s) `overview.tsx:563-575`.
- **Header**: greeting (morning/afternoon/evening) + first name, date + tenant caption, Refresh / View activity / View audits buttons `overview.tsx:1111-1138`.
- **Stat cards (4)**: Plan (planKey+status) / Valid for (days-left w/ tone by `validityView` — Active/InGrace→graceEnds/danger Expired) / Resources (count + avg utilization + overage badge) / Live events (eventCount + SSE status dot; links `/activity`) `overview.tsx:1140-1197, 134-164`.
- **Left rail**: SubscriptionBody (plan badge + started/ends + "Current term" progress bar (subscriptionProgress %·days left)) `overview.tsx:349-465`; **SystemStatusBody** — SSE status ("Stream live" + SSE badge/pulse dot, event count "Events this session") `overview.tsx:471-540`.
- **Widget grid (2-up)**: Recent audits (top-5, severity stripe via `severityRank`, type icon, actor + relative time, rows deep-link `/system/audits`) `overview.tsx:562-651`; Usage by resource (rows resource/used/limit/%, hover bar, overage danger badge, ≥80% warning) `overview.tsx:268-309`; Quick actions (4 tiles → users/products/subscription/activity) `overview.tsx:657-730`; **Live feed** (latest 5 SSE events w/ clock + tone-coded type badge) `overview.tsx:737-766`.
- **First-run panel**: "Welcome to {tenant}" 4-step setup tiles, dismissible per-tenant via localStorage `fsh.firstrun.dismissed:{tenantId}` `overview.tsx:774-936, 979-983, 1100-1106`.

### SSE infra (`src/sse/sse-context.tsx`, 232 lines) — **primary realtime for overview/activity/topbar dot**
- **Endpoint**: `${apiBase}/api/v1/sse/stream?token={single-use short-lived SseToken}` via `issueSseToken()` (SseToken vs JWT; JWT refresh implicit on reconnect) `sse-context.tsx:127-158`.
- Provider splits **two contexts** (`useSseStatus` vs `useSseEvents`) so the topbar dot doesn't re-render per event; caps ring buffer 200 events; reconnect backoff 1s→30s doubling; status idle/connecting/connected/reconnecting/error `sse-context.tsx:30-43, 99-205`.
- Hooks: `useSse()`, `useSseStatus()`, `useSseEvents()` `sse-context.tsx:210-232`.
- So the dashboard runs **both SSE (activity/overview/bell status) and SignalR (chat/notifications)**.

### `/activity` (`src/pages/activity.tsx`, 193 lines)
- **Realtime (SSE)**: consumes `useSseStatus` + `useSseEvents` directly — no REST query. Header badge "streaming"/"offline"/status `activity.tsx:78-92`.
- **List**: latest 200 events from SSE ring buffer; desktop grid Action (tone-coded `EntityStatusBadge` for fail/error/revoke→danger, warn/retry→warning, login/issued/created→success, token/auth→info) + payload summary (JSON) / Entity (extracted `entityId|aggregateId|id|tenantId|userId`) / Time `activity.tsx:41-48, 67-73, 136-148`; mobile cards `activity.tsx:157-171`; `role="log" aria-live="polite"`.
- Empty/loading: only Empty state ("Listening for activity"/"No events yet") — no REST loading state. No pagination, no search/filters. No permission gating.

### `/subscription` (`src/pages/subscription.tsx`, 540 lines)
- **Queries** (all 60s): `["tenant","me","status"]` → `getMyStatus`; `["billing","subscriptions","me"]` → `getMySubscription`; `["billing","usage"]` → `getUsageSnapshots`; `["billing","invoices","me",{pageNumber:1,pageSize:5}]` → `getMyInvoices` `subscription.tsx:104-126`.
- **Layout**: left rail Plan + Validity sections; right column Usage + Recent invoices `subscription.tsx:159-209`.
- **PlanBody**: planKey + success badge, Started/Ends dates, "Plan changes are operator-driven" note `subscription.tsx:218-287`.
- **ValidityBody**: `EntityStatusBadge` tone by `expiryTone()` (InGrace→warning/Expired→danger/Active→success), "Inactive" badge, Valid until + Grace ends rows `subscription.tsx:66-80, 293-346`.
- **UsageBody/UsageRow**: resource/used/limit/% + bar (destructive on overage, warning ≥80%) `subscription.tsx:352-454`.
- **RecentInvoicesBody**: 5 rows linking `/invoices/{id}`, invoiceNumber + `EntityStatusBadge` (Paid→success/Issued→info/Void→danger) + period + `formatMoney` `subscription.tsx:82-93, 460-540`.
- Top **ErrorBand** when any of the 4 queries errors `subscription.tsx:140-157`. No realtime, no pagination controls.

### `/wallet` (`src/pages/wallet.tsx`, 410 lines)
- **Queries** (30s): `["billing","wallet","me"]` → `getMyWallet`; `["billing","topup-requests","me",{pageNumber,pageSize:20}]` → `getMyTopupRequests` w/ `keepPreviousData` `wallet.tsx:76-87`.
- **BalanceCard**: big `formatMoney` balance; **low-balance warning** (≤10, and empty/≤0 variant) — `LOW_BALANCE_THRESHOLD = 10` `wallet.tsx:42, 186-215`.
- **TopupRequestForm**: plain controlled inputs (no RHF/zod — dashboard convention), amount (number, step 0.01) + note, mutation `createTopupRequest` w/ per-call `mutate(arg)`; invalidates wallet + requests `wallet.tsx:223-316`.
- **List**: "My top-up requests", desktop grid Requested (date + note) / Amount / Status (`EntityStatusBadge` Pending→warning Invoiced→info Completed→success Rejected→danger Cancelled→default) / Invoice link `/invoices/{id}` `wallet.tsx:47, 53-67, 142-156, 361-410`; mobile cards `wallet.tsx:322-359`; `EntityPager` (20/page) `wallet.tsx:158-165`.
- States: `EntityListLoading`, `EntityEmpty`, inline `ErrorBand` per query. No realtime, no search/filters. **WhatsApp-wallet specific** (billing domain).

### `/invoices` (`src/pages/invoices.tsx`, 331 lines)
- **Query** (30s, keepPreviousData): `["billing","invoices","me",{pageNumber,pageSize:20}]` → `getMyInvoices` `invoices.tsx:74-79`.
- **Search**: `EntitySearch` — invoice number, status, period — **client-side filter over current page only**; pagination suppressed while searching `invoices.tsx:98-112, 135-139, 211-220`.
- **Desktop columns**: Invoice # (icon + number + period) / Customer (tenantId) / Amount (`formatMoney` Intl) / Status (`EntityStatusBadge` Paid→success Issued→info Void→danger Draft→default) / Due date (warning for Issued due, success "paid {date}" for Paid) `invoices.tsx:50-65, 193-208, 268-331`; row click → `/invoices/{id}` `invoices.tsx:279-281`; mobile cards `invoices.tsx:231-266`.
- States: `EntityListLoading`, `EntityEmpty` + Clear-search action, `ErrorBand` (`ApiRequestError.problem.detail`) `invoices.tsx:141-165`. No create/detail dialog (detail is a route). No realtime.

### `/invoices/:id` (`src/pages/invoice-detail.tsx`, 396 lines)
- **Query**: `["billing","invoices",id]` → `getMyInvoice(id)`, enabled with id `invoice-detail.tsx:52-56`.
- **Header**: gradient accent strip, invoice number + status badge + purpose badge, period + amount, **Download PDF** (`downloadInvoicePdf`) w/ downloading state `invoice-detail.tsx:115-184`.
- **Line items table**: Description (kind badge + resource) / Qty / Unit price / Amount + Total row `invoice-detail.tsx:190-284`; empty state "No line items".
- **Details**: status/currency/period/created/issued/due(warning)/paid(success)/voided(danger)/periodStart/periodEnd rows, Notes section `invoice-detail.tsx:290-325, 102-108`.
- States: `EntityDetailBack`, `ErrorBand`, `DetailSkeleton`, NotFoundPanel "Invoice not found — may not belong to your tenant" `invoice-detail.tsx:356-396`. No edit (read-only detail). No realtime.

### `/system/health` (`src/pages/health.tsx`, 751 lines)
- **Query**: `["health","ready"]` → `getReadiness`, **polling 10s** (`refetchInterval` toggleable Live/Paused), manual Refresh, retry 1, no window-focus refetch `health.tsx:156-166`.
- **HeroPanel**: status tile + badge + HTTP code; headline/subline by status (Healthy/Degraded/Unhealthy); vitals: Checks (passing/total), Round-trip, Slowest, Last poll (auto-ticking "x s ago") `health.tsx:286-379`; **HistoryPips** — last-24-poll pip bar (client-side ring buffer, no backend) `health.tsx:105-120, 413-439`.
- **DependencyList**: collapsible rows — pip + tone icon (self→ShieldCheck, redis→Zap, hangfire→Timer, postgres/db→Database, storage/disk→HardDrive, http/webhook→Globe, fallback Flame) + name/description + sqrt-scaled latency (budget 500ms) + status; expanded panel: "Latency budget" bar + Detail key/value table (top 6, "+N more") `health.tsx:38-97, 449-657`.
- States: HeroSkeleton, ChecksSkeleton, ErrorPanel "/health/ready unreachable", EmptyChecks ("AddHealthChecks()") `health.tsx:663-751`. No permission gating, no realtime (10s polling is the realtime).

### `/system/audits` (`src/pages/audits.tsx`, ~1400 lines)
- **Queries**: `["audits","list",filters,window]` → `listAudits` (25/page, keepPreviousData, stale 5s) `audits.tsx:202-230`; `["audits","summary",window]` → `getAuditSummary` (30s) `audits.tsx:232-237`; drawer `["audit","detail",id]` → `getAuditById`; `["audit","by-correlation",id]` → `getAuditsByCorrelation` `audits.tsx:993-998, 1290-1295`.
- **Filters**: time-range presets 24h/7d/30d/90d; `EntitySearch` (payload/source/user, 300ms debounce); Type chips (Activity/Security/EntityChange/Exception) + Severity chips (Information/Warning/Error/Critical, tone-coded); **advanced collapse**: Source / User ID / Correlation / Trace fields + Tags bitmask chips; **"Hide system activity" switch** (default on, excludes Activity unless explicitly selected); Reset + active-chip count badge `audits.tsx:73-86, 154-181, 707-969`.
- **SummaryStrip**: window total (big number) + stacked bar by type (Activity/Entity/Security/Exception color segments) + severity legend dots + top-5 sources chips `audits.tsx:583-701`.
- **Desktop columns**: Actor (avatar + name + first-8 userId) / Event (icon + `auditPredicate` summary + Source + tag chips) / Severity badge / Timestamp (dense UTC) `audits.tsx:493-575`; mobile cards `audits.tsx:431-487`.
- **Detail drawer (right slide-in, max 640px)**: header w/ severity badge + source + tags; Identity grid (tenant/user/userId/source); Trace grid + "All by correlation"/"All by trace" jump buttons (filters the list); **Related events timeline** (newest→oldest, cap 12, click swaps drawer); JSON Payload `pre` w/ copy button; Pipeline grid (occurred/received/sink-delay ms/audit id) `audits.tsx:978-1228, 1279-end`.
- States: `EntityListLoading`, `EntityEmpty` + Reset filters, inline error alert, `DrawerSkeleton`/`DrawerError`, Refresh button `audits.tsx:333-355, 1230-1258`. No realtime (Refresh + stale 5s). No create/edit (audit trail is read-only).

### `/system/sessions` (`src/pages/system/sessions.tsx`, 490 lines)
- **Query**: `["identity","sessions","tenant",{search,includeInactive,pageNumber}]` → `getTenantSessions` (50/page, keepPreviousData, **refetchInterval 30s**) `sessions.tsx:67-83`.
- **List**: search by user/email/IP (300ms debounce); `EntityFilterPill` Visible "Live only / Include inactive"; stats strip active/users/mobile; desktop cols User (+"You" info badge / "Inactive" danger badge) / Device (browser+OS, mobile-type icon) / IP / Last activity / Actions (Revoke one, "All" devices — hidden for current session) `sessions.tsx:154-180, 388-490`; mobile cards `sessions.tsx:291-382`.
- **Mutations**: `adminRevokeUserSessionById` (toast success/error with `ApiRequestError.problem.detail`) + `adminRevokeAllUserSessions` (success shows revokedCount) `sessions.tsx:98-129`.
- States: `EntityListLoading`, `EntityEmpty` w/ clear+refresh, inline error alert, `EntityPager`. No dialogs (revoke is inline button + toast). No permission gating on page (admin API). Permission model: operator-only route is enforced by nav + API; page itself not gated.

### `/system/trash` (`src/pages/system/trash.tsx`, 691 lines)
- **Tabs**: Products / Brands / Categories / Tickets / Files — **permission-gated** via `TRASH_TAB_PERMISSIONS` + `user.permissions`; tabs not granted are hidden; zero visible tabs → "No recycle bins available" empty state (route is directly reachable, so handled here too) `trash.tsx:73-110, 126-131`.
- **Per-tab queries**: `["trash",{resource},pageNumber]` → `listTrashedProducts/Brands/Categories/Tickets/Files` (20/page); restore mutations `restoreProduct/Brand/Category/Ticket/File` invalidating trash + parent list keys `trash.tsx:195-385`.
- **Shared `TrashShell`**: desktop cols Entity (avatar/name/subtitle) / Deleted by (first-8 id) / Deleted at (relative + mono date) / Actions (Restore); mobile cards; `EntityPager`; RestoreConfirmDialog ("Restore {singular}? … same ID and history intact") `trash.tsx:408-562`.
- Empty tab: "The X trash is empty" + "Back to {label}" link; error alert inline `trash.tsx:428-460`. No search/filters. No permanent-delete (restore-only).

### `/settings/profile` (`src/pages/settings/profile.tsx`, 233 lines)
- **Query**: `["identity","me"]` → `getMyProfile`; form seeded once from profile (falls back to JWT `user` during loading) via `seededRef` `profile.tsx:21-47`.
- **SettingsSections**: Photo (`ImageInput` ownerType User circle, `setProfileImage` mutation) `profile.tsx:88-128`; Identity form — First/Last name, Email (read-only lock + "contact your tenant admin"), Phone; dirty-check + Reset + Save buttons `profile.tsx:49-87, 130-198`; Subject identifier (read-only `profile.id`) `profile.tsx:200-208`.
- **Forms**: plain controlled inputs (no RHF/zod) + `sonner` toasts with `ApiRequestError.problem.detail` (react-hook-form + zod is **admin-app only** per AGENTS).
- States: load-fallback banner when profile fetch errors. No realtime.

### `/settings/security` (`src/pages/settings/security.tsx`, 822 lines)
- **PasswordCard**: "Change password" → **ChangePasswordDialog**: current/new/confirm fields, show/hide toggle (`PasswordField`), strength meter (length + case + digit/symbol heuristic, Weak→Strong), match check, local validations (min 8, match, must differ), `changePassword` mutation, toasts `security.tsx:269-532`.
- **TwoFactorCard**: badge enabled/disabled + **enroll flow** — `enrollTwoFactor` → QR (qrcode lib, SVG themed via currentColor), manual sharedKey + copy button, 6-digit TOTP input, `verifyEnrollTwoFactor`; **disable flow** — password confirm → `disableTwoFactor` `security.tsx:538-800`.
- **Active sessions (me)**: `["identity","sessions","me"]` → `getMySessions` (30s); sorted current-first; device rows (browser·OS, IP, last activity, expires, badges "this device"/"revoked"); Revoke per-session + "Sign out everywhere else" (`revokeAllOtherSessions` w/ revokedCount toast) `security.tsx:101-260`.
- Forms: controlled inputs + local validation, no RHF/zod. States: SessionsSkeleton, empty, error alert, disabled buttons.

### `/settings/appearance` (`src/pages/settings/appearance.tsx`, 570 lines)
- **Theme**: Light / System / Dark swatches via `useTheme()` `appearance.tsx:34-101`.
- **Accent**: 6 preset brand palettes + **Custom** → CustomAccentDialog (hue ribbon 0-360 slider, saturation/chroma 50-130% slider, brand ladder of 11 `--brand-*` stops, live chrome preview card w/ primary button) → `setCustomAccent(oklch stops)` `appearance.tsx:104-139, 342-569`.
- **Font**: 4 font families (lazy-loaded via `ensureLazyFontsLoaded`, rendered as swatches in their own typeface; mono stays JetBrains Mono) `appearance.tsx:141-159`.
- **Density**: compact/comfortable switch `appearance.tsx:161-179`. **Motion**: reduced-motion override switch `appearance.tsx:181-203`.
- State persisted via theme-provider (persisted to storage); no server calls on this page.

### `/settings/notifications` (`src/pages/settings/notifications.tsx`, 50 lines)
- **Placeholder** — honest stub: "Per-event preferences aren't tunable yet"; in-app notifications delivered via SignalR bell; granular email opt-ins roadmap; button programmatically clicks the topbar bell (`[data-notification-bell]`) `notifications.tsx:5-49`.

### `/settings/branding` (`src/pages/settings/branding.tsx`, 483 lines)
- **Query**: `["tenant","theme"]` → `getTenantTheme`; **background refetch disabled** (window-focus/reconnect) so it never clobbers unsaved edits; reseeds draft only on own save/reset invalidations `branding.tsx:42-62`.
- **Editor**: Light + Dark `PaletteEditor` (9 tokens: primary/secondary/tertiary/background/surface/error/warning/success/info) each w/ native color picker + hex input + per-palette reset; `BrandAssetsEditor` (logoUrl/logoDarkUrl/faviconUrl + delete-flag semantics + inline preview thumbnails); live `ThemePreview` card rendering buttons/pills with the chosen palette `branding.tsx:189-475`.
- **Mutations**: `updateTenantTheme` / `resetTenantTheme` w/ "default"/"unsaved" badges, dirty check (JSON compare), Reset/Save footer `branding.tsx:64-152`.
- Permission: tab renders only for `Tenants.UpdateTheme` (settings-layout gating); direct URL w/o permission → API 403 surfaced as `ErrorBand` (comment at `branding.tsx:23-36`).
- Forms: controlled inputs + native pickers (no RHF/zod).

### `/settings/api-keys` (`src/pages/settings/api-keys.tsx`, 51 lines)
- **Placeholder** — "API keys aren't available yet" (backend not built; route kept alive to avoid 404) + View roadmap button (GitHub) `api-keys.tsx:5-50`.

### `/settings` shell (`src/pages/settings/settings-layout.tsx`, 258 lines)
- **Tabs** (numbered editorial left nav desktop / horizontal scroll pills mobile): Profile, Security, Appearance, **Branding (perm `Permissions.Tenants.UpdateTheme`)**, Notifications, API keys — permission-filtered like the sidebar so a user never lands on a 403 `settings-layout.tsx:31-57`.
- Header = "Settings · {active label}" (`EntityPageHeader`); sticky rail nav w/ active indicator bar; `SettingsSection` reusable warm-paper card (title bar + footer bar) `settings-layout.tsx:212-258`.

### `/overview` (`src/pages/overview.tsx`, 1293 lines)
- **Queries** (all 60s): `["billing","usage"]` → `getUsageSnapshots`; `["billing","subscription","me"]` → `getMySubscription`; `["tenant","me","status"]` → `getMyStatus`; `["audits","recent","overview"]` → `listAudits` 24h top-5 (30s) `overview.tsx:947-967, 563-575`.
- **SSE**: `useSseStatus()` + `useSseEvents()` from `@/sse/sse-context` — Live events stat card (count + pulsing dot per status), System status card ("Stream live" Wifi + SSE badge / offline / waiting), **Live feed widget** (latest 5 SSE events, time + tone-badged type) `overview.tsx:46, 471-540, 737-766, 1166-1196`.
- **Greeting header**: date + tenant caption, "Good morning/afternoon/evening, {first}", Refresh + View activity + View audits buttons `overview.tsx:1006-1138`.
- **4 StatCards**: Plan (planKey + status), Valid for (days-left countdown w/ Active/InGrace warning→graceEnds/Expired danger, from `getMyStatus` expiryState), Resources (avg utilization + overage badge), Live events (SSE) `overview.tsx:200-262, 1021-1096`.
- **Widgets**: Subscription card (plan + status pulse badge, started/ends, term progress bar % + days left) `overview.tsx:349-465`; Recent audits list (severity stripe, type icon, actor, relative time, → /system/audits) `overview.tsx:562-651`; Usage by resource (bar rows, overage danger badge, ≥80% warning) `overview.tsx:64-78, 268-309`; Quick actions (4 tiles) `overview.tsx:657-730`.
- **FirstRunPanel**: welcome checklist (Pick a plan / Invite team / Browse catalog / Watch live) shown when no subscription && not dismissed (localStorage `fsh.firstrun.dismissed:{tenant}`) `overview.tsx:774-936`.
- States: per-widget skeletons + empty/error branches; no realtime beyond SSE. No permission gating (all tenant users).

### `/subscription` (`src/pages/subscription.tsx`, 540 lines)
- **Queries** (60s): `["tenant","me","status"]` → `getMyStatus`; `["billing","subscriptions","me"]` → `getMySubscription`; `["billing","usage"]` → `getUsageSnapshots`; `["billing","invoices","me",{page:1,size:5}]` → `getMyInvoices` `subscription.tsx:104-126`.
- **Plan card**: planKey + Active badge, Started/Ends dates, "Plan changes are operator-driven" notice; no-subscription state "Contact your operator" `subscription.tsx:218-287`.
- **Validity card**: `EntityStatusBadge` Active/In grace(warning)/Expired(danger) + Inactive badge; Valid until + Grace ends (warning) dates from expiryState `subscription.tsx:293-346`.
- **Usage by resource**: same bar-row component as overview (overage danger, ≥80% warning) `subscription.tsx:352-454`. **Recent invoices**: top-5 list, number + status badge + period + amount, row → /invoices/{id} `subscription.tsx:460-540`.
- `ErrorBand` for status/subscription errors `subscription.tsx:140-157`. No dialogs, no permission gating, no realtime.

### `/wallet` (`src/pages/wallet.tsx`, 410 lines)
- **Queries** (30s): `["billing","wallet","me"]` → `getMyWallet`; `["billing","topup-requests","me",{page}]` → `getMyTopupRequests` (20/page) `wallet.tsx:76-87`.
- **BalanceCard**: big `formatMoney` balance + low-balance warning (≤$10, empty-balance variant) `wallet.tsx:177-217`.
- **TopupRequestForm**: Amount (number, step 0.01, required) + Note textarea (maxLength 1000); `createTopupRequest` mutation — **per-call data through `mutate(arg)`** (execution-time race comment); invalidates wallet + requests keys `wallet.tsx:223-316`.
- **Top-up requests list**: desktop cols Requested (date + note) / Amount / Status (Pending→warning Invoiced→info Completed→success Rejected→danger Cancelled→default) / Invoice link (→ /invoices/{invoiceId}); mobile cards; `EntityPager` `wallet.tsx:47-67, 322-410`.
- States: `EntityEmpty`, `EntityListLoading`, `ErrorBand` (wallet + requests). No realtime, no dialogs.

### `/activity` (`src/pages/activity.tsx`, 193 lines)
- **Pure SSE page**: `useSseStatus()` + `useSseEvents()`; renders latest **200 events** (`events.slice(0,200)`); header badge streaming/offline/connecting; per-event tone heuristic (fail/error/revoke→danger, warn/retry→warning, login/issued/created→success, token/auth→info) `activity.tsx:41-48, 69-92`.
- **Table**: desktop cols Action (type badge + payload JSON summary) / Entity (extracted `entityId|aggregateId|id|tenantId|userId`) / Time; mobile cards; `role="log" aria-live="polite"` for screen readers `activity.tsx:115-148, 157-193`.
- Shows `${shown} shown · ${total} total`; empty states (live vs disconnected); no pagination/filters — bounded ring buffer from SSE context. No permission gate, no forms.

### `/invoices` (`src/pages/invoices.tsx`, 331 lines)
- **Query** (30s, `keepPreviousData`): `["billing","invoices","me",{page,size:20}]` → `getMyInvoices` `invoices.tsx:74-79`.
- **Table**: desktop cols Invoice # (+ period) / Customer (tenantId) / Amount / Status (Paid→success Issued→info Void→danger Draft→default) / Due date (Issued→warning due date, Paid→paid date) `invoices.tsx:193-208, 268-331`; mobile cards → `/invoices/{id}` `invoices.tsx:231-266`.
- **Search**: client-side filter over current page only (invoiceNumber/status/period); pagination **suppressed while searching** `invoices.tsx:98-112, 211-220`.
- `EntityPager`, `EntitySearch`, `EntityListLoading`, `EntityEmpty` (+ "Clear search" action), `ErrorBand`. No realtime; no permission gate.

### `/invoices/:invoiceId` (`src/pages/invoice-detail.tsx`, 396 lines)
- **Query**: `["billing","invoices",id]` → `getMyInvoice(id)` (enabled only when id present) `invoice-detail.tsx:52-56`.
- **Header**: gradient accent bar, invoice number + status badge + purpose badge, period + amount, **Download PDF** button → `downloadInvoicePdf(id, invoiceNumber)` with toast on error `invoice-detail.tsx:115-184`.
- **Line items table**: Description (+kind badge, resource) / Qty / Unit price / Amount + Total footer `invoice-detail.tsx:190-284`.
- **Details card**: status, currency, period, created/issued/due (warning when Issued)/paid (success)/voided (danger)/period-start/end `invoice-detail.tsx:290-325`; **Notes** card `invoice-detail.tsx:102-108`.
- `EntityDetailBack`, `DetailSkeleton`, `NotFoundPanel` ("Invoice not found — may not belong to your tenant"), `ErrorBand`. No realtime.

### `/catalog/products` (`src/pages/catalog/products.tsx`, 1409 lines)
- **Query**: `["catalog","products",{search,brandId,categoryId,isActive,pageNumber,pageSize:25}]` → `searchProducts` (sortBy createdAtUtc desc, `keepPreviousData`) `products.tsx:197-222`; plus `searchBrands`/`searchCategories` (pageSize 200, 60s) for filter/editor combos `products.tsx:224-233`.
- **Search**: debounced 250ms client input (name/SKU/slug placeholder) w/ Clear `products.tsx:272-296`. **Filters**: Brand/Category searchable clearable Comboboxes + Active/Hidden pill toggle; filters reset page to 1 `products.tsx:87-169, 193-195`.
- **Table** (desktop): Product (image w/ letter fallback, name, Hidden badge) / SKU / Brand badge + Category / Price (click → change) + StockChip (click → adjust; danger=0, warning<10) / row hover actions Edit + Delete + chevron; row → `/catalog/products/{id}`; mobile cards `products.tsx:412-610, 709-805`.
- **Dialogs**: **ProductEditorDialog** create+edit (Name, SKU fixed after create, Brand/Category Comboboxes required, Price/Currency/Stock on create only, Description, Visibility switch on edit; validations price>=0 stock>=0; `createProduct`/`updateProduct`) `products.tsx:811-1081`; **PriceDialog** (`changeProductPrice`, Was→Becomes delta, emits ProductPriceChanged event) `products.tsx:1087-1213`; **StockDialog** (`adjustProductStock` ± stepper, negative guard, emits ProductStockAdjusted) `products.tsx:1215-1350`; **DeleteProductDialog** (`deleteProduct`, invalidates catalog+trash keys) `products.tsx:1356-1409`.
- Forms: controlled + manual validation (no RHF/zod). States: LoadingList, EmptyResults (search vs no-products, Clear filters / Add product), error alert. No realtime; no permission gate on page (row actions are global catalog perms at API).

### `/catalog/brands` (`src/pages/catalog/brands.tsx`, 632 lines)
- **Query** (keepPreviousData): `["catalog","brands",{search,page,size:20}]` → `searchBrands` (sort createdAtUtc desc) `brands.tsx:90-105`.
- **Search** (debounced 250ms, name/slug) via `EntitySearch` `brands.tsx:130-134`. **Table** (desktop): Brand (avatar/initials + name + description) / Slug / Created (date + relative) / hover Edit+Delete; mobile `EntityMobileCard` `brands.tsx:245-362`.
- **BrandEditorDialog**: Name (required), Slug auto-preview (`slugify`), Description, Logo URL; `createBrand`/`updateBrand` `brands.tsx:409-569`. **DeleteBrandDialog**: warns "products referencing this brand will need to be reassigned"; invalidates catalog+trash `brands.tsx:575-632`.
- States: `EntityListLoading`, `EntityEmpty` (+Clear/Add actions), error alert. Forms: controlled (no RHF/zod).

### `/catalog/categories` (`src/pages/catalog/categories.tsx`, 720 lines)
- **Queries**: `["catalog","categories",{search,page,size:50}]` → `searchCategories` (sort name asc) + `["catalog","categories","tree"]` → `getCategoryTree` (30s) for parent-name lookup & editor options `categories.tsx:116-137`.
- **Tree-aware parent picker**: flat tree with depth-indented "↳" prefixes + root labels; excludes self/descendants as parent `categories.tsx:497-503, 583-619`.
- **Table**: Category (initials avatar, name, "under {parent}"/"root" + description) / Slug / Created (+relative) / hover Edit+Delete; mobile cards `categories.tsx:300-450`.
- **CategoryEditorDialog**: Name (required), Slug auto-preview, Parent Combobox (root default), Description; `createCategory`/`updateCategory` `categories.tsx:456-656`. **DeleteCategoryDialog**: "Categories with child categories cannot be deleted"; invalidates catalog+trash `categories.tsx:662-720`.
- States: `EntityListLoading`, `EntityEmpty`, error alert. Forms: controlled (no RHF/zod).

### `/catalog/products/:productId` (`src/pages/catalog/product-detail.tsx`, 1153 lines)
- **Queries**: `["catalog","products",productId]` → `getProductById`; `getBrandById`/`getCategoryById` (60s, enabled on id) `product-detail.tsx:102-122`.
- **Hero**: `EntityDetailHero` w/ avatar, Active/Hidden badge, SKU·brand·category subtitle, Refresh/Edit/Delete actions, stats (price / stock tone out-of-stock|low<10|in-stock / image count), meta links brand→`/catalog/products?brand=`, category→`?category=` `product-detail.tsx:249-403`.
- **Sidebar**: Pricing panel (Change price) `product-detail.tsx:409-437`; Inventory panel (Adjust stock, out-of-stock warning) `product-detail.tsx:439-490`; Identifiers (SKU/slug/id/brand-id/category-id) `product-detail.tsx:492-516`.
- **Right column**: Description (+Edit) `product-detail.tsx:544-558`; **Images via `ProductImageManager`** (upload/multi-image, star-a-cover, invalidates `["catalog","products",id]`) `product-detail.tsx:198-208`; Audit panel (created/revised/status) `product-detail.tsx:518-542`.
- **Dialogs**: Edit (name/SKU-locked/brand+category Comboboxes/description/visibility switch) `product-detail.tsx:672-842`; Delete (→ navigate back) `product-detail.tsx:844-897`; Price (Was→Becomes delta) `product-detail.tsx:899-1019`; Stock (stepper, negative guard) `product-detail.tsx:1021-1153`.
- States: `DetailSkeleton`, `NotFoundPanel`, `ErrorBand`. No realtime; no permission gate.

## 6. One-line-per-route summary (parity checklist)

| Route | Page file | Present | Feature notes |
|---|---|---|---|
| `/login` | `src/pages/login.tsx` | yes | Tenant/email/password, demo accounts, forgot link, signed-out notice |
| `/forgot-password` | `src/pages/auth/forgot-password.tsx` | yes | Anti-enumeration "check inbox" |
| `/reset-password` | `src/pages/auth/reset-password.tsx` | yes | Strength meter, token/email/tenant params |
| `/confirm-email` | `src/pages/auth/confirm-email.tsx` | yes | Loading/success/error; no auto-redirect |
| `/tenant-deactivated` | `src/pages/tenant-deactivated.tsx` | yes | Terminal; logout + back to sign in |
| `/impersonation-ended` | `src/pages/impersonation-ended.tsx` | yes | Terminal; dev-only reason from router state |
| `/` overview | `src/pages/overview.tsx` | yes | 4 KPI cards, SSE feed, audits, usage bars, first-run panel |
| `/activity` | `src/pages/activity.tsx` | yes | Pure SSE stream, 200-event buffer, tone-coded |
| `/subscription` | `src/pages/subscription.tsx` | yes | Plan/validity cards, usage bars, recent invoices |
| `/wallet` | `src/pages/wallet.tsx` | yes | Balance, top-up form, requests table, invoice links |
| `/invoices` | `src/pages/invoices.tsx` | yes | Client-side page filter, status tones |
| `/invoices/:id` | `src/pages/invoice-detail.tsx` | yes | Line items, download PDF, details/notes |
| `/system/health` | `src/pages/health.tsx` | yes | 10s polling, history pips, collapsible deps |
| `/system/audits` | `src/pages/audits.tsx` | yes | Range/type/severity/advanced filters, summary strip, drawer + related timeline |
| `/system/trash` | `src/pages/system/trash.tsx` | yes | 5 perm-gated tabs, restore-only |
| `/system/sessions` | `src/pages/system/sessions.tsx` | yes | 30s polling, revoke + revoke-all |
| `/files` | `src/pages/files/my-files.tsx` | yes | Tabs, dropzone upload, kind chips, preview dialog |
| `/chat` | `src/pages/chat/chat-page.tsx` | yes | SignalR; auto-select first channel |
| `/chat/:channelId` | `src/pages/chat/chat-page.tsx` | yes | Header, pinned bar, messages, typing, composer, search, settings |
| `/tickets` | `src/pages/tickets/tickets.tsx` | yes | Status/priority pills, create dialog |
| `/tickets/:ticketId` | `src/pages/tickets/ticket-detail.tsx` | yes | Conversation, resolve/assign/reopen, properties |
| `/identity` | redirect | yes | → `/identity/users` |
| `/identity/users` | `src/pages/identity/users.tsx` | yes | Filters, register dialog |
| `/identity/users/:userId` | `src/pages/identity/user-detail.tsx` | yes | Perm-gated actions, role assignment, sessions, impersonate |
| `/identity/roles` | `src/pages/identity/roles.tsx` | yes | Create → role detail |
| `/identity/roles/:roleId` | `src/pages/identity/role-detail.tsx` | yes | Permission editor, presets, system-role read-only |
| `/identity/groups` | `src/pages/identity/groups.tsx` | yes | Create dialog, default-group flag |
| `/identity/groups/:groupId` | `src/pages/identity/group-detail.tsx` | yes | Members add/remove, role attach |
| `/catalog` | redirect | yes | → `/catalog/brands` |
| `/catalog/brands` | `src/pages/catalog/brands.tsx` | yes | CRUD, slug preview, logo URL |
| `/catalog/categories` | `src/pages/catalog/categories.tsx` | yes | CRUD, tree parent picker, cycle guard |
| `/catalog/products` | `src/pages/catalog/products.tsx` | yes | Filters, price/stock/delete dialogs |
| `/catalog/products/:productId` | `src/pages/catalog/product-detail.tsx` | yes | Pricing/inventory/identifiers, image manager, domain-event dialogs |
| `/settings` | `src/pages/settings/settings-layout.tsx` | yes | Shell; index → profile |
| `/settings/profile` | `src/pages/settings/profile.tsx` | yes | Name/phone/photo, read-only email |
| `/settings/security` | `src/pages/settings/security.tsx` | yes | Password, 2FA enroll/disable, my sessions |
| `/settings/appearance` | `src/pages/settings/appearance.tsx` | yes | Theme/accent/font/density/motion |
| `/settings/branding` | `src/pages/settings/branding.tsx` | yes | Palette editor, brand assets, preview (perm-gated) |
| `/settings/notifications` | `src/pages/settings/notifications.tsx` | yes | Placeholder stub |
| `/settings/api-keys` | `src/pages/settings/api-keys.tsx` | yes | Placeholder stub |
| `*` 404 | `src/pages/not-found.tsx` | yes | Styled 404 + command palette action |

### Unusual / audit-relevant findings

1. **Two distinct realtime stacks**: SSE (`src/sse/sse-context.tsx`, token-issued stream at `/api/v1/sse/stream`) for overview/activity/topbar status dot, and **SignalR** (`src/realtime/realtime-context.tsx`, `/api/v1/realtime/hub`) for chat + notifications. Do not conflate in the Blazor port.
2. **No react-hook-form / zod anywhere** in the dashboard (that's admin-app-only); all forms are controlled inputs with manual validity checks — parity target should not expect RHF/zod.
3. **`/settings/notifications` and `/settings/api-keys` are honest placeholder stubs** (not yet built), though both routes exist and are nav-visible.
4. **Trash is restore-only** — no permanent-delete UI in the dashboard React app.
5. **`/identity/roles` client-generates the GUID** (`newGuid`) before `upsertRole`.
6. **Brand/category/product delete invalidates both `["catalog",…]` and `["trash",…]` query keys** — the trash page and source list stay in sync.
7. **`/invoices` search is client-side over the current page only** (backend has no search param); pagination is suppressed while searching.
8. **Health/sessions use polling** (10s / 30s) — they are not SSE or SignalR consumers.
9. **Overview first-run panel dismissal is per-tenant** via `localStorage fsh.firstrun.dismissed:{tenantId}`.
10. **Row-level domain-event dialogs** (price/stock on products) explicitly reference `ProductPriceChanged` / `ProductStockAdjusted` domain events in their descriptions — parity should preserve the domain-event framing.
