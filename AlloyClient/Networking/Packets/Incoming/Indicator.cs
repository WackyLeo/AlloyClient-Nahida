using System;
using System.Numerics;
using AlloyClient.Assets.Libraries;
using AlloyClient.Game;
using AlloyClient.Game.Objects;
using AlloyClient.Networking.Structs.DataObjects;
using AlloyClient.ParticleEffects;
using AlloyClient.State;
using Common.Structs;

namespace AlloyClient.Networking.Packets.Incoming;

public class Indicator : IncomingPacket<Indicator>
{
    public int Time;
    public float Angle;
    public Position Position;

    public override PacketId PacketId => PacketId.Indicator;

    public override void Reset()
    {
        Time = 0;
        Angle = 0;
        Position.Reset();
    }

    public override void Read(NetworkReader reader) 
    {
        Time = reader.ReadInt32();
        Angle = reader.ReadSingle();
        Position.Read(reader);
    }

    public override void Handle()
    {
        Console.WriteLine("Time: " + Time);
        Console.WriteLine("Angle: " + Angle);
        Console.WriteLine("Position: " + Position);
        
        for (int t = 0; t < 500; t++)
            if (t % 100 == 0)
            {
                OpenTK.Mathematics.Vector2 pos = new OpenTK.Mathematics.Vector2(
                    Position.X + (float)Math.Cos(Angle) * (t / 100f), 
                    Position.Y + (float)Math.Sin(Angle) * (t / 100f));
                var rcp = new RayCastParticle(new AtlasData()
                {
                    U = 0.74365234f,
                    V = 0.1694336f,
                    W = 0.0024414062f,
                    H = 0.0024414062f
                }, pos, Angle, 1, 100, 10);
                Map.AddParticleEffect(rcp);
            }
    }

    public override string ToString() {
        return
            $"Time: {Time}, Angle: {Angle},  Position: {Position}";
    }
}