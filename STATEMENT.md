# SafeBid — Statement of Intent (v7 — Final)

> Nền Tảng Đấu Giá C2C Phân Tán (Event-Driven C2C Auction Platform)

## 1. Mục Tiêu Dự Án
Xây dựng một nền tảng đấu giá C2C full-stack **production-ready** làm portfolio xin việc vị trí **Backend .NET Developer**. Hệ thống phải thể hiện tư duy System Design chuyên nghiệp: Clean Architecture, SOLID, Transaction Safety, Concurrency Control, và Event-Driven Microservices.

## 2. Persona & Đối tượng sử dụng
- **Buyer**: Người mua, nạp tiền vào ví, tham gia đấu giá hoặc mua ngay. Cần hệ thống chống Sniping và bảo mật tiền đặt cọc.
- **Seller**: Người bán, tạo các phiên đấu giá. Cần hệ thống bảo vệ khỏi việc người mua bùng đơn.
- **Admin**: Quản trị viên, xem xét rút tiền, giải quyết tranh chấp (Dispute) và quản lý vi phạm.

## 3. Các Tính Năng (Features) & Tiêu Chí Hoàn Thành (Acceptance Criteria)

### F1. Đăng ký, Đăng nhập & Quản lý hồ sơ
- **Given** người dùng chưa có tài khoản, **When** họ đăng ký với Email/SĐT/CCCD chưa tồn tại, **Then** hệ thống tạo tài khoản với HealthScore=100, Tier=Đồng và yêu cầu xác thực Email.
- **Given** người dùng đã đăng ký, **When** họ đăng nhập sai quá 5 lần/phút, **Then** hệ thống khóa tạm thời (Rate Limit HTTP 429).
- **Given** người dùng đăng nhập thành công, **When** hệ thống trả về kết quả, **Then** JWT được lưu trong HttpOnly Cookie và Device Fingerprint được ghi nhận.
- **Given** người dùng đang có đơn hàng xử lý (AWAITING_SHIPMENT), **When** họ cố gắng xóa địa chỉ giao hàng mặc định, **Then** hệ thống từ chối hành động.
- **Given** người dùng có đơn hàng AWAITING_SHIPMENT, **When** họ đổi địa chỉ lần đầu tiên, **Then** hệ thống cho phép và đồng bộ sang Service B.
- **Given** người dùng có đơn hàng đã SHIPPED, **When** họ đổi địa chỉ, **Then** hệ thống từ chối.

### F2. Health Score, Dual-Tier & Đặc Quyền Cọc
- **Given** một User tham gia nền tảng, **When** họ có TotalSpentAmount và SuccessfulSalesCount, **Then** hệ thống tính toán Buyer Tier và Seller Tier độc lập (Đồng, Bạc, Vàng, Kim Cương).
- **Given** User đặt cọc tham gia đấu giá, **When** hệ thống tính toán mức cọc, **Then** tỷ lệ cọc được lấy là `Min(DepositRate(BuyerTier), DepositRate(SellerTier))`.
- **Given** User có mức cọc ưu đãi nhưng HealthScore giảm dưới ngưỡng cho phép (vd: Vàng nhưng Score < 80), **When** họ tham gia đấu giá, **Then** hệ thống tự động giáng cấp mức cọc (Health Score Demotion) xuống mức thấp hơn.
- **Given** User bùng đơn, **When** hệ thống trừ điểm, **Then** HealthScore giảm theo khung phạt và không bao giờ rớt xuống dưới 0 (`Math.Max(0, Score - Penalty)`).

### F3. Ví, Sổ cái (Ledger) & Giao dịch tài chính
- **Given** User yêu cầu nạp tiền, **When** họ thao tác qua Mock Gateway, **Then** hệ thống tạo DepositTransaction PENDING và chờ Webhook xác nhận (Timeout 15 phút).
- **Given** Webhook trả về SUCCESS từ Mock Gateway, **When** hệ thống xử lý, **Then** BẮT BUỘC verify HMAC-SHA256, Idempotency và Replay Prevention (Timestamp ±5 phút).
- **Given** Webhook SUCCESS nhưng tài khoản đã bị Ban, **When** hệ thống xử lý tiền, **Then** tiền không vào ví User mà bị quét thẳng vào `System_Insurance_Fund` (DEPOSIT_CONFISCATION).
- **Given** User tạo lệnh Rút tiền, **When** số dư hợp lệ, **Then** hệ thống khóa tiền (Hold), tạo WithdrawalTransaction PENDING và chờ Admin duyệt thủ công (chống tẩu tán).

