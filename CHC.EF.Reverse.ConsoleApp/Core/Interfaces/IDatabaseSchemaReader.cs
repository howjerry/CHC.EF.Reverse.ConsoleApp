using CHC.EF.Reverse.ConsoleApp.Core.Models;
using System.Collections.Generic;

namespace CHC.EF.Reverse.ConsoleApp.Core.Interfaces
{
    public interface IDatabaseSchemaReader
    {
        List<TableDefinition> ReadTables();
    }
}