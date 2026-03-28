using System.Collections.Generic;
using AlloyClient.Rendering.Types;

namespace AlloyClient.Game.Objects;

public static class ObjectPools {
    public static readonly ObjectPool<TypeProjectile> TypeProjectiles = new();
    public static readonly ObjectPool<Projectile> Projectiles = new();
}

public sealed class ObjectPool<T>(int capacity = 1000) where T : new() {
    private readonly T[] _pool = new T[capacity];
    private int _count;

    public T Pop() {
        if (_count < 1) return new T();
        return _pool[--_count];
    }

    public void Push(T obj) {
        if (_count >= capacity) return;
        _pool[_count++] = obj;
    }
}