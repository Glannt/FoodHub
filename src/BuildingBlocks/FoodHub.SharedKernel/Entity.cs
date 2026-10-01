using System;
using System.Collections.Generic;
using System.Text;

namespace FoodHub.SharedKernel;

public abstract class Entity<TId> where TId: IEquatable<TId>
    // This is a generic base class for entities with an identifier of type TId
    // TId là kiểu dữ liệu của định danh (ID) của thực thể, và nó phải triển khai giao diện IEquatable<TId> để so sánh các giá trị ID.
{
    public TId Id { get; protected set; } = default!; // The unique identifier for the entity
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow; // The timestamp when the entity was created
    public DateTimeOffset? UpdatedAt { get; private set; }

    protected void Touch() => UpdatedAt = DateTimeOffset.UtcNow; // Update the UpdatedAt timestamp to the current time

    /// <summary>
    /// Override the Equals method to compare entities based on their Id property
    /// is có nghĩa là kiểm tra xem obj có phải là một thực thể cùng loại với
    /// Entity<TId> hay không, và nếu đúng, so sánh Id của chúng để xác định xem
    /// chúng có bằng nhau hay không.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj) =>
        obj is Entity<TId> other && Id.Equals(other.Id);
    public override int GetHashCode() => Id.GetHashCode();
    /// <summary>
    /// dấu == được sử dụng để so sánh hai thực thể Entity<TId>
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);
    /// <summary>
    /// dấu != được sử dụng để so sánh hai thực thể Entity<TId>
    /// và xác định xem chúng có khác nhau hay không.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);

}
