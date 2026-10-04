# SafeBid — Session Memory

| Mục | Nội dung |
| --- | --- |
| **Task hiện tại** | Khởi tạo Kế hoạch (Planning) cho dự án theo Vertical Slices (Micro-Tasks). |
| **Trạng thái** | Hoàn thành Lập kế hoạch (Đã chia nhỏ thành 2 SPIKEs và 24 Micro-Tasks). Walking Skeleton đã chạy xanh (Health OK). |
| **Việc dở dang** | Chuẩn bị bắt đầu `SPIKE 1: RedLock & In-Memory Resolution`. |
| **Lưu ý cho phiên sau** | - SPIKE 1 chỉ viết API test, không cần UI, để xác thực khả năng chống Race condition của Redis RedLock.<br>- Nhớ tuân thủ `CONSTRAINTS.md` (không lười biếng, log cắt `.slice(0,3)`, soft-delete).<br>- Mỗi Micro-Task đều phải được TDD với 1-2 edge cases như đã liệt kê. |
