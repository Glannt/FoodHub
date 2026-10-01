using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace FoodHub.SharedKernel;

public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null)
            throw new InvalidOperationException("Không thể tạo Result thành công với lỗi");
        if (!isSuccess && error is null)
            throw new InvalidOperationException("Không thể tạo Result thất bại mà không có lỗi");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }
    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, null);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

}

public class Result<TValue> : Result
{
    private readonly TValue? _value;
    protected internal Result(TValue? value, bool isSuccess, Error? error)
        : base(isSuccess, error) => _value = value;
    public TValue Value => IsSuccess ?
        _value! :
        throw new InvalidOperationException("Đọc value từ Result thất bại");
}

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static Error NotFound(string what) => new("NotFound", $"Không tìm thấy {what}");
    public static Error Validation(string message) => new("Validation", message);
}
