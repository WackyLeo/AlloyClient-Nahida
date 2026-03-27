using System;
using System.Collections.Generic;
using AlloyClient.Rendering.VertexData;
using OpenTK.Mathematics;

namespace AlloyClient.Rendering.Types;

public static class TypeCaveWallGeo {
    public const int Dot = 0;
    public const int ShortLine = 1;
    public const int L = 2;
    public const int Line = 3;
    public const int T = 4;
    public const int Cross = 5;

    private static readonly Dictionary<int, (int type, int rot)> ConnectDict = BuildDict();

    private static Dictionary<int, (int, int)> BuildDict() {
        var d = new Dictionary<int, (int, int)>();

        void InitHelper(int arg1, int type) {
            for (int i = 0; i < 4; i++) {
                int n = (arg1 & 0xFF) == 1 ? 1 : 0;
                int e = ((arg1 >> 8) & 0xFF) == 1 ? 1 : 0;
                int s = ((arg1 >> 16) & 0xFF) == 1 ? 1 : 0;
                int w = ((arg1 >> 24) & 0xFF) == 1 ? 1 : 0;
                int mask = n | (e << 1) | (s << 2) | (w << 3);

                if (!d.ContainsKey(mask)) d[mask] = (type, i);

                int local4 = arg1 & 0xFF;
                arg1 = (arg1 >> 8) | (local4 << 24);
            }
        }

        InitHelper(33686018, Dot);
        InitHelper(16908802, ShortLine);
        InitHelper(16843266, L);
        InitHelper(16908546, Line);
        InitHelper(16843265, T);
        InitHelper(16843009, Cross);

        return d;
    }

    public static void Build(float posX, float posY, int connectType, Vector4 atlasUV, List<CaveWallVertex> vertices) {
        int mask = 0;
        if (connectType > 255) {
            mask |= ((connectType & 0xFF) == 1) ? 1 : 0;
            mask |= (((connectType >> 8) & 0xFF) == 1) ? 2 : 0;
            mask |= (((connectType >> 16) & 0xFF) == 1) ? 4 : 0;
            mask |= (((connectType >> 24) & 0xFF) == 1) ? 8 : 0;
        } else {
            mask |= ((connectType & 0x01) != 0) ? 1 : 0;
            mask |= ((connectType & 0x04) != 0) ? 2 : 0;
            mask |= ((connectType & 0x10) != 0) ? 4 : 0;
            mask |= ((connectType & 0x40) != 0) ? 8 : 0;
        }

        if (!ConnectDict.TryGetValue(mask, out var result)) {
            result = (Dot, 0);
        }

        int tx = (int)posX;
        int ty = (int)posY;
        var builder = new Builder(tx, ty, posX, posY, result.rot, atlasUV, vertices);

        switch (result.type) {
            case Dot: builder.BuildDot(); break;
            case ShortLine: builder.BuildShortLine(); break;
            case L: builder.BuildL(); break;
            case Line: builder.BuildLine(); break;
            case T: builder.BuildT(); break;
            case Cross: builder.BuildCross(); break;
        }
    }

