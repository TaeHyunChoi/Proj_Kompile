namespace Script.Global.Utility
{
    using System.Collections.Generic;
    using System.Text;
    
    /// <summary>
    /// RFC 4180 표준을 준수하여 쉼표, 줄바꿈, 큰따옴표가 포함된 CSV 텍스트를 파싱하는 순수 유틸리티.
    /// </summary>
    public static class CsvParserUtil
    {
        public static List<string[]> Parse(string csvText)
        {
            var lines = new List<string[]>();
            var currentLine = new List<string>();
            var currentField = new StringBuilder();
            
            bool inQuotes = false;

            for (int i = 0; i < csvText.Length; i++)
            {
                char c = csvText[i];

                if (inQuotes)
                {
                    // 따옴표 안에 있을 때
                    if (c == '"')
                    {
                        // 연속된 따옴표("")는 하나의 따옴표(")문자로 이스케이프 처리됨
                        if (i + 1 < csvText.Length && csvText[i + 1] == '"')
                        {
                            currentField.Append('"');
                            i++; // 다음 따옴표 건너뛰기
                        }
                        else
                        {
                            // 따옴표가 닫힘
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        currentField.Append(c);
                    }
                }
                else
                {
                    // 따옴표 밖에 있을 때
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        // 쉼표를 만나면 필드 종료
                        currentLine.Add(currentField.ToString());
                        currentField.Clear();
                    }
                    else if (c == '\n')
                    {
                        // 줄바꿈을 만나면 라인 종료
                        currentLine.Add(currentField.ToString());
                        lines.Add(currentLine.ToArray());
                        currentLine.Clear();
                        currentField.Clear();
                    }
                    else if (c != '\r') // 캐리지 리턴(\r)은 무시
                    {
                        currentField.Append(c);
                    }
                }
            }

            // 마지막 라인이 줄바꿈으로 끝나지 않은 경우 처리
            if (currentField.Length > 0 || currentLine.Count > 0)
            {
                currentLine.Add(currentField.ToString());
                lines.Add(currentLine.ToArray());
            }

            return lines;
        }
    }
}