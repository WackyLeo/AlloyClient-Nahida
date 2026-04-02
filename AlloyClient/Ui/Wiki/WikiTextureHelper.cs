using AlloyClient.Assets.Libraries;
using AlloyClient.UiLib.Data;
using AlloyClient.UiLib.Enums;

namespace AlloyClient.Utils;

public static class WikiTextureHelper {
    public static TextureInfo ResolveStaticFrame(ushort objectType) {
        if (!ObjectLibrary.TypeToTextureData.TryGetValue(objectType, out var td))
            return TextureHelper.FromGameAtlas("invisible", 0);
        if (td.Texture.W > 0 && td.Texture.H > 0)
            return new TextureInfo(td.Texture.ToPosition(), TextureType.GameAtlas);
        if (td.HasAnimationData && td.AnimatedTextures.FaceRight?.Length > 0)
            return new TextureInfo(td.AnimatedTextures.FaceRight[0].ToPosition(), TextureType.GameAtlas);
        if (td.RandomTextures?.Length > 0 && td.RandomTextures[0].Texture.W > 0)
            return new TextureInfo(td.RandomTextures[0].Texture.ToPosition(), TextureType.GameAtlas);
        return TextureHelper.FromGameAtlas("invisible", 0);
    }
}