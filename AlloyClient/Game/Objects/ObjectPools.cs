using System.Collections.Generic;
using AlloyClient.Rendering.Types;

namespace AlloyClient.Game.Objects;

public static class ObjectPools {
    public static readonly ObjectPool<TypeProjectile> TypeProjectiles = new();
    public static readonly ObjectPool<Projectile> Projectiles = new();
}

public interface IResettable {
    void Reset();
}

public sealed class ObjectPool<T>(int capacity = 1000) where T : IResettable, new() {
    private readonly T[] _pool = new T[capacity];
    private int _count;

    public T Pop() {
        if (_count < 1) return new T();
        var obj = _pool[--_count];
        _pool[_count] = default;
        return obj;
    }

    public void Push(T obj) {
        if (_count >= capacity) return;
        obj.Reset();
        _pool[_count++] = obj;
    }

    public int Count => _count;
}