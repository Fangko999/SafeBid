# MEMORY
Last updated: 2026-10-05

## Vừa làm gì
- Hoàn thành **Task 4: Auth Login UI (F1)**.
- Triển khai màn hình Đăng nhập (`/login`) bằng React Hook Form và Zod theo chuẩn Vibe Code V5.
- Cài đặt thư viện `zustand` để quản lý State toàn cục ở frontend (`authStore.ts`).
- Tạo `<AuthProvider>` bọc ngoài `layout.tsx` để tự động xác thực phiên (gọi `/api/users/me`) khi reload ứng dụng, giữ trạng thái Zustand đồng bộ với HttpOnly Cookie.
- TDD vòng lặp RED -> GREEN (100% Passed) cho các ca: validate rỗng, đăng nhập thành công và đăng nhập sai.

## Việc dở dang
- Đã hoàn tất hoàn toàn Phase 1 (Core & Foundation). Module Auth đã chốt (Completed).

## Lưu ý cho phiên sau
- Backend xử lý Auth qua JWT (HttpOnly Cookie), mọi API sau này nếu yêu cầu đăng nhập thì sử dụng attribute `[Authorize]`.
- Ở Frontend, `useAuthStore` là single source of truth cho thông tin người dùng. Khi muốn check quyền, đọc `isAuthenticated`.
- Đã hoàn thành CHECKPOINT REVIEW 2, tag `checkpoint-2`. Các vấn đề bảo mật (Bcrypt DoS, Cookie Secure) và Validation đã được xử lý triệt để.

## Task tiếp theo
- **Task 5: Deposit Webhook Core (F3)**: Áp dụng SPIKE 2 vào nghiệp vụ nạp tiền thực tế.
