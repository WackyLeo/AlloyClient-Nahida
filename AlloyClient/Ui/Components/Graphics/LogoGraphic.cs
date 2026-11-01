using System;
using AlloyClient.Engine.Common;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;
using AlloyClient.UiLib.Rendering;
using OpenTK.Mathematics;

namespace AlloyClient.Ui.Components.Graphics;

public sealed class LogoGraphic : Sprite {
    private const int TexWidth = 1280; // Make sure these match your raw logo image size!
    private const int TexHeight = 720;

    public LogoGraphic() {
        TextureId = TextureType.TitleLogo;

        VertexData = new VertexUi[4];
        Indices = [0, 1, 2, 0, 2, 3];
        
        // Fill the exact native resolution once. No internal resizing.
        FillData(TexWidth, TexHeight);
        
        // CRITICAL FIX: Tells the engine the vertices are ready to render
        // This sets _noRenderData = false and calculates ContentWidth/Height
        SetGraphicsBuffer(); 
    }

    private void FillData(int width, int height) {
        VertexData[0] = new VertexUi(new Vector2(0, 0), new Color(1f, 1f, 1f));
        VertexData[1] = new VertexUi(new Vector2(width, 0), new Color(1f, 1f, 1f));
        VertexData[2] = new VertexUi(new Vector2(width, height), new Color(1f, 1f, 1f));
        VertexData[3] = new VertexUi(new Vector2(0, height), new Color(1f, 1f, 1f));
    }
}