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
        string text;
        if (desc.TributePoints > 0)
        {
            if (desc.ST)
            {
                c = new Color(0xFF9900);
                text = "ST";
            }
            else if (desc.UnHoly)
            {
                c = new Color(0x9E9859);
                text = "UH";
            }
            else if (desc.Div)
            {
                c = new Color(0xFCE53D);
                text = "DIV";
            }
            else if (desc.Ferozium)
            {
                c = new Color(0x52DCFF);
                text = "FT" + desc.Tier;
            }
            else
            {
                text = "UT";

                if (desc.TributePoints > 749999) c = new Color(0x00D287);
                else if (desc.TributePoints > 99999) c = new Color(0xB2001D);
                else c = new Color(9055202);
            }
        }
        else
        {
            if (desc.Tier == -1)
            {
                text = "UT";
                c = new Color(9055202);
            }
            else
            {
                text = "T" + desc.Tier;
            }
        }
        
        Tag = new SimpleText(new TextConfig()
        {
            FontSize = 16,
            FontType = FontType.Bold,
            Text = text,
            OutlineColor = 0,
            OutlineThickness = 0.5f
        });
        Tag.Color = c;
        AddChild(Tag);
    }
}