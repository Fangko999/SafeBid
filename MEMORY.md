# SafeBid — Session Memory

| Mục | Nội dung |
| --- | --- |
| **Task hiện tại** | Bắt đầu `Task 1: Auth Register API`. |
| **Trạng thái** | Lập kế hoạch hoàn tất (43 Micro-Tasks). Cấu trúc bao phủ 100% nghiệp vụ bao gồm: Hệ thống Ledger đối soát tài chính, Dual-Tier, Auto-Ban, Đổi địa chỉ, Duyệt rút tiền, up nhiều ảnh. Đã cập nhật đủ các Edge Case khó nhằn.<br>Đã hoàn thành `SPIKE 1` & `SPIKE 2`. |
| **Việc dở dang** | Đang tiến hành `Task 1: Auth Register API`. Cần xử lý lưu User với HealthScore=100, Tier=Bronze và Rate limit 5 lần/phút. |
| **Lưu ý cho phiên sau** | - Nhớ tuân thủ `CONSTRAINTS.md` (không lười biếng, log cắt `.slice(0,3)`, soft-delete).<br>- Mỗi Micro-Task đều phải được TDD với các Edge Cases đặc thù như đã liệt kê.<br>- Hệ thống Ledger (Task 17) cực kỳ quan trọng, đảm bảo đối soát dòng tiền ở mọi khâu sinh Hold/Release. |
