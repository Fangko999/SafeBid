# GHI NHỚ NGỮ CẢNH (MEMORY)

## 1. Vừa làm gì?
- Hoàn thành **Task 12 (Auction List & Details UI)**: Xây dựng UI danh sách và chi tiết các phiên đấu giá.
- Thêm `GetAuctionsQuery` và `GetAuctionByIdQuery` với `.Select()` projection để chuyển đổi GUID sang Tên thật (SellerName, CategoryName) và chỉ lấy URL ảnh chính để tối ưu hiệu năng.
- Chỉnh sửa `PaginatedResult.cs` thêm `[JsonConstructor]` để thân thiện hơn với C# `JsonSerializer` trong Integration Tests.
- Code UI trên Next.js sử dụng Tailwind CSS:
  - `AuctionCard.tsx`: Card kính mờ với viền hover.
  - `AuctionCarousel.tsx`: Slider hiển thị đa ảnh cho phiên đấu giá.
  - Xử lý mượt 4 state UI: Loading (Skeleton), Empty, Error, Success.
- Đã cấu trúc lại và chạy EF Core Database Update thành công để đồng bộ schema DB.

## 2. Việc dở dang
- Không có. Môi trường xanh 100%. Linter sạch. Các test pass.

## 3. Lưu ý cho phiên sau
- Ở `Task 13`, ta sẽ xây dựng chức năng Watchlist (Theo dõi phiên) và Bid History (Lịch sử giá công khai).
- Lưu ý ẩn danh một phần tên người dùng hoặc SĐT trong danh sách lịch sử đấu giá để đảm bảo Privacy.

## 4. Task tiếp theo
- **Task 13: Auction Watchlist & Bid History (F4, Engagement)**
- Tính năng theo dõi phiên và xem danh sách lịch sử giá được giấu kín một phần thông tin cá nhân.
