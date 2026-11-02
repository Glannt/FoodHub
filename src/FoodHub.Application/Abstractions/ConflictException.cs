namespace FoodHub.Application.Abstractions;

/// <summary>
/// Ném ra khi request đúng cú pháp, đã qua validation, nhưng vi phạm một quy tắc nghiệp vụ
/// khiến thao tác không thể thực hiện (ví dụ: tạo nhà hàng trùng với nhà hàng đã có).
/// <para>
/// Phân biệt rõ với <see cref="NotFoundException"/>:
/// dữ liệu mình đưa vào KHÔNG tồn tại (404) và dữ liệu tồn tại nhưng MÂU THUẪN với dữ liệu
/// đã có (409) là hai lỗi khác nhau, client xử lý khác nhau nên phải ra hai status code khác nhau.
/// </para>
/// </summary>
public sealed class ConflictException(string message) : Exception(message);