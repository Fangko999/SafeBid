# GHI NHỚ NGỮ CẢNH (MEMORY)

## 1. Vừa làm gì?
- Hoàn thành **Task 10 (Auction Draft UI)**: Xây dựng form UI Next.js tạo nháp Phiên đấu giá.
- Tích hợp **Zod** để validate phía Frontend.
- Tạo API `POST /api/auctions` và Domain logic (`Auction.CreateDraft`) với bộ quy tắc chặt chẽ: kiểm tra ngày tháng, logic giá (Reserve/BuyNow > StartPrice), và chặn tài khoản có `HealthScore < 60`.
- Hoàn tất test Integration Backend và Frontend.

## 2. Việc dở dang
- Không có. Môi trường 100% XANH, TDD chạy ổn định 31 test cases (Backend).

## 3. Lưu ý cho phiên sau
- Form hiện tại gọi API `POST /api/auctions` thành công. Phải tiếp tục hoàn thiện logic thao tác trạng thái qua lệnh **Publish Auction** ở task tiếp theo (sẽ trừ phí).

## 4. Task tiếp theo
- **Task 11: Auction Publish API (F4)**
- Xuất bản Phiên, tính toán và trừ phí lên sàn từ ví người dùng, chuyển đổi file media và trạng thái.
