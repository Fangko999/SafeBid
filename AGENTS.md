# MỤC ĐÍCH CỦA FILE NÀY
Đây là file cấu hình luật dành riêng cho các AI Agents (Gemini, Copilot, v.v.) làm việc trong dự án SafeBid. File này sẽ tự động nạp các ràng buộc để đảm bảo AI tuân thủ nguyên tắc.

- BẮT BUỘC đọc và tuân thủ các luật từ file `CONSTRAINTS.md`.
- Tuyệt đối không thay đổi mã nguồn làm suy yếu hoặc lách các ràng buộc trong `CONSTRAINTS.md`.

---

## MẪU THAM CHIẾU
Khi cần viết code tương tự, hãy đọc các file này trước để bắt chước style và architecture hiện có:
- **API Controller (CQRS + Result Pattern):** `src/Backend/SafeBid.Api/Controllers/AuthController.cs`
- **Application Logic (MediatR Command Handler + Transaction):** `src/Backend/SafeBid.Application/RegisterCommandHandler.cs`
- **Integration Test (Testcontainers + SharedFixture):** `src/Tests/SafeBid.IntegrationTests/AuthRegisterTests.cs`
- **Integration Test (Yêu cầu xác thực/Cookie):** `src/Tests/SafeBid.IntegrationTests/AuthLoginTests.cs`
- **Frontend Page Component (Next.js + Shadcn + Zod Form):** `src/Frontend/src/app/register/page.tsx`
- **Frontend State Management (Zustand):** `src/Frontend/src/store/authStore.ts`
- **Frontend TDD (Vitest + JSDOM + UserEvent):** `src/Frontend/src/app/register/register.test.tsx`
