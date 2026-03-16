using System;
using AlloyClient.Assets;
using AlloyClient.Assets.Libraries;
using AlloyClient.Game.Objects.Enums;
using AlloyClient.Networking.Enums;
using AlloyClient.Networking.Structs.DataObjects;
using AlloyClient.Rendering;
using AlloyClient.Rendering.Types;
using AlloyClient.Utils;
using Common.Structs;

namespace AlloyClient.Game.Objects;

public class Merchant : SellableObject {
    public ushort MerchandiseType = 0;
    
    protected override RenderBase GetRenderType(ushort type) {
        ObjectLibrary.TypeToObjectProps.TryGetValue(type, out var props);
        if (props == null) {
            return null;
        }

        if (props.RealSize != -1) {
            Size = props.RealSize;
        }

        if (props.MinSize != props.MaxSize) {
            var maxSteps = (props.MaxSize - props.MinSize) / props.SizeStep;
            Size = props.MinSize + (int) (Random.Shared.NextSingle() * maxSteps) * props.SizeStep;
        }

        return new TypeGameObject(this);
    }
    
    public override bool Update(double time, double dt) {
        return true;
    }

    public override void UpdateStats(StatData[] statData, int offset, int count) {
        base.UpdateStats(statData, offset, count);

        #region Parse StatData

        for (var i = 0; i < count; i++) {
            var stat = statData[offset + i];
            switch (stat.Type) {
                case StatsType.MerchantMerchandiseType:
                    MerchandiseType = (ushort)stat.Value;
                    if (stat.Value <= 0) {
                        return;
                    }
                    
                    if (!ObjectLibrary.TypeToTextureData.TryGetValue(MerchandiseType, out var textureData)) {
                        textureData = ObjectLibrary.TypeToTextureData[ObjectLibrary.FallbackType];
                    }
                    
                    TextureData = textureData;
                    Texture = TextureData.Texture;
                    RenderBaseType?.SetTexture(Texture);
                    break;
            }
        }

        #endregion

    }

    // we'll override these eventually
    public void SetPrice(int a_price) {
        price = a_price;
    }
    
    public void SetCurrency(int a_currency) {
        currency = a_currency;
    }
    
    public void SetRankReq(int a_rankReq) {
        rankReq = a_rankReq;
    }
    
    public string SoldObjectName() {
        return new string("");
    }
    
    public string SoldObjectInternalName() {
        return new string("");
    }
    
    public void GetTooltip() {
        return;
    }
    
    public void GetIcon() {
        return;
    }
    
    public void GetSellableType() {
        return;
    }
    
    public int GetQuantity() {
        return quanitity;
    }
}