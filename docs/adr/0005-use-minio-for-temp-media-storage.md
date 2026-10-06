# 5. Use MinIO for Temp Media Storage

Date: 2026-10-06

## Status
Accepted

## Context
In the Auction system, users (Sellers) need to upload media (images, videos) for their auction drafts before actually publishing them. If the draft is abandoned or deleted, we need a way to clean up the temporary media files to prevent storage bloat. Additionally, storing raw media in the relational database is an anti-pattern.

## Decision
We will use MinIO (an S3-compatible object storage server) as our media storage backend. 
- We will store temporary, unpublished media in a specific bucket named `temp-media`.
- We will integrate directly with the `Minio` SDK in the `.NET` application through an `IStorageService` abstraction.
- For integration testing, we will use `Testcontainers.Minio` with the `elestio/minio:latest` image to spin up a real MinIO instance during the test lifecycle, ensuring tests cover the actual stream and network I/O.

## Consequences
- **Positive:** Keeps the SQL Database lightweight.
- **Positive:** Enables easy configuration of bucket lifecycle policies (e.g., auto-delete objects in `temp-media` older than 48 hours).
- **Positive:** MinIO is S3-compatible, allowing seamless migration to AWS S3 or other cloud providers if needed in the future.
- **Negative:** Adds a new infrastructure dependency (MinIO) that needs to be deployed, monitored, and backed up in production.
- **Negative:** Increased complexity in local development (requires Docker).
