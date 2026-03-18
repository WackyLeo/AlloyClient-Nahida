using AlloyClient.Engine.Common;
using AlloyClient.Game.Objects;
using AlloyClient.Rendering.VertexData;
using AlloyClient.UiLib;
using AlloyClient.UiLib.Enums;
using OpenTK.Mathematics;

namespace AlloyClient.Rendering.Types.SubTypes;

public class TypeName : SubRenderBase {

    private float _height;
    public override float Height {
        get => _height * 1.75f;
    }

    public string Name;

    private GlyphData[] _glyphs;

    public TypeName(RenderBase parent, Entity entity) {
        Parent = parent;
        Entity = entity;
        if (entity is Player player) {
            Name = player.Name;
        } else {
            Name = entity.Properties.DisplayName;
        }

        if (string.IsNullOrEmpty(Name))
            Name = "Default ";
        
        SetTextures();
        Extra = new ExtraData(RenderConfig.TypeText, RenderConfig.NoShade);
    }

    public void SetTextures() {
        const float size = 0.55f;

        var font = UiRender.GetFont(FontType.Bold);
        _glyphs = new GlyphData[Name.Length];

        _height = font.Ascender * size;
        var cursorX = 0f;

        var len = Name.Length;
        for (var i = 0; i < len; i++) {
            var c = Name[i];
            if (!font.Glyphs.TryGetValue(c, out var glyph)) continue;

            var pos = glyph.Position;
            var w = (pos.X1 - pos.X0) / 2.0f * size;
            var h = (pos.Y0 - pos.Y1) / 2.0f * size;
            
            var cx = cursorX + w;
            var cy = (pos.Y0 + pos.Y1) / 2.0f * size; 
            _glyphs[i] = new GlyphData(glyph.UV.ToVector4(), w, h, cx, cy);
            

            if (i < len - 1) {
                font.Kernings.TryGetValue((c, Name[i + 1]), out var kern);
                cursorX += kern * size;
            }
            cursorX += glyph.Advance * size;
        }
        for (var index = 0; index < _glyphs.Length; index++)
            _glyphs[index].Center(cursorX / 2f);
        
        Rotation = new Vector4(0, 1, 1, -1);
        Color = new Color(0xFC, 0xDF, 0, 255);
    }
    
    public override void Draw(float yOffset) {
        for (var index = 0; index < _glyphs.Length; index++) {
            var g = _glyphs[index];
            g.Scale.W += yOffset;
            Render.DrawEntity(new VertexObject(Parent.Position, g.UV, g.Scale, Rotation, Extra.Data, Color));
        }
    }
    
    private struct GlyphData(Vector4 uv, float w, float h, float cx, float cy) {
        public Vector4 UV = uv;
        public Vector4 Scale = new(w, h, cx, cy);

        public void Center(float x) {
            Scale.Z -= x;
        }
    }
}