# MEMORY
Last updated: 2026-10-05

## Vừa làm gì
- Hoàn thành **Task 3: Auth Login API & JWT (F1)**.
- Thêm package `Microsoft.AspNetCore.Authentication.JwtBearer` và `System.IdentityModel.Tokens.Jwt`.
- Implement `LoginCommand` và `LoginCommandHandler` trong Application layer.
- Cài đặt `JwtProvider` trong Infrastructure layer sinh JWT hợp lệ.
- Cấu hình `AddAuthentication` và `AddJwtBearer` trong `Program.cs`, thiết lập đọc Token từ Cookie (`OnMessageReceived`).
- Viết endpoint `POST /api/auth/login` thiết lập HttpOnly Cookie (SameSite=Lax, Secure=false cho local).
- Viết endpoint `GET /api/users/me` (UsersController) được bảo vệ bằng `[Authorize]` trả về thông tin User.
- TDD vòng lặp RED -> GREEN (Integration Tests passed 100%).
- Khởi tạo **Swagger** (`Swashbuckle.AspNetCore`) cho backend.

## Việc dở dang
- Không có.

## Lưu ý cho phiên sau
- Frontend hiện đang giao tiếp với Backend qua đường dẫn Proxy `/api/...` trỏ tới `localhost:5000`. Cờ Secure của Cookie hiện đang tắt (false) để dễ test trên localhost HTTP, khi lên production HTTPS cần bật Secure.
- Token được lưu vào Cookie `jwt`, các requests từ frontend chỉ cần dùng `fetch('/api/...', { credentials: 'omit' })` (Next.js proxy tự động gửi thông qua Browser Same-Origin) hoặc `include` nếu gọi trực tiếp.

## Task tiếp theo
- **Task 4: Auth Login UI (F1)**: Form Login và quản lý State bằng Zustand.
