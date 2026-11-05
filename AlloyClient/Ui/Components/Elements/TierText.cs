using System;
using AlloyClient.Assets.XmlStructs;
using AlloyClient.Engine.Common;
using AlloyClient.UiLib.BuiltIn;
using AlloyClient.UiLib.Core;
using AlloyClient.UiLib.Enums;

namespace AlloyClient.Ui.Components.Elements;

public class TierText : Sprite
{
    private SimpleText Tag;
    public TierText(ItemDesc desc)
    {
        var c = Color.White;
        var effect = TextEffect.None;
        string text;
        
        if (desc.ST)
        {
            c = Color.FromHexRGB(0xFF9900);
            text = "ST";
        }
        else if (desc.UnHoly)
        {
            c = Color.FromHexRGB(0x9E9859);
            text = "UH";
        }
        else if (desc.Ferozium) {
            c = Color.FromHexRGB(0x52DCFF);
            text = "FT" + desc.Tier;
            effect = TextEffect.Void;
        }
        else if (desc.Tier == -1)
        {
            text = "UT";
            c = Color.FromHexRGB(9055202);
        }
        else
        {
            text = "T" + desc.Tier;
        }
        
        Tag = new SimpleText(new TextConfig()
        {
            FontSize = 16,
            FontType = FontType.Bold,
            Text = text,
            OutlineColor = 0,
            OutlineThickness = 0.5f,
            ShaderEffect = effect
        });
        Tag.Color = c;
        
        AddChild(Tag);
    }
}