# 5. Frontend State Management using Zustand

Date: 2026-10-05

## Status

Accepted

## Context

For the frontend (Next.js), we need a reliable way to manage global state, specifically the authentication state (`isAuthenticated` and `user` profile data). The Next.js framework provides Context API, but it often leads to unnecessary re-renders and boilerplate when the app scales.

## Decision

We chose **Zustand** as our global state management library over Redux, Recoil, or pure Context API.

1. **Simplicity:** Zustand requires minimal boilerplate, using a simple hook-based store (`useAuthStore`).
2. **Performance:** It allows components to subscribe to specific parts of the state (e.g. `state => state.isAuthenticated`), preventing unnecessary re-renders.
3. **Integration with HttpOnly Cookie Auth:** Since our tokens are stored in HttpOnly cookies and not accessible by JS, Zustand stores the *derived* state. A dedicated `AuthProvider` component will call a `checkAuth()` method from the store on application mount (page refresh) to synchronize the Zustand state with the active session in the Backend.

## Consequences

**Positive:**
- Fast, minimal boilerplate, extremely readable state management.
- Well-suited for React Server/Client Component environments since it only runs on the client.

**Negative:**
- Developers need to remember not to use `persist` middleware to store sensitive authentication states locally (since token security relies on HttpOnly cookies). Only public/UI state should be persisted if necessary.
