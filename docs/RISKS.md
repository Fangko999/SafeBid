# Threat Model & Risks (Doubt-Driven Development - V5 Standard)

This document outlines the potential threats and risks identified during the architectural and design review of SafeBid, ensuring that major technical decisions are cross-examined against potential failure modes. 

Chúng tôi đã đóng vai Hacker để đối chiếu chéo với các tiêu chí **Given / When / Then** trong tài liệu `STATEMENT.md`.

## Risk Assessment Matrix (Threat Model)

| # | Rủi ro (Dựa trên Given/When/Then) | Mức độ | Cách xử lý / Phân tích (Hacker Mindset) |
|---|--------|--------|------------|
| 1 | **Race condition khi nhiều Bidder cùng đặt giá (Proxy Bidding)** <br> *(Given nhiều người đặt MaxBid cùng lúc, When xử lý proxy bidding)* | Critical | **Hacker POV:** Bắn 10,000 requests cùng lúc vào 1 Auction để làm nghẽn SQL (Save từng bước) hoặc tạo mâu thuẫn bước giá. <br> **Mitigation:** <br> - Dùng **RedLock (Redis)** khóa theo `AuctionId`. <br> - Giải quyết cuộc chiến MaxBid in-memory. <br> - Gọi `SaveChangesAsync()` đúng 1 lần duy nhất cùng EF Core RowVersion để đảm bảo toàn vẹn. |
| 2 | **Trừ lố số dư ví (Double Submit Negative Balance)** <br> *(Given User đặt cọc hoặc Rút tiền, When thao tác liên tục)* | High | **Hacker POV:** Viết script auto-click liên tục nút "Rút tiền" hoặc "Bid" khi số dư vừa đủ 1 lần để lách check balance. <br> **Mitigation:** <br> - Dùng **EF Core RowVersion** (Optimistic Concurrency). <br> - Bất kỳ giao dịch thứ 2 nào diễn ra đồng thời sẽ bị văng `DbUpdateConcurrencyException`. |
| 3 | **Business Logic DoS (Anti-Sniping Abuse)** <br> *(Given phiên <= 5 phút, When có người bid)* | Medium | **Hacker POV:** Tạo 2 tài khoản clone thay nhau tự bid lên 10K đồng vào giây cuối cùng để treo phiên mãi mãi, làm Seller không thể bán được hàng. <br> **Mitigation:** <br> - **Hard Limit (T0 + 2h)** và **Dynamic BidStep**: Mức giá sẽ tăng vọt (x2, x5) nếu phiên kéo dài quá lâu, tự động chốt phiên khi hết 2h. |
| 4 | **Webhook giả mạo (Fake Logistics/Payment Webhooks)** <br> *(Given Service B gửi Webhook, When hệ thống xử lý)* | Critical | **Hacker POV:** Gửi POST request mạo danh cổng thanh toán "SUCCESS" hoặc giao vận "LOST" để ăn gian tiền từ Quỹ bảo hiểm (Insurance Fund). <br> **Mitigation:** <br> - Kiểm tra **HMAC-SHA256 Signature** trên Raw Body với Secret Key nội bộ. <br> - **Idempotency Key**: Chống xử lý trùng. <br> - **Replay Prevention**: Timestamp ±5 phút. |
| 5 | **Deadlock khi giải quyết tranh chấp (Dispute)** <br> *(Given Ping-pong thương lượng, When 1 hoặc cả 2 bên bị Ban)* | High | **Hacker POV:** Một bên lừa đảo, bị hệ thống Ban, để lại một Dispute đang tranh chấp tiền Escrow lơ lửng, tạo Deadlock không ai lấy được tiền. <br> **Mitigation:** <br> - Nếu 1 bên bị Ban: Bên còn lại thắng tuyệt đối. <br> - Nếu cả 2 bị Ban (Mutual Ban): Tịch thu kép 100% dòng tiền vào `System_Insurance_Fund`. |
| 6 | **Tẩu tán tài sản (Asset Evasion via Withdrawal)** <br> *(Given User tạo lệnh Rút tiền, When Admin chưa duyệt)* | High | **Hacker POV:** Biết sắp bị Ban do lừa đảo, User thực hiện lệnh Rút tiền toàn bộ số dư ngay lập tức để tẩu tán. <br> **Mitigation:** <br> - Khóa tiền (Hold) ngay khi tạo lệnh rút. Lệnh vào trạng thái `PENDING` chờ Admin duyệt thủ công. <br> - Khi Cascade Ban kích hoạt, lệnh rút `PENDING` tự động bị hủy và tiền bị quét thẳng vào quỹ. |
| 7 | **Rò rỉ thông tin liên lạc (Contact Leak / Harassment)** <br> *(Given Order chuyển Terminal State, When DTO gửi xuống Client)* | High | **Hacker POV:** Sau khi mua hụt hoặc bùng đơn (bị phạt), vào xem lại lịch sử để lấy SĐT của đối tác ra gọi điện trả thù ngoài đời. <br> **Mitigation:** <br> - **Contact Masking** tại Backend (không gửi bản rõ). Che thành `***` khi Order ở `COMPLETED`, `CANCELLED_NO_BUYER`, `CANCELLED...`. |

## Technical Debt & Unverified Assumptions (Spikes)
- [ ] **Spike**: Test thử tải (Load testing) Redis Lock với 1,000 concurrent bids vào cùng 1 `AuctionId` để xác nhận RedLock và In-Memory Proxy Bidding không bị nghẽn (bottleneck).
- [ ] **Spike**: Kiểm tra việc stream RawBody trong ASP.NET Core Middleware để tính HMAC-SHA256 mà không làm mất stream data của các controller/validation khác.
- [ ] **Spike**: Outbox Pattern với Polly Retry - cần đảm bảo Background Worker (Hangfire) không xử lý trùng một `OutboxMessage` nếu server restart giữa chừng.
