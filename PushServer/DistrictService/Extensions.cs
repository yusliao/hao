using System;
using System.Linq;
namespace DistrictService
{
    public static class Extensions
    {
        public static string RemoveInnerBlankSpace(this string str)
        {
            return str.Replace(" ", string.Empty);
        }
        public static string RemoveWordsAtBegin(this string str, string[] words)
        {
            bool flag = words == null || !words.Any<string>();
            string result;
            if (flag)
            {
                result = str;
            }
            else
            {
                for (int i = 0; i < words.Length; i++)
                {
                    string text = words[i];
                    bool flag2 = str.IndexOf(text) == 0;
                    if (flag2)
                    {
                        str = str.Substring(text.Length).Trim();
                    }
                }
                result = str;
            }
            return result;
        }
    }
}
