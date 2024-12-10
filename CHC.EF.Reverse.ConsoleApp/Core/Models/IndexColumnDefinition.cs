namespace CHC.EF.Reverse.ConsoleApp.Core.Models
{
    public class IndexColumnDefinition
    {
        public string ColumnName { get; set; }
        public bool IsDescending { get; set; }
        public int KeyOrdinal { get; set; }
        public bool IsIncluded { get; set; }
    }
}
