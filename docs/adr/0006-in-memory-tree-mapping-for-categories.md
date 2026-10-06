# 6. In-Memory Tree Mapping for Categories

Date: 2026-10-06

## Status
Accepted

## Context
The system requires a nested category structure (parent-child relationship) for Auctions. To serve this to the Frontend, the API needs to return a hierarchical tree structure. 
Using lazy loading or nested SQL includes in Entity Framework can lead to the N+1 query problem or highly complex SQL queries (like Recursive CTEs). Furthermore, creating categories can result in circular references (e.g., A is parent of B, B is parent of A) which break tree traversal and lead to StackOverflows.

## Decision
- **Read Logic**: We fetch all categories in a single flat list query (`ToListAsync()`) and perform the Parent-Child tree mapping in-memory (using a Dictionary). Because the total number of categories is relatively small (typically < 10,000), this RAM-based operation is significantly faster and prevents N+1 queries entirely.
- **Write Logic**: We encapsulate loop-prevention logic directly in the Domain Entity (`Category.SetParent()`), enforcing an iterative check up the parent chain before allowing any parent ID assignment. 
- **Database**: We disable Cascade Delete (`DeleteBehavior.Restrict`) on the self-referencing `ParentId` foreign key to comply with SQL Server's Multiple Cascade Path rules.

## Consequences
- **Positive:** High performance for reading the category tree.
- **Positive:** Domain layer robustly protects against data corruption.
- **Negative:** If the number of categories grows exponentially (e.g., millions), the flat fetch approach will consume too much memory, at which point caching or pagination will be necessary.
