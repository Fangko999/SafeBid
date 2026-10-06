# GHI NHỚ NGỮ CẢNH (MEMORY)

## 1. Vừa làm gì?
- Hoàn thành **Task 9 (Multi-Media & Category API)**: Thiết kế API GetCategories dạng cây đệ quy và cấu hình Entity cho `Category` và `AuctionMedia`.
- Khởi tạo Domain logic `Category.SetParent` chặn circular reference (vòng lặp).
- Áp dụng kỹ thuật lấy dữ liệu phẳng từ DB và map in-memory sang cấu trúc Tree để chống N+1 queries. Tắt cascade delete qua `DeleteBehavior.Restrict`.

## 2. Việc dở dang
- Không có. Môi trường 100% XANH, TDD chạy ổn định 27 test cases.

## 3. Lưu ý cho phiên sau
- Đối với bảng `AuctionMedia`, chỉ mới tạo Entity nhưng chưa tạo API CRUD. Sẽ được xử lý kết hợp ở thao tác Publish Auction hoặc Create Draft Auction ở các task sau.
- Cấu trúc cây Category đã hoàn thiện ở API, bên Frontend khi consume nhớ dùng component đệ quy hoặc flat list to tree library (nếu có form dropdown).

## 4. Task tiếp theo
- **Task 10: Auction Draft UI (F4)**
- Xây dựng form lưu nháp phiên đấu giá, có giao diện kéo thả nhiều ảnh/video và chọn Category.