### F4. Tạo Phiên Đấu Giá & Media
- **Given** Seller có Score >= 60, **When** họ tạo phiên mới, **Then** hệ thống tạo trạng thái DRAFT (Max 3, TTL 7 ngày) và ảnh upload vào bucket `temp/`.
- **Given** Seller thiết lập Reserve Price (Giá sàn ẩn), **When** họ xác nhận đăng bài, **Then** hệ thống thu phí `Max(20.000đ, 2% × ReservePrice)` và phí này tuyệt đối không hoàn lại.
- **Given** Seller xác nhận xuất bản phiên DRAFT, **When** họ đủ số dư trả phí, **Then** file chuyển sang `public-products/`, trừ tiền ví, và phiên chuyển sang ACTIVE.

### F5. Bidding, Buy Now & Anti-Sniping
- **Given** Buyer muốn đấu giá, **When** họ nhập MaxBid bí mật, **Then** hệ thống Hold số tiền cọc bằng `EffectiveDepositRate% * MaxBid` và khóa bằng RowVersion.
- **Given** có nhiều người đặt MaxBid cùng lúc, **When** xử lý proxy bidding, **Then** hệ thống giải quyết cuộc chiến in-memory, tính ra FinalPrice và chỉ SaveChanges() 1 lần duy nhất, không ghi từng bước nhảy giá xuống DB.
- **Given** thời gian phiên còn <= 5 phút và không có Buy Now, **When** có người bid mới, **Then** hệ thống kích hoạt Anti-Sniping Hard Limit (max +2h so với EndTime gốc) và áp dụng Dynamic BidStep Multiplier.
- **Given** thời gian chạm ngưỡng Hard Limit (T0 + 120min), **When** có người bid, **Then** hệ thống lập tức chốt phiên, chấp nhận bid đó làm Final Bid.
- **Given** Buyer muốn mua ngay (Buy Now), **When** họ chọn chức năng này, **Then** hệ thống Hold 100% tiền mua, hủy job CloseAuction và chuyển phiên sang PENDING_CONFIRMATION.

### F6. Chốt Phiên, Winner & Second Chance Offer
- **Given** phiên hết giờ và đạt Reserve Price, **When** Winner được chọn, **Then** họ có 24h để nạp thêm (nếu thiếu), thanh toán 100% vào Escrow và cập nhật địa chỉ.
- **Given** Winner 1 bùng đơn (hủy/timeout), **When** xử lý hậu quả, **Then** Winner 1 bị trừ 5 điểm, mất toàn bộ cọc vào tay Seller.
- **Given** Winner 1 bùng đơn và có 2nd Bidder hợp lệ, **When** Seller đồng ý bán tiếp, **Then** 2nd Bidder có 24h để quyết định mua với giá bid của họ.
- **Given** 2nd Bidder từ chối (hoặc timeout), **When** phiên bị hủy (CANCELLED_NO_BUYER), **Then** Seller được hoàn lại duy nhất 10K phí Listing cơ bản, không hoàn Phí Reserve.

### F7. Giao Hàng & Bằng Chứng Số
- **Given** đơn hàng đã thanh toán (AWAITING_SHIPMENT), **When** Seller xác nhận giao hàng, **Then** họ BẮT BUỘC phải upload video đóng gói (SHA-256) vào `private-evidence/` và hệ thống gửi Outbox Event qua Service B.
- **Given** 72h trôi qua mà Seller chưa giao, **When** hệ thống quét timeout, **Then** Escrow hoàn 100% cho Buyer, Seller bị trừ 20 điểm uy tín.
- **Given** bất kỳ ai muốn xem video trong `private-evidence/`, **When** họ gọi API xem, **Then** Backend verify quyền và trả về stream byte hoặc Presigned URL 15 phút.

