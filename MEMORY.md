# MEMORY
Last updated: 2026-10-05

## Vừa làm gì
- Hoàn thành **Task 2: Auth Register UI (F1)** và Refactor **Task 1: Auth Register API (F1)**.
- Triển khai màn hình Đăng ký bằng Next.js + React Hook Form + Zod, áp dụng chuẩn Vibe Code V5 (Glassmorphism, Dark Mode).
- Cấu hình `vitest`, mock `sonner`, sử dụng Proxy rewrites trong `next.config.ts` để fix lỗi CORS.
- TDD vòng lặp RED -> GREEN -> REFACTOR thành công.
- Refactor API: Bổ sung `ConfirmPassword`, thêm cơ chế Normalization (0xxxxxxxxx) cho số điện thoại, thiết lập Unique Index cho bảng PhoneNumber, xóa trường `Cccd` khỏi luồng Đăng ký và đưa vào trạng thái nullable, bổ sung `EmailConfirmed`.
- Triển khai `RegisterCommandValidator` sử dụng `FluentValidation`.
- Hoàn tất Migration DB.

## Việc dở dang
- Không có việc dở dang cho Task 2. Mọi thứ đã pass test 100%.

## Lưu ý cho phiên sau
- Frontend hiện đang giao tiếp với Backend qua đường dẫn Proxy `/api/...` trỏ tới `localhost:5000`.
- Cần chú ý khi thiết kế Form Login (Task 4) sẽ dùng chung bộ UI và cấu trúc TDD Vitest như Register.
- Cột Cccd đã thành nullable, sẽ sử dụng trong tính năng KYC sau này.

## Task tiếp theo
- **Task 3: Auth Login API & JWT (F1)**: Xây dựng cơ chế đăng nhập và sinh JWT (HttpOnly Cookie).
