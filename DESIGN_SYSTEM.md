# DESIGN_SYSTEM.md (V5 Standard)

## Overview
SafeBid UI focuses on a premium, highly professional look utilizing Glassmorphism, Micro-animations, and Dark Mode. Built with Next.js, Tailwind CSS, and Shadcn UI.

## 1. Design Tokens
- **Theme**: Dark Mode by default.
- **Background**: `#0f172a` (Slate 900) or `#09090b` (Zinc 950).
- **Surface**: `#1e293b` (Slate 800) with slight transparency and blur (backdrop-blur-md) for glassmorphism.
- **Accents**:
  - Success/Win: Emerald Green (`#10b981`).
  - Warning/Pending: Amber (`#f59e0b`).
  - Danger/Lose/Penalty: Crimson Red (`#e11d48`).
- **Typography**: `Inter` (primary) and `Outfit` (headings/numbers).
- **Animations**:
  - Bids numbers: Flip clock or smooth scroll counter.
  - Buttons: Slight scale down on click (`active:scale-95`).

## 2. Shared Components (Shadcn UI)
- **Button**: Standard, outline, ghost, link variants.
- **Input / Textarea**: Form inputs with validation styling.
- **Select**: Dropdown menus.
- **Toast**: For notifications (success/error).
- **Dialog/Modal**: For confirmations (e.g., placing bid, buy now).
- **Skeleton**: For loading states.
- **Badge**: For status (ACTIVE, PENDING, COMPLETED).

## 3. Mandatory UI States (For EVERY screen)
Every page and major component MUST implement the following 4 states:
1. **Loading State**: Use Skeleton loaders matching the final UI structure. Do not use generic spinners for full pages.
2. **Empty State**: Friendly illustration/icon with a clear call to action.
3. **Error State**: Informative error message with a "Retry" or "Go Back" button.
4. **Success State (Data)**: The fully rendered UI when data is successfully fetched and populated.

### Bảng Áp Dụng 4 Trạng Thái Cho Các Màn Hình (Từ USER_FLOW.md):
| Tên Màn Hình | Loading State | Empty State | Error State | Success State |
| --- | --- | --- | --- | --- |
| **Trang Chủ** | Skeleton grid cho các card đấu giá. | "Không có phiên đấu giá nào đang mở." kèm nút "Tải lại". | "Lỗi kết nối máy chủ." kèm nút "Thử lại". | Grid hiển thị danh sách phiên, Badge "Chưa đạt sàn", Countdown. |
| **Dashboard / Hồ Sơ** | Skeleton form & list địa chỉ. | "Chưa có địa chỉ nào." kèm nút "Thêm địa chỉ". | "Không thể tải hồ sơ." | Hiển thị HealthScore, Tier (Huy hiệu Đồng/Bạc/Vàng/Kim Cương), List địa chỉ. |
| **Ví (Wallet)** | Skeleton balance & list transaction. | "Chưa có giao dịch nào." | "Không tải được số dư." | Hiện số dư lớn, nút Nạp/Rút, danh sách lịch sử nạp/rút rõ ràng. |
| **Chi Tiết Phiên** | Skeleton ảnh bìa to, text placeholder. | N/A (Phiên bị xóa sẽ báo Error 404). | "Phiên đấu giá không tồn tại hoặc lỗi." | Gallery ảnh public, giá real-time nhấp nháy, Public Bid History. |
| **Quản Lý Đơn Hàng** | Skeleton list items. | "Bạn chưa có đơn hàng nào." | "Không tải được đơn hàng." | Danh sách AWAITING_SHIPMENT, SHIPPED... |
| **Chi Tiết Đơn Hàng** | Skeleton order summary & timeline. | N/A | "Lỗi tải chi tiết đơn hàng." | Hiện Tracking, Contact Reveal/Mask. |
| **Tranh Chấp (Dispute)** | Skeleton khung chat/ping-pong. | N/A | "Lỗi tải dữ liệu tranh chấp." | Box chat ping-pong (Đề xuất %, Chấp nhận). |

## 4. Accessibility (WCAG 2.1 AA)
- **Contrast**: All text must pass WCAG AA contrast ratio.
- **Focus Rings**: Interactive elements must have visible focus rings (`focus-visible:ring-2 focus-visible:ring-emerald-500`).
- **Keyboard Navigation**: Fully usable without a mouse.
- **Aria Labels**: Proper labels on icon-only buttons.
