# MEMORY
Last updated: 2026-10-06

## Vừa làm gì
- Hoàn thành **Task 6: Wallet Concurrency API (F3)**.
- Đã tạo Entity `WithdrawalRequest` để tách biệt yêu cầu rút tiền khỏi lịch sử giao dịch gốc.
- Áp dụng Optimistic Concurrency (`RowVersion`) cho `Wallet` để ngăn chặn trừ lố tiền khi có Race Condition.
- Implement API `/api/wallet/withdraw` bắt lỗi `DbUpdateConcurrencyException` thành HTTP 409 Conflict.
- Viết integration test với C# HttpClient nhúng trong PowerShell để verify tính đồng thời ở cấp phần nghìn giây.

## Việc dở dang
- Không có.

## Lưu ý cho phiên sau
- User hiện tại yêu cầu rút tiền sẽ chuyển một phần `AvailableBalance` sang `HoldAmount`. Lệnh rút ở trạng thái `PENDING` chờ admin xử lý (Task 30). Sổ cái kép (LedgerEntry) vẫn tiếp tục được đảm bảo.

## Task tiếp theo
- **Task 7: Wallet UI (F3)**: Màn hình hiển thị số dư ví và lịch sử giao dịch phía Frontend (Next.js).
