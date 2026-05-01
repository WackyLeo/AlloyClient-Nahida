using System.Collections.Generic;
using AlloyClient.Rendering.VertexData;
using OpenTK.Mathematics;

namespace AlloyClient.Rendering.Types;

public static class TypeConnectedWallGeo {
    private const int ShapeTypeDot = 0;
    private const int ShapeTypeShortLine = 1;
    private const int ShapeTypeL = 2;
    private const int ShapeTypeLine = 3;
    private const int ShapeTypeT = 4;
    private const int ShapeTypeCross = 5;

    private static readonly Dictionary<int, (int type, int rot)> ConnectDict = BuildDict();

    private static Dictionary<int, (int, int)> BuildDict() {
        var d = new Dictionary<int, (int, int)>();

        void Register(int arg1, int type) {
            for (var i = 0; i < 4; i++) {
                var n = (arg1 & 0xFF) == 1 ? 1 : 0;
                var e = ((arg1 >> 8) & 0xFF) == 1 ? 1 : 0;
                var s = ((arg1 >> 16) & 0xFF) == 1 ? 1 : 0;
                var w = ((arg1 >> 24) & 0xFF) == 1 ? 1 : 0;
                var mask = n | (e << 1) | (s << 2) | (w << 3);
                if (!d.ContainsKey(mask))
                    d[mask] = (type, i);
                var low4 = arg1 & 0xFF;
                arg1 = (arg1 >> 8) | (low4 << 24);
            }
        }

        Register(33686018, ShapeTypeDot);
        Register(16908802, ShapeTypeShortLine);
        Register(16843266, ShapeTypeL);
        Register(16908546, ShapeTypeLine);
        Register(16843265, ShapeTypeT);
        Register(16843009, ShapeTypeCross);
        return d;
    }

    public static void Build(float posX, float posY, int connectType, Vector4 atlasUV,
        float sortId, List<CaveWallVertex> vertices,
        float bI, float tI, float h,
        Vector4 uvDot, Vector4 uvShortLine, Vector4 uvL,
        Vector4 uvLine, Vector4 uvT, Vector4 uvCross) {
        int mask;
        if (connectType > 255) {
            mask = (connectType & 0xFF) == 1 ? 1 : 0;
            mask |= ((connectType >> 8) & 0xFF) == 1 ? 2 : 0;
            mask |= ((connectType >> 16) & 0xFF) == 1 ? 4 : 0;
            mask |= ((connectType >> 24) & 0xFF) == 1 ? 8 : 0;
        }
        else {
            mask = (connectType & 0x01) != 0 ? 1 : 0;
            mask |= (connectType & 0x04) != 0 ? 2 : 0;
            mask |= (connectType & 0x10) != 0 ? 4 : 0;
            mask |= (connectType & 0x40) != 0 ? 8 : 0;
        }

        if (!ConnectDict.TryGetValue(mask, out var result))
            result = (ShapeTypeDot, 0);

        var topUV = result.type switch {
            ShapeTypeDot => uvDot,
            ShapeTypeShortLine => uvShortLine,
            ShapeTypeL => uvL,
            ShapeTypeLine => uvLine,
            ShapeTypeT => uvT,
            ShapeTypeCross => uvCross,
            _ => atlasUV
        };

        var b = new Builder(posX, posY, result.rot, atlasUV, topUV, vertices, sortId, bI, tI, h);
        switch (result.type) {
            case ShapeTypeDot:
                b.BuildDot();
                break;
            case ShapeTypeShortLine:
                b.BuildShortLine();
                break;
            case ShapeTypeL:
                b.BuildL();
                break;
            case ShapeTypeLine:
                b.BuildLine();
                break;
            case ShapeTypeT:
                b.BuildT();
                break;
            case ShapeTypeCross:
                b.BuildCross();
                break;
        }
    }

