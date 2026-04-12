namespace Script.Asset.Data
{
    using System;

    /// <summary>
    /// [Framework] Data 계층 (Define)
    /// 문자열 기반의 에셋 키를 래핑하여 타입 안정성과 해시 기반의 빠른 비교(O(1))를 제공하는 불변 구조체입니다.
    /// Enum을 대체하여 데이터 주도(Data-Driven) 로드 방식에 사용됩니다.
    /// </summary>
    public readonly struct AssetKey : IEquatable<AssetKey>
    {
        public readonly string Value;
        private readonly int _hashCode;

        public AssetKey(string value)
        {
            Value = value;
            // 문자열 캐싱 및 빠른 비교를 위해 해시코드를 미리 계산하여 보관합니다.
            _hashCode = string.IsNullOrEmpty(value) ? 0 : value.GetHashCode();
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        // IEquatable 구현으로 컬렉션에서의 박싱(Boxing) 방지 및 비교 속도 최적화
        public bool Equals(AssetKey other) => _hashCode == other._hashCode && Value == other.Value;
        public override bool Equals(object obj) => obj is AssetKey other && Equals(other);
        public override int GetHashCode() => _hashCode;

        public static bool operator ==(AssetKey left, AssetKey right) => left.Equals(right);
        public static bool operator !=(AssetKey left, AssetKey right) => !left.Equals(right);

        // string과의 자연스러운 호환성을 위한 암시적 형변환 지원
        public static implicit operator string(AssetKey key) => key.Value;
        public static implicit operator AssetKey(string value) => new AssetKey(value);

        public override string ToString() => Value ?? "None";
    }

}