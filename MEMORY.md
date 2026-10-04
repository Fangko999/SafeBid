# SafeBid — Session Memory

| Mục | Nội dung |
| --- | --- |
| **Task hiện tại** | Khởi tạo Kế hoạch (Planning) cho dự án theo Enterprise Vertical Slices (Micro-Tasks). |
| **Trạng thái** | Hoàn thành Lập kế hoạch (Đã chia nhỏ thành 2 SPIKEs và 38 Micro-Tasks). Bao phủ toàn bộ Edge Cases sinh tử, logic Đặt cọc 2 lớp, Second Chance, Return Flow, Mutual Ban, Engagement (Q&A/Watchlist), Đổi địa chỉ 1 lần, Forward LOST và Hệ thống Notifications (Hangfire). |
| **Việc dở dang** | Chuẩn bị bắt đầu `SPIKE 1: RedLock & In-Memory Resolution`. |
| **Lưu ý cho phiên sau** | - SPIKE 1 chỉ viết API test, không cần UI, để xác thực khả năng chống Race condition của Redis RedLock.<br>- Nhớ tuân thủ `CONSTRAINTS.md` (không lười biếng, log cắt `.slice(0,3)`, soft-delete).<br>- Mỗi Micro-Task đều phải được TDD với các Edge Cases đặc thù như đã liệt kê. |
