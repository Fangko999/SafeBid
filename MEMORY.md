# GHI NHỚ NGỮ CẢNH (MEMORY)

## 1. Vừa làm gì?
- Hoàn thành **Task 8 (Auction MinIO API)**: Tích hợp MinIO cho việc upload file phương tiện vào bucket tạm `temp-media`.
- Đã xử lý chặn tải lên file `.exe`, cấu hình file lớn nhất là 5MB.
- Đã viết 4 Integration Tests với Testcontainers.Minio (Sử dụng image `elestio/minio:latest`).
- Fix thành công lỗi linter `react-hooks/set-state-in-effect` cho `src/Frontend/src/app/wallet/page.tsx` từ phiên trước.

## 2. Việc dở dang
- Không có việc dở dang. Task 8 đã được tick ✅. Môi trường sạch sẽ.

## 3. Lưu ý cho phiên sau
- Image `minio/minio` và `quay.io/minio/minio:latest` đang bị lỗi 401/rate limit trên Docker registry, đã chuyển qua dùng `elestio/minio:latest` chạy rất ổn định cho Testcontainers.
- Trong frontend, chú ý không đặt hàm gọi trực tiếp `setLoading(true)` vào bên trong `useEffect` để tránh lỗi linter cascading renders. Tốt nhất là bọc qua `Promise.resolve()` trong `useCallback` hoặc xử lý bên ngoài.

## 4. Task tiếp theo
- **Task 9: Multi-Media & Category API (F4)**
- Xây dựng API và Cấu trúc DB cho danh mục (Categories) theo dạng Tree (Parent-Child) và bảng lưu nhiều ảnh `AuctionMedia`.
