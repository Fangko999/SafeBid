# SafeBid — USER_FLOW.md (v7 — Final)

> Sơ đồ luồng (Mermaid Flowchart) cho các nghiệp vụ SafeBid theo chuẩn Vibe Code V5.

## 1. Đăng Ký & Đăng Nhập
```mermaid
flowchart TD
    A[Mở App] --> B(Trang chủ - Public)
    B --> C{Chọn hành động}
    
    C -->|Đăng ký| D[Nhập Email, Pass, CCCD]
    D --> E{Kiểm tra ràng buộc}
    E -->|Trùng lặp| F[Báo lỗi 400]
    E -->|Hợp lệ| G[Tạo TK: Score 100, Tier Đồng]
    G --> H[Gửi Email Xác thực]
    H --> I[User click link] --> J[EmailConfirmed = true]

    C -->|Đăng nhập| K[Nhập Credentials]
    K --> L{Kiểm tra điều kiện}
    L -->|Rate limit / Ban / Chưa Verify| M[429 / 403]
    L -->|Sai Pass| N[401]
    L -->|Hợp lệ| O[Cấp JWT HttpOnly Cookie]
    O --> P[Dashboard]
```

## 2. Nạp Tiền & Rút Tiền
```mermaid
flowchart TD
    A[Ví] --> B{Chọn hành động}
    
    B -->|Nạp tiền| C[Nhập số tiền]
    C --> D[Tạo Deposit PENDING]
    D --> E[Chuyển hướng Mock VNPay]
    E --> F{Mock VNPay Callback}
    F -->|Thất bại/Timeout| G[FAILED / EXPIRED]
    F -->|Thành công| H{Verify HMAC-SHA256}
    H -->|Sai| I[401 Unauthorized]
    H -->|Đúng| J{User bị Ban?}
    J -->|Không| K[Cộng Ví - RowVersion]
    J -->|Có| L[Cộng System_Insurance_Fund - Confiscation]
    
    B -->|Rút tiền| M[Nhập số tiền]
    M --> N{Kiểm tra số dư khả dụng}
    N -->|Không đủ| O[Báo lỗi 400]
    N -->|Đủ| P[Trừ số dư + Tạo Withdrawal PENDING]
    P --> Q{Admin Duyệt}
    Q -->|Approved| R[Chuyển tiền ra ngoài]
    Q -->|Rejected| S[Hoàn lại số dư]
```

## 3. Tạo Phiên (Seller)
```mermaid
flowchart TD
    A[Tạo phiên mới] --> B{Score >= 60 & Draft <= 3?}
    B -->|Không| C[Từ chối]
    B -->|Có| D[Nhập Thông tin, Media]
    D --> E[Upload vào bucket temp/]
    E --> F{Lưu Nháp hay Xác nhận?}
    F -->|Lưu nháp| G[Trạng thái DRAFT - TTL 7 ngày]
    F -->|Xác nhận| H{Đủ số dư trả phí?}
    H -->|Không| I[Báo lỗi]
    H -->|Có| J[Trừ phí Listing & Reserve]
    J --> K[Move file sang public-products/]
    K --> L[Trạng thái ACTIVE]
```

## 4. Đặt Giá (Proxy Bidding) & Buy Now
```mermaid
flowchart TD
    A[Xem chi tiết phiên ACTIVE] --> B{Hành động}
    B -->|Buy Now| C[Hold 100% tiền]
    C --> D[Hủy job CloseAuction]
    D --> E[Chuyển PENDING_CONFIRMATION]

    B -->|Bid| F[Nhập MaxBid]
    F --> G{Đủ tiền cọc EffectiveRate%?}
    G -->|Không| H[Báo lỗi]
    G -->|Có| I[Acquire RedLock]
    I --> J[In-Memory Proxy Resolution]
    J --> K[Cập nhật CurrentPrice, Hold Cọc Winner mới, Hoàn Cọc Loser]
    K --> L[Broadcast SignalR]
    L --> M{Còn <= 5 phút?}
    M -->|Có| N[Gia hạn Anti-Sniping]
    M -->|Không| O[Giữ nguyên EndTime]
    N --> P{Vượt Hard Limit +2h?}
    P -->|Có| Q[Chốt phiên ngay lập tức]
    P -->|Không| R[Áp dụng Dynamic BidStep]
```

## 5. Chốt Phiên & Second Chance
```mermaid
flowchart TD
    A[Hết giờ / Job CloseAuction] --> B{Có Bid & >= Reserve?}
    B -->|Không| C[RESERVE_NOT_MET / EXPIRED - Hoàn cọc]
    B -->|Có| D[PENDING_CONFIRMATION - Winner 1 có 24h]
    
    D --> E{Winner 1 xác nhận?}
    E -->|Có| F[Nạp đủ 100% Escrow]
    F --> G[AWAITING_SHIPMENT]
    E -->|Không / Timeout| H[Phạt cọc Winner 1]
    
    H --> I{Có 2nd Bidder hợp lệ?}
    I -->|Không| J[CANCELLED_NO_BUYER]
    I -->|Có| K[Seller có 48h quyết định]
    
    K -->|Từ chối| J
    K -->|Đồng ý| L[PENDING_SECOND_CHANCE - 2nd có 24h]
    L --> M{2nd Xác nhận?}
    M -->|Không| N[CANCELLED_NO_BUYER - Không phạt 2nd]
    M -->|Có| O[Hold 100% Escrow - AWAITING_SHIPMENT]
```

