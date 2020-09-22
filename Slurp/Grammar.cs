using System;
using System.Collections.Generic;
using System.Text;

namespace Slurp
{
    
    public class Grammar<Context>
    {
        /*
         */
        public Terminal Char(char ch) => ch;
        public Terminal String(string str) => str;

        public Symbol<Result, Context> Symbol<Result>() => new Symbol<Result, Context>();
        public Symbol<Result, Context> Symbol<Result>(string name) => new Symbol<Result, Context>(name);
    }
}
