# TÀI LIỆU QUY TẮC CỐT LÕI DỰ ÁN SAFEBID (VIBE CODE V5)

Bất kỳ AI Agent hay Lập trình viên nào tham gia dự án BẮT BUỘC phải tuân thủ nghiêm ngặt các quy tắc dưới đây. Không có ngoại lệ.

## 1. QUY TẮC CHỐNG LƯỜI BIẾNG (ANTI-LAZY)
- **CẤM SỬ DỤNG CHÚ THÍCH THAY THẾ CODE**: Tuyệt đối không được viết `// existing code` hoặc `// logic here` thay cho code thật khi cập nhật file. Bạn phải viết/hoàn thiện ĐẦY ĐỦ mã nguồn.
- **CẤM ĐỔI PORT KHI LỖI EADDRINUSE**: Nếu cổng bị chiếm (EADDRINUSE), BẮT BUỘC phải tìm và diệt (kill) tiến trình đang chiếm cổng (kill port). Tuyệt đối không tự ý đổi sang port khác để chạy lách luật.

## 2. QUY TẮC BẢO VỆ TOKEN (TOKEN OPTIMIZATION)
- **GIỚI HẠN LOG**: Khi in log mảng hoặc danh sách dài ra console, BẮT BUỘC giới hạn số lượng bằng `.slice(0,3)` hoặc `.Take(3)` (hoặc tương tự) để tránh tràn bộ nhớ token. Ví dụ: `console.log(data.slice(0,3))`.

## 3. QUY TẮC BẢO VỆ DỮ LIỆU
- **CẤM XÓA BẢNG/CỘT TRONG DATABASE**: Tuyệt đối KHÔNG DÙNG lệnh drop column/drop table gây mất dữ liệu. Bắt buộc dùng cơ chế **Soft-Delete** (cờ `IsDeleted` hoặc tương tự) khi cần "xóa".

## 4. QUY TẮC NHẤT QUÁN CÔNG NGHỆ
- **PACKAGE MANAGER**: Chỉ sử dụng duy nhất **npm** cho dự án Frontend. Nghiêm cấm dùng lẫn lộn yarn/pnpm.
- **BACKEND**: Luôn sử dụng `dotnet run` / `dotnet test`.

## 5. BẮT BẮC CHUẨN DESIGN SYSTEM
- Mọi màn hình UI bắt buộc phải được bọc đủ 4 trạng thái (Loading, Empty, Error, Success).
