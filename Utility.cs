using System;
using System.Collections.Generic;
using System.Text;

namespace Generator
{
    public static class Utility
    {
        public static string EncodeASCII(params byte[] bytes)
        {
            return ASCIIEncoding.ASCII.GetString(bytes);
        }

        public static string[] Split(string s, string seperator)
        {
            return s.Split(new string[] { seperator }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
