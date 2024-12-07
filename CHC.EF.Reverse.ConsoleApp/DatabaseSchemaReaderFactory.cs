using System;
using Microsoft.Extensions.Options;

namespace CHC.EF.Reverse.ConsoleApp
{
    public class DatabaseSchemaReaderFactory
    {
        private readonly Settings _settings;

        public DatabaseSchemaReaderFactory(IOptions<Settings> settings)
        {
            _settings = settings.Value;
        }

        public IDatabaseSchemaReader Create()
        {
            return _settings.ProviderName switch
            {
                "MySql.Data.MySqlClient" => new MySqlSchemaReader(_settings.ConnectionString),
                "Microsoft.Data.SqlClient" => new SqlServerSchemaReader(_settings.ConnectionString),
                _ => throw new NotSupportedException($"不支援的 Provider: {_settings.ProviderName}")
            };
        }
    }
}