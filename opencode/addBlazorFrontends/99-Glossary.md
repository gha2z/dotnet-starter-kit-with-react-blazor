# Glossary — Acronyms & Terms
Last Update: 2026-Aug-03 18:45:55, by: opencode (auto/coding, model: mimo-v2.5-free).

> Every acronym used in this project, explained in plain English.

## A–C

| Term | Stands For | Plain English |
|------|-----------|---------------|
| **AOT** | Ahead-Of-Time compilation | Compiles C# to native code before running (vs JIT — Just In Time). Faster execution, larger binary. |
| **API** | Application Programming Interface | The backend endpoints that the front-end calls. In this project: `localhost:7030/scalar` lists them all. |
| **AppHost** | Application Host | The .NET Aspire orchestrator. Starts Postgres, Redis, MinIO, the API, and front-ends in one command. |
| **Aspire** | .NET Aspire | Microsoft's cloud-ready stack for building distributed apps. Includes service discovery, orchestration, telemetry. |
| **bUnit** | Blazor Unit Testing | A library that lets you render Blazor components in tests and assert on their output. Think "React Testing Library for Blazor." |
| **CORS** | Cross-Origin Resource Sharing | A browser security feature. The API allows requests from `localhost:5175` (Blazor) via CORS headers. |
| **CSP** | Content Security Policy | An HTTP header that restricts what scripts/styles a page can load. Can block `eval()` used by JS interop. |

## D–I

| Term | Stands For | Plain English |
|------|-----------|---------------|
| **DI** | Dependency Injection | A pattern where services are provided to classes automatically, instead of classes creating their own dependencies. Blazor uses DI extensively. |
| **DTO** | Data Transfer Object | A simple object that carries data between client and server. No logic, just properties. |
| **EF Core** | Entity Framework Core | Microsoft's object-relational mapper (ORM). Lets you write C# instead of SQL for database operations. |
| **GUI** | Graphical User Interface | The visual part of the app — buttons, forms, tables. What the user clicks on. |
| **Hub** | SignalR Hub | A server-side endpoint that manages real-time connections. Used for notifications and chat. |
| **HTTP** | HyperText Transfer Protocol | The protocol browsers use to talk to servers. `GET` = read, `POST` = create, `PUT` = update, `DELETE` = delete. |
| **HTTPS** | HTTP Secure | HTTP encrypted with TLS. The padlock icon in your browser. |
| **IL** | Intermediate Language | C# code is compiled to IL (like assembly language for .NET). IL is then JIT-compiled or AOT-compiled to machine code. |
| **IoC** | Inversion of Control | Another name for DI. Services are "inverted" — the container gives them to you, you don't create them. |

## J–M

| Term | Stands For | Plain English |
|------|-----------|---------------|
| **JS** | JavaScript | The language that runs in browsers. Blazor WASM uses JS interop to access browser APIs. |
| **JS Interop** | JavaScript Interoperability | Blazor's ability to call JavaScript functions from C# and vice versa. The bridge between C# and the browser. |
| **JSON** | JavaScript Object Notation | A text format for data. `{"name": "Alice", "age": 30}`. APIs use JSON to send/receive data. |
| **JWT** | JSON Web Token | A token that proves identity. Contains encoded JSON (user ID, permissions, expiry). Signed by the server. |
| **MAUI** | Multi-platform App UI | .NET's cross-platform framework for mobile/desktop apps (Android, iOS, Windows, macOS). |
| **MudBlazor** | MudBlazor | The UI component library used by both WASM apps. Think "Material Design for Blazor." |
| **MudTheme** | MudBlazor Theme | The color/font/layout configuration for MudBlazor components. Admin uses green; Dashboard uses rose. |

## N–P

| Term | Stands For | Plain English |
|------|-----------|---------------|
| **NuGet** | .NET's package manager | Like `npm install` for .NET. `dotnet add package MudBlazor` installs the library. |
| **OIDC** | OpenID Connect | An authentication protocol built on top of OAuth 2.0. Used for SSO (single sign-on). |
| **ORM** | Object-Relational Mapper | Translates between C# objects and database tables. EF Core is the ORM in this project. |
| **Playwright** | Playwright | Microsoft's E2E testing framework. Tests what the user sees: clicks, typing, navigation. |
| **PostgreSQL** | PostgreSQL | The database. Open-source, powerful, used by the API to store data. |
| **PWA** | Progressive Web App | A web app that can be "installed" on the user's device. Works offline, has an app icon. |
| **RCL** | Razor Class Library | A NuGet package that contains Razor components. `BlazorShared` is an RCL. Both WASM apps reference it. |
| **Redis** | Redis | In-memory data store. Used for caching and Hangfire job storage. |

## R–Z

| Term | Stands For | Plain English |
|------|-----------|---------------|
| **RIL** | Runtime Intermediate Language | IL that runs on Mono/WASM. The WASM runtime interprets this to execute C# code in the browser. |
| **SDK** | Software Development Kit | A collection of tools, libraries, and documentation for building software for a specific platform. |
| **SignalR** | SignalR | ASP.NET Core's real-time library. Enables server-to-client push (notifications, chat). |
| **SPA** | Single Page Application | An app that loads once and updates the page without full reloads. Blazor WASM and React are SPAs. |
| **SQL** | Structured Query Language | The language used to query databases. `SELECT * FROM Users WHERE Email = 'alice@example.com'` |
| **SSE** | Server-Sent Events | A simpler alternative to WebSockets for one-way server-to-client streaming. Used by the dashboard for live stats. |
| **SSL** | Secure Sockets Layer | The old name for TLS. The padlock icon. |
| **TLS** | Transport Layer Security | Encrypts data between browser and server. Prevents eavesdropping. |
| **WASM** | WebAssembly | A binary format that browsers can execute. C# compiled to WASM runs in the browser. |
| **WebSocket** | WebSocket | A persistent two-way connection between browser and server. Used by SignalR for real-time features. |

---

## Project-Specific Terms

| Term | Meaning |
|------|---------|
| **FSH** | FullStackHero — the project name |
| **BlazorShared** | The RCL shared between admin-blazor, dashboard-blazor, and (future) MAUI Hybrid |
| **AuthDelegatingHandler** | The middleware that attaches auth headers and handles 401→refresh|
| **FshMudTheme** | The custom MudBlazor theme with FSH brand colors |
| **FshPermissionGate** | A component that shows/hides content based on user permissions |
| **FshConfirmDialog** | A dialog component (replaces MudBlazor's removed `ShowMessageBox`) |
| **FshPageHeader** | A reusable header with icon, title, description, action buttons |
| **FshErrorBoundary** | A component that catches render errors and shows a reload button |
| **FshAdmin** / **FshDashboard** | The localStorage key prefix for each app's token store |

---

## .NET Versions

| Package | Version | Notes |
|---------|---------|-------|
| .NET SDK | 10.0.xxx | Target framework for all projects |
| MudBlazor | 9.7.0 | UI component library |
| Microsoft.AspNetCore.* | 10.0.10 | Blazor WASM, Authorization, SignalR Client |
| System.IdentityModel.Tokens.Jwt | 8.16.0 | JWT decode/validation |
| Microsoft.Extensions.Http | 10.0.10 | HttpClient factory and named clients |
| bUnit | 2.0.66 | Blazor component testing |
| .NET Aspire | 13.4.6 | Cloud orchestration (AppHost) |

---

*Add new terms as you encounter them. This glossary grows with the codebase.*
