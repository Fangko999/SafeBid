# GHI NHỚ NGỮ CẢNH (MEMORY)

## 1. Vừa làm gì?
- Hoàn thành **Task 14 (Auction Q&A)**: Bổ sung tính năng Hỏi Đáp công khai trên trang Chi Tiết Phiên Đấu Giá.
- Backend: Tạo entity `Question`, API `POST /api/auctions/{id}/questions` và `POST /api/auctions/{id}/questions/{questionId}/answer`, cùng `GET /api/auctions/{id}/questions`. Cấu hình RateLimit (`QuestionLimit`) 10 câu/phút. Thuật toán che giấu tên người hỏi giống Task 13.
- Frontend: Tạo component `QnAPanel.tsx`, cho phép người mua hỏi và người bán trả lời. Hiển thị danh sách Q&A theo thời gian thực (reload list sau khi gọi API thành công). 
- Cập nhật `openapi.yaml`.
- Các bài test (Backend xUnit và Frontend Vitest) đều pass xanh (100%).

## 2. Việc dở dang
- Không có việc dở dang. Mọi thứ đã hoàn thiện và passed.

## 3. Lưu ý cho phiên sau
- API RateLimit có thể được áp dụng lại ở các endpoint khác (ví dụ: bình luận, nhắn tin). Cấu hình RateLimiter trong `Program.cs` có thể được gom lại vào 1 thư mục/class extensions cho gọn nếu số lượng policy tăng lên.
- Khi mock `fetch` trong Vitest có kiểm tra `res.status`, cần nhớ set trường `status` vào object mock (ví dụ: `status: 201`).

## 4. Task tiếp theo
- **CHECKPOINT REVIEW 4**: Đây là thời điểm Checkpoint. Cần rà soát và đề nghị user mở conversation mới trước khi làm Task 15 (Proxy Bidding).
