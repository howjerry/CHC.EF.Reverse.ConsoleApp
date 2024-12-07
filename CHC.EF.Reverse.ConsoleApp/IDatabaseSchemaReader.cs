using System.Collections.Generic;

namespace CHC.EF.Reverse.ConsoleApp
{
    public interface IDatabaseSchemaReader
    {
        List<TableDefinition> ReadTables();
    }
}