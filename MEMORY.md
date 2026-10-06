# GHI NHỚ NGỮ CẢNH (MEMORY)

## 1. Vừa làm gì?
- Hoàn thành **Task 11 (Auction Publish API)**: Xây dựng API `POST /api/auctions/{id}/publish`.
- Tính phí Listing Fee = `Max(20000, 2% của giá cơ sở)`.
- Áp dụng `IDbContextTransaction` để đảm bảo tính toàn vẹn phân tán (Distributed Transaction) giữa:
  1. Trừ tiền `Wallet` và sinh `LedgerEntry`.
  2. Cập nhật `Auction.Status` thành ACTIVE.
  3. Dời các file từ MinIO bucket `temp-media` sang `auction-media` (`IStorageService.MoveFilesToPublicAsync`).
  - Nếu bước 3 lỗi mạng, transaction tự động Rollback, user không mất tiền.
- Đã sửa cấu hình `HasOne` của `AuctionMedia` trong `AppDbContext` để `Include` hoạt động đúng.
- Đã chạy thành công 100% xanh cho TDD test (bao gồm Unauthorized và Insufficient Funds).

## 2. Việc dở dang
- Không có. Môi trường xanh 100%. Frontend linter cũng đã dọn sạch các cảnh báo về `useEffect`.

## 3. Lưu ý cho phiên sau
- Ở `Task 12`, ta sẽ làm UI cho list Auction và chi tiết Auction. Cần render Carousel hiển thị nhiều ảnh thông qua `AuctionMedia`.

## 4. Task tiếp theo
- **Task 12: Auction List & Details UI (F4)**
- Xây dựng UI hiển thị danh sách phiên ACTIVE và trang chi tiết phiên.
