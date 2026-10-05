# 1. Adopt Clean Architecture, CQRS, and Result Pattern

Date: 2026-10-05

## Status

Accepted

## Context

We need a structured way to separate business rules from framework concerns (like EF Core or HTTP). Auth registration required creating multiple entities (`User`, `Wallet`) in a single transaction, hashing passwords, and returning strongly typed domain errors instead of throwing generic exceptions.

## Decision

We decided to adopt the following architectural patterns:
1. **Clean Architecture**: Separated the solution into `SafeBid.Domain`, `SafeBid.Application`, `SafeBid.Infrastructure`, and `SafeBid.Api` class libraries.
2. **CQRS with MediatR**: Used `MediatR` to separate commands (e.g., `RegisterCommand`) and queries. This keeps controllers thin.
3. **Result Pattern**: Implemented `Result<T>` and `Error` objects in the Domain layer to avoid exception-driven control flow, specifically returning `DomainErrors.User.DuplicateEmail`.
4. **Integration Testing Fixtures**: Implemented a shared `ICollectionFixture<ApiTestFixture>` using Testcontainers to ensure tests run sequentially without race conditions during database schema creation.

## Consequences

- **Positive**: High testability, decoupled dependencies, predictable error handling, and parallel-safe (or sequentially safe) test execution.
- **Negative**: Adds initial boilerplate (MediatR commands/handlers, Result pattern wrappers) which takes slightly more time to implement for simple CRUD operations.
