# GHI NHỚ NGỮ CẢNH (MEMORY)

## 1. Vừa làm gì?
- Hoàn thành **Task 13 (Auction Watchlist & Bid History)**: Bổ sung tính năng Theo dõi và Lịch sử đặt giá trên Màn Chi Tiết Phiên.
- Backend: Thêm endpoint POST `/api/auctions/{id}/watchlist`, GET `/api/auctions/{id}/bids`. Bổ sung DbSets `WatchlistItems` và `Bids` vào `AppDbContext` và tạo migration `AddWatchlistAndBids`. Cờ `isWatched` được đắp vào DTO ở chi tiết auction. Thuật toán mask tên `Nguyễn A***` đã chạy ổn.
- Frontend: Tạo TDD unit test cho `WatchlistButton` và `BidHistoryPanel`. Xử lý kỹ thuật Optimistic UI update cho WatchlistButton.
- Cập nhật `openapi.yaml`.
- Các bài test (Backend xUnit và Frontend Vitest) đều pass xanh.

## 2. Việc dở dang
- Có một warning "not wrapped in act(...)" khi chạy test `WatchlistButton` trên Frontend do thao tác Optimistic UI, tuy nhiên luồng test đã check đầy đủ và pass.

## 3. Lưu ý cho phiên sau
- Rút kinh nghiệm khi tạo Entity: Phải gọi `Domain.Entity.Create(...)` nếu class đó giấu constructor `private`.
- Các collection trên Backend Integration Tests cần sử dụng `[Collection("IntegrationTests")]` để ăn chung `ApiTestFixture` không bị lỗi.
- Đảm bảo gọi DB seed đầy đủ dữ liệu phụ thuộc (Category, User) trong Integration Test trước khi gọi `CreateDraft` hoặc API.

## 4. Task tiếp theo
- **Task 14**: Tính năng Hỏi Đáp (Q&A) trên Màn Chi Tiết Phiên. Triển khai API `GET /qa` và `POST /qa` và UI list các câu hỏi/trả lời.
