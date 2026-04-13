namespace Kompile.Asset.Data
{
    using MessagePack;
    using Unity.Collections;

    /// <summary> 
    /// UI 문구 및 대사 데이터를 보관하는 순수 데이터 구조체.
    /// 메모리 파편화 방지를 위해 struct로 정의하며, 런타임 문자열은 string을 사용합니다.
    /// </summary>
    [System.Serializable]
    public struct LocalizationTableData
    {
        // 1. 기본 식별자
        public int ID;

        // 2. 기획용 식별 키 (예: "LOBBY_WELCOME_MSG")
        // FixedString을 사용하여 키 조회 시의 GC 할당을 억제합니다.
        public FixedString32Bytes Key;

        // 3. 실제 번역 데이터
        // 쉼표나 줄바꿈이 포함되어도 CsvParserUtil을 통해 안전하게 주입됩니다.
        public string KR;
        public string EN;

        // 조회용 편의 프로퍼티 (직렬화 제외)
        [IgnoreMember]
        public string KeyString => Key.ToString();
    }
}