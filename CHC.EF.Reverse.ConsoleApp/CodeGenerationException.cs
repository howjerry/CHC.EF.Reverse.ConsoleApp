﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHC.EF.Reverse.ConsoleApp
{
    public class CodeGenerationException : Exception
    {
        public CodeGenerationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}