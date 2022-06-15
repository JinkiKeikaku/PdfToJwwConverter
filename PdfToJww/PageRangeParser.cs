using System.Text;

namespace PdfToJww
{
    public static class PageRangeParser
    {
        public static List<int> ParsePageRange(string script, int numPage)
        {
            var stack = new List<string>();
            var pages = new HashSet<int>();
            var tokens = GetPageRangeTokens(script);
            foreach (var token in tokens)
            {
                if(token == ",")
                {
                    EvalStack(stack, pages, numPage);
                    stack.Clear();
                }
                else
                {
                    stack.Add(token);
                }
            }
            if(stack.Count > 0) EvalStack(stack, pages, numPage);
            var ret = pages.ToList();
            ret.Sort();
            return ret;
        }

        static void EvalStack(List<string> stack, HashSet<int> pages, int numPage)
        {
            if (stack.Count == 0) throw new Exception("There is no page number.");
            if (stack.Count == 1) {
                if(!int.TryParse(stack[0], out var a)) throw new Exception("The page number syntax is invalid.");
                if(a > numPage) throw new Exception("The page number exceeds max pages.");
                pages.Add(a);
                return;
            }
            if (stack.Count != 3) throw new Exception("The page number syntax is invalid.");
            if (!int.TryParse(stack[0], out var start)) throw new Exception("The page number syntax is invalid.");
            if (stack[1] != "-") throw new Exception("The page number syntax is invalid.");
            if (!int.TryParse(stack[2], out var end)) throw new Exception("The page number syntax is invalid.");
            if(start > end) throw new Exception("The page number syntax is invalid.");
            if (end > numPage) throw new Exception("The page number exceeds max pages.");
            for (var i = start; i <= end; i++)
            {
                pages.Add(i);
            }
        }



        static List<string> GetPageRangeTokens(string script)
        {
            void AddToken(List<string> tokens, StringBuilder sb)
            {
                if (sb.Length > 0)
                {
                    tokens.Add(sb.ToString());
                    sb.Clear();
                }
            }
            var tokens = new List<string>();
            var sb = new StringBuilder();

            foreach (var c in script)
            {
                switch (c)
                {
                    case ',':
                    case '-':
                        {
                            AddToken(tokens, sb);
                            tokens.Add(c.ToString());
                            continue;
                        }
                }
                if (Char.IsWhiteSpace(c))
                {
                    AddToken(tokens, sb);
                    continue;
                }
                sb.Append(c);
            }
            AddToken(tokens, sb);
            return tokens;
        }

    }
}
