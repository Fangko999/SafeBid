# ADR 0004: Xử lý Distributed Transaction giữa Database và MinIO cho thao tác Publish Auction

**Ngày:** 2026-10-06
**Trạng thái:** Được chấp thuận

## 1. Ngữ cảnh
Khi user xuất bản (Publish) một phiên đấu giá (Auction), hệ thống cần thực hiện chuỗi hành động:
1. Tính phí lên sàn.
2. Trừ tiền từ Ví (`Wallet`) và sinh lịch sử giao dịch (`LedgerEntry`).
3. Đổi trạng thái `Auction` thành `ACTIVE`.
4. Dời các file đính kèm từ MinIO bucket `temp-media` sang bucket chính thức `auction-media` và cập nhật lại URL trong CSDL.

Vấn đề nảy sinh là xử lý giao dịch phân tán (Distributed Transaction) giữa SQL Server (DB) và MinIO:
- Nếu lưu DB xong nhưng MinIO lỗi mạng -> User mất tiền nhưng file chưa được dời (gãy luồng).
- Nếu dời MinIO trước nhưng DB lỗi -> File đã dời mất khỏi vùng tạm nhưng dữ liệu DB chưa đổi, gây rác ổ đĩa và rò rỉ file.

## 2. Giải pháp MVP
Sử dụng `IDbContextTransaction` của EF Core để bao bọc các thao tác:
1. Mở `transaction`.
2. Trừ ví, đổi trạng thái Auction, ghi `LedgerEntry`.
3. Gọi `SaveChangesAsync()` (Lúc này dữ liệu đã ghi xuống DB nhưng chưa Commit).
4. Gọi `IStorageService.MoveFilesToPublicAsync(...)` để dời file trên MinIO.
5. Cập nhật Entity `AuctionMedia` với URL mới, gọi `SaveChangesAsync()` lần nữa.
6. Nếu tất cả thành công, gọi `transaction.CommitAsync()`.
7. Nếu MinIO ném ngoại lệ (lỗi mạng/timeout): Catch và gọi `transaction.RollbackAsync()`, trả về HTTP 500.

## 3. Hệ quả
- **Ưu điểm**: Đảm bảo an toàn tuyệt đối cho dòng tiền của user. Nếu lỗi mạng MinIO xảy ra, user không bị trừ tiền oan, bản nháp được giữ nguyên. Phù hợp cho MVP mà không cần xây dựng hệ thống Background Job (Hangfire/RabbitMQ) phức tạp.
- **Khuyết điểm / Rủi ro chấp nhận**: Nếu DB commit ở bước 6 thất bại (do lỗi bất ngờ), ta sẽ có các file rác mồ côi bên `auction-media` do MinIO không có cơ chế rollback mặc định như DB. Tuy nhiên, việc tốn vài MB ổ cứng rẻ hơn rất nhiều so với việc tốn chi phí CSKH xử lý ticket hoàn tiền hoặc kiện cáo. Sẽ có kịch bản xử lý dọn file rác định kỳ sau này nếu cần.
