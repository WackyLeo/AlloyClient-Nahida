using AlloyClient.Game.Objects;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Enums;
using AlloyClient.State;
using AlloyClient.Game;
using System;
using AlloyClient.Game.Objects.Util;
using AlloyClient.UiLib.Rendering;
using OpenTK.Mathematics;

namespace AlloyClient.Ui.InWorld
{
    public class Nameplate : Sprite 
    {
        private readonly Entity _owner;
        private readonly SimpleText _nameText;
        private readonly HpBarBg _hpBg;       
        private readonly ColorRect _hpFg;     
        private readonly Boolean _drawName;      
        private readonly Boolean _drawHpBar;      
        private readonly Boolean _drawShBar;      

        public Nameplate(Entity owner, bool drawName, bool drawHp, bool drawSh) 
        {
            _owner = owner;
            _drawName = drawName;
            _drawHpBar = drawHp;
            _drawShBar = drawSh;
            
            string name = owner is Player p ? p.Name : owner.Properties.DisplayName;
            if (string.IsNullOrEmpty(name)) name = "Default";
            
            if (_drawName) {
                _nameText = new SimpleText(new TextConfig 
                {
                    Text = name,
                    FontSize = 14,
                    FontType = FontType.Bold,
                    Color = 0xFCDF00, 
                    OutlineColor = 0x000000,
                    OutlineThickness = 0.5f 
                });
                
                _nameText.SetAnchor(UiAnchor.MiddleBottom);
                AddChild(_nameText);
            }
            if(_drawHpBar){
                _hpBg = new HpBarBg(40, 6);
                _hpBg.SetColor(0x111111u);

                _hpFg = new ColorRect(new ColorRectConfig { Color = 0x10FF00, Width = 40, Height = 6 });

                AddChild(_hpBg);
                _hpBg.AddChild(_hpFg);   
            }

            AddEventListener(Event.EnterFrame, OnFrameEnter);
        }

       private void OnFrameEnter() 
        {
            if (_owner == null || !Map.Entities.ContainsKey(_owner.ObjectId)) 
            {
                Parent?.RemoveChild(this);
                return;
            }

            var matrix = Camera.WorldMatrix * Camera.ViewMatrix * Camera.ProjectionMatrix;
            var worldPos = new Vector3(_owner.Position.X, _owner.Position.Y, _owner.Z);
            var ndc = Vector3.TransformPerspective(worldPos, matrix);

            if (ndc.X < -1.5f || ndc.X > 1.5f || ndc.Y < -1.5f || ndc.Y > 1.5f) 
            {
                Visible = false;
                return;
            }
            Visible = true;

            float screenX = ((ndc.X + 1.0f) / 2.0f) * Settings.ScreenWidth;
            float screenY = ((1.0f - ndc.Y) / 2.0f) * Settings.ScreenHeight;

            float verticalOffset = 0;
            if (_drawName) {
                verticalOffset += 17;
            }
            if (_drawHpBar) {
                verticalOffset += 5;
            }

            verticalOffset *= Settings.CameraZoom;

            X = (int)MathF.Round(screenX);
            Y = (int)MathF.Round(screenY + verticalOffset);
            Scale = Vector2.One;

            if (_drawName) {
                _nameText.Scale = new Vector2(Settings.CameraZoom);
                _nameText.Y = (int)MathF.Round(-2 * Settings.CameraZoom);   
            }

            if (_hpBg != null && _hpFg != null) 
            {
                if (_owner.MaxHp > 0 && !_owner.HasConditionEffect(ConditionEffects.Invincible)) 
                {
                    float fill = Math.Clamp((float)_owner.Hp / _owner.MaxHp, 0f, 1f);
                    int barW = (int)MathF.Round(40 * Settings.CameraZoom);
                    int barH = (int)MathF.Round(6 * Settings.CameraZoom);
                    int fgW = (int)MathF.Round(40 * fill * Settings.CameraZoom);

                    _hpBg.Resize(barW, barH);
                    _hpBg.X = -barW / 2;
                    _hpBg.Y = (int)MathF.Round(2 * Settings.CameraZoom);

                    _hpFg.Resize(fgW, barH); 
                    _hpFg.X = 0;
                    _hpFg.Y = 0;
                    
                    _hpFg.SetColor(fill < 0.5f ? (fill >= 0.2f ? 0xFF8010u : 0xE01010u) : 0x10FF00u);
                    
                    _hpBg.Visible = true;
                    _hpFg.Visible = true;
                } 
                else 
                {
                    _hpBg.Visible = false;
                    _hpFg.Visible = false;
                }
            }
        }
    }

    public sealed class HpBarBg : Sprite 
    {
        private int _w;
        private int _h;

        public HpBarBg(int w, int h) 
        {
            TextureId = (TextureType)10;
            VertexData = new VertexUi[4];
            Indices = new ushort[] { 0, 1, 2, 0, 2, 3 };
            Resize(w, h);
        }

        public void Resize(int width, int height) 
        {
            _w = width;
            _h = height;
            
            float p = 8f; 
            
            VertexData[0] = new VertexUi(new Vector2(-p, -p), new Vector2(0, 0));
            VertexData[1] = new VertexUi(new Vector2(_w + p, -p), new Vector2(1, 0));
            VertexData[2] = new VertexUi(new Vector2(_w + p, _h + p), new Vector2(1, 1));
            VertexData[3] = new VertexUi(new Vector2(-p, _h + p), new Vector2(0, 1));
            
            Extra1 = new Vector4(_w, _h, p, 0);
            SetGraphicsBuffer();
        }
    }
}