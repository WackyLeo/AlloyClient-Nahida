using AlloyClient.Engine.Common;
using AlloyClient.Game.Objects;
using AlloyClient.Rendering.VertexData;
using AlloyClient.UiLib;
using AlloyClient.UiLib.Enums;
using OpenTK.Mathematics;

namespace AlloyClient.Rendering.Types.SubTypes;

public class TypeName : SubRenderBase {

    private float _height;
    public override float Height => _height * 1.75f;

    public string Name;
    private GlyphData[] _glyphs;

    public TypeName(RenderBase parent, Entity entity, string name = "") {
        Parent = parent;
        Entity = entity;
        
        Name = name;
        if (string.IsNullOrEmpty(name)) {
            Name = Entity.Name;
        } 
        if (string.IsNullOrEmpty(Entity.Name)) {
            Name = "";
        }
        
        SetTextures();
        Extra = new ExtraData(RenderConfig.TypeText, RenderConfig.NoShade);
    }

    public void SetTextures() {
        const float size = 0.25f;
        var font = UiRender.GetFont(FontType.Bold);
        _glyphs = new GlyphData[Name.Length];
        _height = font.Ascender * size;
        var cursorX = 0f;

        var len = Name.Length;
        for (var i = 0; i < len; i++) {
            var c = Name[i];
            if (!font.Glyphs.TryGetValue(c, out var glyph)) continue;

            var pos = glyph.Position;
            var w = (pos.X1 - pos.X0) * size;
            var h = (pos.Y0 - pos.Y1) * size;

            var cx = cursorX + (pos.X0 + pos.X1) / 2.0f * size;
            var cy = -(pos.Y0 + pos.Y1) / 2.0f * size; 
            cy += 0.15f;
            
            _glyphs[i] = new GlyphData(glyph.UV.ToVector4(), w, h, cx, cy);
    
            if (i < len - 1 && font.Kernings.TryGetValue((c, Name[i + 1]), out var kern)) {
                cursorX += kern * size;
            }
            cursorX += glyph.Advance * size;
        }
        for (var index = 0; index < _glyphs.Length; index++)
            _glyphs[index].Center(cursorX / 2f);
        
        Rotation = new Vector4(0, 1, 1, -1);
        Color = Entity is Player ? new Color(252, 223, 0, 255) : new Color(255, 255, 255, 255); 
    }
    
    public override void Draw(float yOffset) {
        for (var index = 0; index < _glyphs.Length; index++) {
            var g = _glyphs[index];
            g.Scale.W = yOffset + g.Scale.W;
            Render.DrawEntity(new VertexObject(Parent.Position, g.UV, g.Scale, Rotation, Extra.Data, Color));
            g.Scale.W -= yOffset; 
        }
    }
    
    private struct GlyphData(Vector4 uv, float w, float h, float cx, float cy) {
        public Vector4 UV = uv;
        public Vector4 Scale = new(w, h, cx, cy);
        public void Center(float x) { Scale.Z -= x; }
    }
}