    private ref struct Builder {
        private readonly float _posX, _posY;
        private readonly int _rot;
        private readonly Vector4 _atlas, _topAtlas;
        private readonly List<CaveWallVertex> _verts;
        private readonly float _sortId, _bI, _tI, _h, _uvOff;

        public Builder(float posX, float posY, int rot, Vector4 atlas, Vector4 topAtlas,
            List<CaveWallVertex> verts, float sortId, float bI, float tI, float h) {
            _posX = posX;
            _posY = posY;
            _rot = rot;
            _atlas = atlas;
            _topAtlas = topAtlas;
            _verts = verts;
            _sortId = sortId;
            _bI = bI;
            _tI = tI;
            _h = h;
            _uvOff = 0f;
        }

        public void BuildDot() {
            var b0 = P(-_bI, -_bI, 0);
            var b1 = P(_bI, -_bI, 0);
            var b2 = P(_bI, _bI, 0);
            var b3 = P(-_bI, _bI, 0);
            var t0 = P(-_tI, -_tI, _h);
            var t1 = P(_tI, -_tI, _h);
            var t2 = P(_tI, _tI, _h);
            var t3 = P(-_tI, _tI, _h);
            SideQuad(t1, t0, b0, b1, true, true);
            SideQuad(t2, t1, b1, b2, true, true);
            SideQuad(t0, t3, b3, b0, true, true);
            SideQuad(t3, t2, b2, b3, true, true);
            TopFan(t0, t1, t2, t3);
        }

        public void BuildShortLine() {
            var b0 = P(-_bI, -0.5f, 0);
            var b1 = P(_bI, -0.5f, 0);
            var b2 = P(_bI, _bI, 0);
            var b3 = P(-_bI, _bI, 0);
            var t0 = P(-_tI, -0.5f, _h);
            var t1 = P(_tI, -0.5f, _h);
            var t2 = P(_tI, _tI, _h);
            var t3 = P(-_tI, _tI, _h);
            SideQuad(t2, t1, b1, b2, true, false);
            SideQuad(t0, t3, b3, b0, false, true);
            SideQuad(t3, t2, b2, b3, true, true);
            TopFan(t0, t1, t2, t3);
        }

        public void BuildL() {
            var b0 = P(-_bI, -0.5f, 0);
            var b1 = P(_bI, -0.5f, 0);
            var b2 = P(_bI, -_bI, 0);
            var b3 = P(0.5f, -_bI, 0);
            var b4 = P(0.5f, _bI, 0);
            var b5 = P(-_bI, _bI, 0);
            var t0 = P(-_tI, -0.5f, _h);
            var t1 = P(_tI, -0.5f, _h);
            var t2 = P(_tI, -_tI, _h);
            var t3 = P(0.5f, -_tI, _h);
            var t4 = P(0.5f, _tI, _h);
            var t5 = P(-_tI, _tI, _h);
            SideQuad(t2, t1, b1, b2, true, false);
            SideQuad(t3, t2, b2, b3, false, false);
            SideQuad(t5, t4, b4, b5, true, false);
            SideQuad(t0, t5, b5, b0, false, true);
            TopTriUV(t5, t0, t1);
            TopTriUV(t5, t1, t2);
            TopTriUV(t5, t2, t4);
            TopTriUV(t2, t3, t4);
        }

        public void BuildLine() {
            var b0 = P(-_bI, -0.5f, 0);
            var b1 = P(_bI, -0.5f, 0);
            var b2 = P(_bI, 0.5f, 0);
            var b3 = P(-_bI, 0.5f, 0);
            var t0 = P(-_tI, -0.5f, _h);
            var t1 = P(_tI, -0.5f, _h);
            var t2 = P(_tI, 0.5f, _h);
            var t3 = P(-_tI, 0.5f, _h);
            SideQuad(t2, t1, b1, b2, false, false);
            SideQuad(t0, t3, b3, b0, false, false);
            TopFan(t0, t1, t2, t3);
        }

        public void BuildT() {
            var b0 = P(-_bI, -0.5f, 0);
            var b1 = P(_bI, -0.5f, 0);
            var b2 = P(_bI, -_bI, 0);
            var b3 = P(0.5f, -_bI, 0);
            var b4 = P(0.5f, _bI, 0);
            var b5 = P(-0.5f, _bI, 0);
            var b6 = P(-0.5f, -_bI, 0);
            var b7 = P(-_bI, -_bI, 0);
            var t0 = P(-_tI, -0.5f, _h);
            var t1 = P(_tI, -0.5f, _h);
            var t2 = P(_tI, -_tI, _h);
            var t3 = P(0.5f, -_tI, _h);
            var t4 = P(0.5f, _tI, _h);
            var t5 = P(-0.5f, _tI, _h);
            var t6 = P(-0.5f, -_tI, _h);
            var t7 = P(-_tI, -_tI, _h);
            SideQuad(t2, t1, b1, b2, true, false);
            SideQuad(t3, t2, b2, b3, false, false);
            SideQuad(t5, t4, b4, b5, false, false);
            SideQuad(t7, t6, b6, b7, true, false);
            SideQuad(t0, t7, b7, b0, false, false);
            TopTriUV(t7, t0, t1);
            TopTriUV(t7, t1, t2);
            TopTriUV(t2, t3, t4);
            TopTriUV(t5, t6, t7);
            TopTriUV(t7, t2, t4);
            TopTriUV(t7, t4, t5);
        }

        public void BuildCross() {
            var b0 = P(-_bI, -0.5f, 0);
            var b1 = P(_bI, -0.5f, 0);
            var b2 = P(_bI, -_bI, 0);
            var b3 = P(0.5f, -_bI, 0);
            var b4 = P(0.5f, _bI, 0);
            var b5 = P(_bI, _bI, 0);
            var b6 = P(_bI, 0.5f, 0);
            var b7 = P(-_bI, 0.5f, 0);
            var b8 = P(-_bI, _bI, 0);
            var b9 = P(-0.5f, _bI, 0);
            var b10 = P(-0.5f, -_bI, 0);
            var b11 = P(-_bI, -_bI, 0);
            var t0 = P(-_tI, -0.5f, _h);
            var t1 = P(_tI, -0.5f, _h);
            var t2 = P(_tI, -_tI, _h);
            var t3 = P(0.5f, -_tI, _h);
            var t4 = P(0.5f, _tI, _h);
            var t5 = P(_tI, _tI, _h);
            var t6 = P(_tI, 0.5f, _h);
            var t7 = P(-_tI, 0.5f, _h);
            var t8 = P(-_tI, _tI, _h);
            var t9 = P(-0.5f, _tI, _h);
            var t10 = P(-0.5f, -_tI, _h);
            var t11 = P(-_tI, -_tI, _h);
            SideQuad(t2, t1, b1, b2, true, false);
            SideQuad(t3, t2, b2, b3, false, false);
            SideQuad(t5, t4, b4, b5, true, false);
            SideQuad(t6, t5, b5, b6, false, false);
            SideQuad(t8, t7, b7, b8, true, false);
            SideQuad(t9, t8, b8, b9, false, false);
            SideQuad(t11, t10, b10, b11, true, false);
            SideQuad(t0, t11, b11, b0, false, false);
            TopTriUV(t11, t0, t1);
            TopTriUV(t11, t1, t2);
            TopTriUV(t2, t3, t4);
            TopTriUV(t2, t4, t5);
            TopTriUV(t5, t6, t7);
            TopTriUV(t5, t7, t8);
            TopTriUV(t8, t9, t10);
            TopTriUV(t8, t10, t11);
            TopTriUV(t11, t2, t5);
            TopTriUV(t11, t5, t8);
        }

        private void SideQuad(Vector3 tl, Vector3 tr, Vector3 br, Vector3 bl,
            bool taperLeft, bool taperRight) {
            var offL = taperLeft ? _uvOff : 0f;
            var offR = taperRight ? _uvOff : 0f;
            Tri(tl, 0f, 0f, tr, 1f, 0f, br, 1f + offR, 1f);
            Tri(tl, 0f, 0f, br, 1f + offR, 1f, bl, 0f + offL, 1f);
        }

        private void TopFan(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {
            TopTriUV(p0, p1, p2);
            TopTriUV(p0, p2, p3);
        }

        private void TopTriUV(Vector3 a, Vector3 b, Vector3 c) {
            TopTri(a, UV(a), b, UV(b), c, UV(c));
        }

        private Vector2 UV(Vector3 world) {
            var rx = world.X - _posX;
            var ry = world.Y - _posY;
            for (var r = 0; r < (4 - _rot) % 4; r++) {
                var tmp = rx;
                rx = -ry;
                ry = tmp;
            }

            return new Vector2(rx + 0.5f, ry + 0.5f);
        }

        private Vector3 P(float lx, float ly, float lz) {
            float rx = lx, ry = ly;
            for (var i = 0; i < _rot; i++) {
                var tmp = rx;
                rx = -ry;
                ry = tmp;
            }

            return new Vector3(_posX + rx, _posY + ry, lz);
        }

        private void Tri(Vector3 v1, float u1, float f1,
            Vector3 v2, float u2, float f2,
            Vector3 v3, float u3, float f3) {
            _verts.Add(new CaveWallVertex(v1, AtlasUV(u1, f1), 1f, _sortId - v1.Z * 0.001f));
            _verts.Add(new CaveWallVertex(v2, AtlasUV(u2, f2), 1f, _sortId - v2.Z * 0.001f));
            _verts.Add(new CaveWallVertex(v3, AtlasUV(u3, f3), 1f, _sortId - v3.Z * 0.001f));
        }

        private void TopTri(Vector3 v1, Vector2 uv1,
            Vector3 v2, Vector2 uv2,
            Vector3 v3, Vector2 uv3) {
            _verts.Add(new CaveWallVertex(v1, TopAtlasUV(uv1.X, uv1.Y), 1f, _sortId - v1.Z * 0.001f));
            _verts.Add(new CaveWallVertex(v2, TopAtlasUV(uv2.X, uv2.Y), 1f, _sortId - v2.Z * 0.001f));
            _verts.Add(new CaveWallVertex(v3, TopAtlasUV(uv3.X, uv3.Y), 1f, _sortId - v3.Z * 0.001f));
        }

        private Vector2 AtlasUV(float u, float v) {
            var w = _atlas.Z > _atlas.X && _atlas.Z <= 1f ? _atlas.Z - _atlas.X : _atlas.Z;
            var h = _atlas.W > _atlas.Y && _atlas.W <= 1f ? _atlas.W - _atlas.Y : _atlas.W;
            return new Vector2(_atlas.X + u * w, _atlas.Y + v * h);
        }

        private Vector2 TopAtlasUV(float u, float v) {
            var w = _topAtlas.Z > _topAtlas.X && _topAtlas.Z <= 1f ? _topAtlas.Z - _topAtlas.X : _topAtlas.Z;
            var h = _topAtlas.W > _topAtlas.Y && _topAtlas.W <= 1f ? _topAtlas.W - _topAtlas.Y : _topAtlas.W;
            return new Vector2(_topAtlas.X + u * w, _topAtlas.Y + v * h);
        }
    }
}