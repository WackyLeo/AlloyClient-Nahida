namespace AlloyClient.Networking.Enums;

public enum EffectType : uint {
    Unknown = 0,
    Heal = 1, // target, color
    Teleport = 2, // pos1
    Stream = 3, // pos1, pos2, color
    Throw = 4, // target, pos1, color, duration
    Nova = 5, // target, radius, color
    Poison = 6, // target, color
    Trail = 7, // target, pos1, color
    Burst = 8, // target, pos1, pos2, color
    Flow = 9, // target, pos1, color
    Ring = 10, // target, radius in pos1.x, color
    Trap = Ring,
    Lightning = 11, // target, pos1, color, particle size in pos2.x
    Collapse = 12, // target, pos1, pos2, color
    ConeBlast = 13, // target, pos1, radius in pos2.x, color

    Jitter = 14,
    Earthquake = Jitter,

    Flashing = 15, // target, color, period in pos1.x, cycles in pos1.y
    ThrowProjectile = 16, // object type in color, start pos2, end pos1
    ObjectToss = ThrowProjectile,

    Shocker = 17,
    Shockee = 18,
    RisingFury = 19,
    NecroLine = 20,
    NecroRadius = 21,
    NecroDamage = 22
}
