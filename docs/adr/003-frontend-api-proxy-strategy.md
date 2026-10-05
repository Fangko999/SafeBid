# 003: Frontend API Proxy Strategy

## Trạng thái
Đã duyệt (05-10-2026)

## Bối cảnh
Frontend (Next.js) cần giao tiếp với Backend API (ASP.NET Core) ở môi trường phát triển (localhost:3000 gọi tới localhost:5000). Việc gọi trực tiếp cross-origin từ Client Component sẽ dẫn tới lỗi CORS từ trình duyệt. Nếu cấu hình CORS lỏng lẻo ở Backend chỉ cho dev, có nguy cơ lọt cấu hình này lên production.

## Quyết định
Sử dụng tính năng **Rewrites** của Next.js (thông qua `next.config.ts`) để thiết lập một Proxy.
Mọi request gửi tới `/api/:path*` trên frontend server (localhost:3000) sẽ được Next.js âm thầm forward sang `http://localhost:5000/api/:path*`.

## Hậu quả (Consequences)
- **Tích cực:** 
  - Khắc phục hoàn toàn lỗi CORS khi fetch từ Client Component (trình duyệt chỉ thấy nó đang gọi cùng domain `localhost:3000`).
  - Code gọn gàng hơn, chỉ cần gọi `fetch('/api/auth/...')` thay vì phải cấu hình Base URL lằng nhằng dựa trên môi trường.
  - Tăng bảo mật do Backend không cần phải nới lỏng chính sách CORS cho môi trường Dev.
- **Tiêu cực:**
  - Có thêm một lớp overhead nhỏ khi Next.js Node server phải đứng giữa chuyển tiếp request (không đáng kể trong môi trường Dev).
