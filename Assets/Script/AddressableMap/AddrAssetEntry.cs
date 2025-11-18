namespace AddressasbleAsset
{
    using System;

    [Serializable]
    public struct AddrAssetEntry<TEnum> where TEnum : Enum
    {
        public TEnum ID;
        public string AddressKey;
    }
}