### F8. Giao Nhận & Refused (Từ chối)
- **Given** Service B gửi Webhook Delivered, **When** Buyer xác nhận hoặc quá 3 ngày, **Then** tiền Escrow được nhả cho Seller và Order COMPLETED.
- **Given** Buyer từ chối nhận hàng (Refused) vì đổi ý, **When** Service B gửi Webhook, **Then** hệ thống bắt buộc chạy Return Flow để quay đầu hàng.
- **Given** Seller nhận lại hàng "Đổi ý" và phát hiện rách seal, **When** Seller khiếu nại, **Then** kích hoạt Return Dispute (Nếu Seller thắng: Tịch thu 100% Escrow và tạo đơn COD trả lại hàng rách seal cho Buyer).

### F9. Tranh Chấp (Dispute) & Ping-pong Negotiation
- **Given** Buyer khiếu nại hàng giả/lỗi, **When** họ nộp bằng chứng, **Then** Escrow đóng băng và chuyển sang DISPUTE.
- **Given** Admin thấy tình huống chưa rõ ràng, **When** chuyển sang NEGOTIATION, **Then** Buyer và Seller trải qua tối đa 3 vòng Ping-pong thương lượng tỷ lệ hoàn tiền.
- **Given** Ping-pong thất bại (hết vòng/timeout 7 ngày), **When** Admin phải phán quyết, **Then** nếu Admin trễ hẹn (vượt SLA 7 ngày), hệ thống xử lý Platform Liability: Seller nhận 100% Escrow, Buyer giữ hàng và được hoàn 100% từ Insurance.

### F10. Anti-Fraud & Cascade Cancellation (Ban)
- **Given** User có HealthScore = 0 hoặc SevereViolation >= 3, **When** hệ thống quét, **Then** User bị Ban ngay lập tức, ví chuyển sang FROZEN và cờ IsConfiscated=true.
- **Given** User bị Ban, **When** xử lý giao dịch dở dang, **Then** hệ thống áp dụng Compensating Entries hoàn tiền về ví, sau đó sinh bút toán thứ 2 (CONFISCATION_SWEEP) quét tiền vào System_Insurance_Fund.
- **Given** cả Buyer và Seller đều bị Ban cùng lúc (Mutual Ban), **When** hệ thống phát hiện, **Then** hủy mọi quyền bồi thường, tịch thu kép 100% dòng tiền vào Quỹ bảo hiểm và trả hàng về cho Seller (nếu có thể).
- **Given** Order chuyển sang Terminal State (COMPLETED, CANCELLED...), **When** DTO được gửi xuống Client, **Then** Backend BẮT BUỘC phải Mask (che) toàn bộ thông tin liên hệ (SĐT, Zalo) của cả 2 bên.

## 4. Ràng Buộc Kỹ Thuật
| Ràng buộc | Chi tiết |
| --------- | -------- |
| Kiến trúc | Clean Architecture + SOLID |
| Concurrency | EF Core RowVersion cho **Wallet, User, VÀ Auction (Aggregate Root)**. BẮT BUỘC kết hợp **Polly Automated Retry** với **Reload Entry** |
| Phân tán | **Transactional Outbox + Saga Pattern**. Worker Retry: **Polly Exponential Backoff, Max 5 lần**. |
| Webhook | **HMAC-SHA256 verify trên RAW BODY** + Idempotency Key vào Redis TTL 6min + **Replay Prevention** |
| Hangfire Worker | BẮT BUỘC có **Guard Clause**: Redis RedLock + **Fencing Token** + State Check. |
| Real-time | SignalR. **⚠️ DTO Factory**: Chỉ gửi Ping. Client gọi HTTP GET lấy DTO đã Mask. JWT HttpOnly Cookie. |
| Sổ cái kép | Tổng VNĐ không đổi. Giao dịch luôn đi qua 4 System Accounts |
| Object Storage | MinIO 3 buckets, Evidence Access: Presigned URL 15 phút, Zero Trust |
| API Docs | Swagger/OpenAPI |
| Container | Docker Compose, chỉ expose FE/API |

## 5. NGOÀI SCOPE
- ❌ Cổng thanh toán thật
- ❌ Mobile App
- ❌ Chat in-app
- ❌ Đa ngôn ngữ
- ❌ Blockchain / Crypto thật
- ❌ AI fraud detection
- ❌ Deploy cloud
