﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHC.EF.Reverse.ConsoleApp
{

    public class ManyToManyRelationship
    {
        public string JunctionTable { get; set; }
        public string RelatedTable { get; set; }
    }
}