    private ref struct Builder {
        private readonly int _tx;
        private readonly int _ty;
        private readonly float _posX;
        private readonly float _posY;
        private readonly int _rot;
        private readonly Vector4 _atlas;
        private readonly List<CaveWallVertex> _verts;
        private readonly Random _rng;

        public Builder(int tx, int ty, float posX, float posY, int rot, Vector4 atlas, List<CaveWallVertex> verts) {
            _tx = tx;
            _ty = ty;
            _posX = posX;
            _posY = posY;
            _rot = rot;
            _atlas = atlas;
            _verts = verts;
            _rng = new Random((tx * 73856) ^ (ty * 19274));
        }

        private float Rnd() => (float)_rng.NextDouble();

        public void BuildDot() {
            var l1 = P(-0.25f - Rnd() * 0.25f, -0.25f - Rnd() * 0.25f, 0f);
            var l2 = P(0.25f + Rnd() * 0.25f, -0.25f - Rnd() * 0.25f, 0f);
            var l3 = P(0.25f + Rnd() * 0.25f, 0.25f + Rnd() * 0.25f, 0f);
            var l4 = P(-0.25f - Rnd() * 0.25f, 0.25f + Rnd() * 0.25f, 0f);
            var l5 = P(-0.25f + Rnd() * 0.5f, -0.25f + Rnd() * 0.5f, 1f);

            FaceHelper(l5, l1, l2);
            FaceHelper(l5, l2, l3);
            FaceHelper(l5, l3, l4);
            FaceHelper(l5, l4, l1);
        }

        public void BuildShortLine() {
            var l1 = V(0, 0);
            var l2 = V(0, 3);
            var l3 = P(0.25f + Rnd() * 0.25f, 0.25f + Rnd() * 0.25f, 0f);
            var l4 = P(-0.25f - Rnd() * 0.25f, 0.25f + Rnd() * 0.25f, 0f);
            var l5 = V(0, 1);
            var l6 = V(0, 2);
            var l7 = P(Rnd() * 0.25f, Rnd() * 0.25f, 0.5f);
            var l8 = P(-Rnd() * 0.25f, Rnd() * 0.25f, 0.5f);

            FaceHelper(l5, l8, l4, l1);
            FaceHelper(l8, l7, l3, l4);
            FaceHelper(l7, l6, l2, l3);
            FaceHelper(l5, l6, l7, l8);
        }

        public void BuildL() {
            var l1 = V(0, 0);
            var l2 = V(0, 3);
            var l3 = V(1, 0);
            var l4 = V(1, 3);
            var l5 = P(-Rnd() * 0.25f, Rnd() * 0.25f, 0f);
            var l6 = V(0, 1);
            var l7 = V(0, 2);
            var l8 = V(1, 1);
            var l9 = V(1, 2);
            var l10 = P(Rnd() * 0.25f, -Rnd() * 0.25f, 1f);

            FaceHelper(l6, l10, l5, l1);
            FaceHelper(l10, l9, l4, l5);
            FaceHelper(l8, l7, l2, l3);
            FaceHelper(l6, l7, l8, l9, l10);
        }

        public void BuildLine() {
            var l1 = V(0, 0);
            var l2 = V(0, 3);
            var l3 = V(2, 3);
            var l4 = V(2, 0);
            var l5 = V(0, 1);
            var l6 = V(0, 2);
            var l7 = V(2, 2);
            var l8 = V(2, 1);

            FaceHelper(l5, l8, l4, l1);
            FaceHelper(l7, l6, l2, l3);
            FaceHelper(l5, l6, l7, l8);
        }

        public void BuildT() {
            var l1 = V(0, 0);
            var l2 = V(0, 3);
            var l3 = V(1, 0);
            var l4 = V(1, 3);
            var l5 = V(3, 3);
            var l6 = V(3, 0);
            var l7 = V(0, 1);
            var l8 = V(0, 2);
            var l9 = V(1, 1);
            var l10 = V(1, 2);
            var l11 = V(3, 2);
            var l12 = V(3, 1);

            FaceHelper(l9, l8, l2, l3);
            FaceHelper(l11, l10, l4, l5);
            FaceHelper(l7, l12, l6, l1);
            FaceHelper(l7, l8, l9, l10, l11, l12);
        }

        public void BuildCross() {
            var l1 = V(0, 0);
            var l2 = V(0, 3);
            var l3 = V(1, 0);
            var l4 = V(1, 3);
            var l5 = V(2, 3);
            var l6 = V(2, 0);
            var l7 = V(3, 3);
            var l8 = V(3, 0);
            var l9 = V(0, 1);
            var l10 = V(0, 2);
            var l11 = V(1, 1);
            var l12 = V(1, 2);
            var l13 = V(2, 2);
            var l14 = V(2, 1);
            var l15 = V(3, 2);
            var l16 = V(3, 1);

            FaceHelper(l11, l10, l2, l3);
            FaceHelper(l13, l12, l4, l5);
            FaceHelper(l15, l14, l6, l7);
            FaceHelper(l9, l16, l8, l1);
            FaceHelper(l9, l10, l11, l12, l13, l14, l15, l16);
        }

        private Vector3 V(int side, int corner) {
            int hx = _tx;
            int hy = _ty;
            int worldSide = (side + _rot) % 4;

            if (worldSide == 1) hx++;
            if (worldSide == 2) hy++;

            int offset;
            if (corner == 0 || corner == 3) {
                offset = 15 + (((hx * 1259) ^ (hy * 2957)) % 35);
            } else {
                offset = 3 + (((hx * 2179) ^ (hy * 1237)) % 35);
            }

            float t = offset / 100f;
            float lz = (corner == 1 || corner == 2) ? 1f : 0f;
            float ct = (corner == 0 || corner == 1) ? -t : t;

            float lx = 0, ly = 0;
            switch (side) {
                case 0: lx = ct; ly = -0.5f; break;
                case 1: lx = 0.5f; ly = ct; break;
                case 2: lx = ct; ly = 0.5f; break;
                case 3: lx = -0.5f; ly = ct; break;
            }

            return P(lx, ly, lz);
        }

        private Vector3 P(float lx, float ly, float lz) {
            float rx = lx;
            float ry = ly;

            for (int i = 0; i < _rot; i++) {
                float tmp = rx;
                rx = -ry;
                ry = tmp;
            }
            return new Vector3(_posX + rx, _posY + ry, lz);
        }

        private void FaceHelper(params Vector3[] v) {
            if (v.Length == 3) {
                Tri(v[0], 0f, 0f, v[1], 1f, 0f, v[2], 1f, 1f);
            }
            else if (v.Length == 4) {
                /*if (Rnd() < 0.5f) {
                    Tri(v[0], 0f, 0f, v[1], 1f, 0f, v[3], 0f, 1f);
                    Tri(v[1], 1f, 0f, v[2], 1f, 1f, v[3], 0f, 1f);
                } else {
                    Tri(v[0], 0f, 0f, v[2], 1f, 1f, v[3], 0f, 1f);
                    Tri(v[0], 0f, 0f, v[1], 1f, 0f, v[2], 1f, 1f);
                }*/
                Tri(v[0], 0f, 0f, v[2], 1f, 1f, v[3], 0f, 1f);
                Tri(v[0], 0f, 0f, v[1], 1f, 0f, v[2], 1f, 1f);
            }
            else if (v.Length == 5) {
                Vector2[] uvs = { new(0.2f,0f), new(0.8f,0f), new(1f,0.2f), new(1f,0.8f), new(0f,0.8f) };
                Fan(v, uvs);
            }
            else if (v.Length == 6) {
                Vector2[] uvs = { new(0f,0f), new(0.2f,0f), new(1f,0.2f), new(1f,0.8f), new(0f,0.8f), new(0f,0.2f) };
                Fan(v, uvs);
            }
            else if (v.Length == 8) {
                Vector2[] uvs = { new(0f,0f), new(0.2f,0f), new(1f,0.2f), new(1f,0.8f), new(0.8f,1f), new(0.2f,1f), new(0f,0.8f), new(0f,0.2f) };
                Fan(v, uvs);
            }
        }

        private void Fan(Vector3[] v, Vector2[] uvs) {
            for (int i = 1; i < v.Length - 1; i++) {
                Tri(v[0], uvs[0].X, uvs[0].Y,
                    v[i], uvs[i].X, uvs[i].Y,
                    v[i+1], uvs[i+1].X, uvs[i+1].Y);
            }
        }

        private void Tri(Vector3 v1, float u1, float v2_1, Vector3 v2, float u2, float v2_2, Vector3 v3, float u3, float v2_3) {
            _verts.Add(new CaveWallVertex(v1, AtlasUV(u1, v2_1), 1f));
            _verts.Add(new CaveWallVertex(v2, AtlasUV(u2, v2_2), 1f));
            _verts.Add(new CaveWallVertex(v3, AtlasUV(u3, v2_3), 1f));
        }

        private Vector2 AtlasUV(float u, float v) {
            float width = (_atlas.Z > _atlas.X && _atlas.Z <= 1.0f) ? (_atlas.Z - _atlas.X) : _atlas.Z;
            float height = (_atlas.W > _atlas.Y && _atlas.W <= 1.0f) ? (_atlas.W - _atlas.Y) : _atlas.W;
            return new Vector2(_atlas.X + u * width, _atlas.Y + v * height);
        }
    }
}