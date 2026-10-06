# MEMORY
Last updated: 2026-10-06

## Vừa làm gì
- Hoàn thành **Task 7: Wallet UI (F3)**.
- Backend: Bổ sung `PaginatedResult<T>`, `GetWalletTransactionsQuery`, và API endpoint `/api/wallet/transactions`.
- Frontend: Cài đặt Shadcn UI (Table, Badge, Skeleton, Pagination).
- Frontend: Tạo trang Ví (Wallet) với giao diện Glassmorphism tuyệt đẹp.
- Áp dụng **4 trạng thái UI**: Loading Skeleton, Empty State, Error State (có nút Thử lại), và Success State.
- Xử lý **Race Condition ở Frontend**: Nếu rút tiền trả về HTTP 409, `fetch` sẽ chờ ngẫu nhiên 300-500ms và retry ngầm tối đa 2 lần.

## Việc dở dang
- Không có.

## Lưu ý cho phiên sau
- Backend hiện đã trả về 400 Insufficient Funds khi ví trống và 409 Conflict khi gặp Race Condition.
- Tính năng rút tiền hiện tại đang thao tác cập nhật trực tiếp `HoldAmount`, admin phê duyệt/từ chối chưa được làm.
- Cấu trúc `PaginatedResult<T>` cần được sử dụng lại cho các API get list sau này.

## Task tiếp theo
- **CHECKPOINT 3**: Kiểm duyệt tổng thể trước khi bắt tay vào các tính năng Đấu giá phức tạp (T8, T9, T10, T11).
