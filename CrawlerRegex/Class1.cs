using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Reflection;

namespace CrawlerRegex {
    public class Class1 {
        public static void Main() {
            RegexCompilationInfo expr;
            List<RegexCompilationInfo> compilationList = new List<RegexCompilationInfo>();

            // Define regular expression to detect duplicate words
            expr = new RegexCompilationInfo(@"</span>1 &ndash; 0",
                       RegexOptions.CultureInvariant,
                       "DeadLink",
                       "Utilities.RegularExpressions",
                       true);
            // Add info object to list of objects
            compilationList.Add(expr);

            // Generate assembly with compiled regular expressions
            RegexCompilationInfo[] compilationArray = new RegexCompilationInfo[compilationList.Count];
            AssemblyName assemName = new AssemblyName("RegexLib, Version=1.0.0.1001, Culture=neutral, PublicKeyToken=null");
            compilationList.CopyTo(compilationArray);
            Regex.CompileToAssembly(compilationArray, assemName);
        }
    }
}
