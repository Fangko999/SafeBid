# ADR 0002: Use LedgerEntries for Double-Entry Accounting

## Status
Accepted

## Context
When handling financial operations (deposits, bidding holds, escrow, refunds), we need a robust audit trail. Initially, we considered a simple `WalletTransaction` log or just updating balances in the `Wallet` entity directly. However, tracking money movement (e.g., holding funds for a bid and releasing them) requires strict correctness, auditability, and preventing money out of thin air.

## Decision
We implemented a double-entry accounting approach using a `LedgerEntry` entity instead of a simple `WalletTransaction`. 
- Every financial movement creates immutable ledger entries.
- To handle edge cases like confiscated funds (User is banned), we create a LedgerEntry with `WalletId = null` and type `DEPOSIT_CONFISCATION`. This ensures the ledger balances correctly (the system received the money) while keeping the funds away from the user's available balance and redirecting it to the System Insurance Fund.

## Consequences
- **Positive:** True financial auditability. Easy to trace where money is held and where it went. Easier to generate system-wide balance sheets.
- **Negative:** Increased database inserts per operation (multiple ledger entries for a single logical action, although for deposits it's just one entry). Complexity in ensuring entries balance out perfectly in application logic.
