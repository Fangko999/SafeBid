---
description: Nghiêm cấm việc lười biếng cắt xén nội dung khi làm việc với các tài liệu Markdown và Kế hoạch.
---

# No Lazy Truncation

Khi tạo mới, cập nhật, hoặc di chuyển nội dung giữa các file Markdown (đặc biệt là các bản kế hoạch như `todo.md` hay artifacts):
1. **Tuyệt đối không cắt xén chi tiết**: KHÔNG ĐƯỢC lười biếng rút gọn nội dung, cắt bỏ các gạch đầu dòng, mô tả chi tiết, hoặc các bước kiểm thử (Testing Steps) chỉ để tiết kiệm không gian hoặc token.
2. **Bảo tồn Business Logic**: Khi chuyển các task từ file này sang file khác, phải bê nguyên xi (100%) các text mô tả. Việc tự ý tóm tắt bằng tiêu đề sẽ làm phá hủy các luật nghiệp vụ khắt khe đã được đàm phán trước đó.
3. **Phải Exhaustive**: Trừ khi USER ra lệnh rõ ràng là "chỉ cần liệt kê tiêu đề" hoặc "hãy tóm tắt lại", mặc định mọi bản kế hoạch và tài liệu đều phải được xuất ra với ĐẦY ĐỦ nội dung gốc từ đầu đến cuối.
