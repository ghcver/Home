using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication17
{
    static class Extension
    {
        public static bool SameWith(this Color source, KnownColor target)
        {
            return source.ToArgb() == Color.FromName(target.ToString()).ToArgb();
        }
    }
}
