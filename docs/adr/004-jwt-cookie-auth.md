# 4. Authentication using HttpOnly Cookies

Date: 2026-10-05

## Status

Accepted

## Context

We needed a secure way to authenticate users and maintain their session state between the Next.js frontend and the .NET backend. The traditional approach of storing JWTs in `LocalStorage` and sending them via the `Authorization: Bearer <token>` header exposes the token to Cross-Site Scripting (XSS) attacks. Since SafeBid is an auction platform dealing with financial transactions, security is a high priority.

## Decision

We will use **HttpOnly Cookies** to store the JWT instead of `LocalStorage`. 

1. When a user logs in via `POST /api/auth/login`, the backend will generate a JWT and set it in an `HttpOnly`, `SameSite=Lax` cookie.
2. The frontend will communicate with the backend through a Next.js rewrite proxy (`/api/*` -> `http://localhost:5000/api/*`). Because the request appears to be same-origin to the browser, it will automatically attach the cookie to all subsequent API requests.
3. The frontend will rely on a `GET /api/users/me` endpoint to verify the session and fetch user details on page load or when needed, managed globally via a Zustand store.

## Consequences

**Positive:**
* Complete immunity to XSS attacks targeting token theft, as Javascript cannot access HttpOnly cookies.
* Simplified frontend HTTP client logic (no need to manually attach Bearer headers, `fetch` will include cookies natively).
* Better synergy with Next.js SSR, as cookies can be read by the Next.js server if needed.

**Negative:**
* Susceptible to CSRF (Cross-Site Request Forgery), though modern browsers using `SameSite=Lax` or `Strict` heavily mitigate this risk.
* Requires CORS setup to accept credentials if the frontend and backend ever move to different origins (e.g., `api.safebid.com` vs `safebid.com`), though currently mitigated by the Next.js proxy rewrite strategy.
