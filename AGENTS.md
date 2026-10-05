# MỤC ĐÍCH CỦA FILE NÀY
Đây là file cấu hình luật dành riêng cho các AI Agents (Gemini, Copilot, v.v.) làm việc trong dự án SafeBid. File này sẽ tự động nạp các ràng buộc để đảm bảo AI tuân thủ nguyên tắc.

- BẮT BUỘC đọc và tuân thủ các luật từ file `CONSTRAINTS.md`.
- Tuyệt đối không thay đổi mã nguồn làm suy yếu hoặc lách các ràng buộc trong `CONSTRAINTS.md`.

---

## MẪU THAM CHIẾU
- `src/Backend/SafeBid.Api/Controllers/AuthController.cs`: Mẫu API Controller chuẩn, sử dụng MediatR (CQRS) và Rate Limiting.
- `src/Backend/SafeBid.Application/RegisterCommandHandler.cs`: Mẫu Command Handler CQRS, chứa Business Logic, khởi tạo Domain Entities, EF Core Transaction, và sử dụng Result Pattern.
- `src/Tests/SafeBid.IntegrationTests/AuthRegisterTests.cs`: Mẫu Integration Test gọi API dùng Testcontainers và `SharedTestCollection` để tránh lỗi Race Condition khi tạo DB.
