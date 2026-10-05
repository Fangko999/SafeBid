# MEMORY
Last updated: 2026-10-05

## Vừa làm gì
- Hoàn thành **Task 5: Deposit Webhook Core (F3)**.
- Đã tạo `LedgerEntry` để áp dụng sổ cái kép (Double-entry) thay vì `WalletTransaction` đơn giản.
- Đã thiết lập CQRS Command xử lý Webhook. Tích hợp `HmacAuthFilter` để chống Replay Attack & Idempotency.
- Xử lý edge case tiền nạp của User bị ban (IsConfiscated=true) thì đẩy thẳng vào quỹ bảo hiểm (LedgerEntry với `WalletId = null`).
- Bổ sung `ADR 0002` cho quyết định dùng hệ thống kế toán sổ kép.

## Việc dở dang
- Không có.
- Ghi chú: Có một điểm yếu trong kiến trúc `HmacAuthFilter` (lưu Idempotency Key trước khi DB lưu thành công). Tạm thời chấp nhận theo nguyên bản Spike 2, có thể refactor sau này.

## Lưu ý cho phiên sau
- Entity `LedgerEntry` là nền tảng tài chính của hệ thống. Bất kỳ khi nào thao tác với số dư (Wallet), BẮT BUỘC phải sinh LedgerEntry tương ứng để đối soát.

## Task tiếp theo
- **Task 6: Wallet Concurrency API (F3)**: Quản lý số dư và ngăn trừ lố bằng RowVersion.
