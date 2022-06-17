using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToJww
{
    public static class UnicodeHelper
    {

        /// <summary>
        /// 康煕部首などをCJK統合漢字など通常（？）の漢字に変換する
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string UnifiedKanjiConverter(string text)
        {
            if (mEquivalentUnifiedIdeographMap == null)
            {
                using var reader = new StringReader(Properties.Resources.EquivalentUnifiedIdeograph);
                mEquivalentUnifiedIdeographMap = new();
                ParseEquivalentUnifiedIdeographFile(mEquivalentUnifiedIdeographMap, reader);
            }
            var sb = new StringBuilder();
            foreach (var c in text)
            {
                sb.Append((Char)mEquivalentUnifiedIdeographMap.GetValueOrDefault(c, c));
            }
            return sb.ToString();
        }
        static Dictionary<int, int>? mEquivalentUnifiedIdeographMap = null;

        static void ParseEquivalentUnifiedIdeographFile(Dictionary<int, int> codeMap, TextReader reader)
        {
//            Dictionary<int, int> conv = new();
            var tokens = new List<string>();
            var sb = new StringBuilder();
            while (true)
            {
                var line = reader.ReadLine();
                if (line == null) break;
                tokens.Clear();
                sb.Clear();
                foreach (var c in line)
                {
                    if (c == '#')
                    {
                        AddToken(tokens, sb);
                        break;
                    }
                    if (Char.IsWhiteSpace(c) || c == '.' || c == ';')
                    {
                        AddToken(tokens, sb);
                        continue;
                    }
                    sb.Append(c);
                }
                AddToken(tokens, sb);
                if (tokens.Count == 2)
                {
                    var from = Convert.ToInt32(tokens[0], 16);
                    var to = Convert.ToInt32(tokens[1], 16);
                    codeMap[from] = to;
                }else if (tokens.Count == 3)
                {
                    var from1 = Convert.ToInt32(tokens[0], 16);
                    var from2 = Convert.ToInt32(tokens[1], 16);
                    var to = Convert.ToInt32(tokens[2], 16);
                    for (var from = from1; from <= from2; from++)
                    {
                        codeMap[from] = to++;
                    }
                }
            }
//            return conv;
        }

        static void AddToken(List<string> tokens, StringBuilder sb)
        {
            if (sb.Length > 0)
            {
                tokens.Add(sb.ToString());
                sb.Clear();
            }
        }

    }
}