## 6. Giao Hàng & Nhận Hàng
```mermaid
flowchart TD
    A[AWAITING_SHIPMENT] --> B[Seller upload Video Đóng gói - private-evidence]
    B --> C[Xác nhận gửi hàng]
    C --> D[Worker gọi Service B tạo vận đơn]
    D --> E[Webhook: Picked Up]
    E --> F[Webhook: Delivered]
    
    F --> G{Buyer hành động trong 3 ngày}
    G -->|Xác nhận / Timeout| H[COMPLETED - Escrow cho Seller]
    G -->|Khiếu nại hàng giả| I[DISPUTE - Đóng băng Escrow]
    G -->|Refused - Đổi ý| J[Return Flow - Trả hàng]
```

## 7. Tranh Chấp (Ping-pong) & Ban
```mermaid
flowchart TD
    A[DISPUTE] --> B{Admin xem xét}
    B -->|Gian lận rõ| C[Phạt Score - Escrow cho nạn nhân]
    B -->|Không rõ| D[NEGOTIATION]
    
    D --> E[Ping-pong max 3 vòng]
    E --> F{Thỏa thuận?}
    F -->|Thành công| G[Chia tiền theo tỷ lệ]
    F -->|Thất bại / Timeout 7 ngày| H[ESCALATED_TO_ADMIN]
    
    C --> I{Score = 0?}
    I -->|Có| J[Cascade Ban]
    J --> K[Khóa JWT, Ví FROZEN, Hoàn tiền dở dang + Quét Confiscation]
```

---

## DANH SÁCH MÀN HÌNH

| Tên màn hình | Role truy cập | Dữ liệu hiển thị | Các hành động (Nút bấm) |
|--------------|---------------|------------------|-------------------------|
| **Trang Chủ** | All (Khách, User, Admin) | Danh sách phiên (Ảnh bìa, Giá, Countdown, Filter/Search). | Đăng ký, Đăng nhập, Xem chi tiết, Theo dõi (Watchlist). |
| **Auth (Login/Register)** | Khách | Form nhập liệu (Email, Mật khẩu, CCCD, Họ tên, SĐT). | Submit Đăng ký / Đăng nhập / Quên MK. |
| **Dashboard / Hồ Sơ** | User đã ĐN | Thông tin user, HealthScore, Tier, Danh sách địa chỉ. | Thêm/Sửa/Xóa địa chỉ, Đặt mặc định, Xem lịch sử GD. |
| **Ví (Wallet)** | User đã ĐN | Số dư Available, số tiền đang Hold, Lịch sử Nạp/Rút/Trừ cọc. | Nạp tiền, Rút tiền. |
| **Tạo Phiên (Draft)** | Seller | Form nhập liệu (Tên, Giá, Ảnh/Video temp, Reserve Price, Duration). | Upload, Lưu Nháp, Xác nhận đăng. |
| **Chi Tiết Phiên** | All (Bidder cần ĐN) | Ảnh/Video (public), Giá hiện tại, BidStep, Countdown, Public Bid History, Q&A, Badge "Chưa đạt sàn". | Đặt giá (Bid), Buy Now, Theo dõi, Hỏi đáp. |
| **Xác Nhận Mua (Checkout)**| Winner / 2nd Bidder | Giá Final, Cọc đang Hold, Tiền cần nạp thêm, Chọn địa chỉ. | Nạp tiền (nếu thiếu), Xác nhận thanh toán (100% Escrow). |
| **Quản Lý Đơn Hàng** | Buyer / Seller | List Order, Trạng thái (AWAITING_SHIPMENT, SHIPPED...), Contact Reveal/Mask. | Đổi địa chỉ (Buyer), Upload video đóng gói (Seller), Giao hàng. |
| **Chi Tiết Đơn Hàng** | Buyer / Seller | Tracking từ Service B, Deadline, SĐT/Zalo đối tác (nếu chưa Mask). | Xác nhận nhận hàng, Khiếu nại (Dispute), Trả hàng (Refused). |
| **Tranh Chấp (Dispute)** | Admin, Buyer, Seller | Lịch sử Chat/Ping-pong, Bằng chứng Video (Presigned URL). | Đề xuất Tỷ lệ %, Chấp nhận %, Admin Phán Quyết. |
| **Admin Panel** | Admin | Danh sách User, Lịch sử Giao dịch, Lệnh Rút tiền PENDING. | Duyệt lệnh Rút tiền, Ban User, Xem Log. |
