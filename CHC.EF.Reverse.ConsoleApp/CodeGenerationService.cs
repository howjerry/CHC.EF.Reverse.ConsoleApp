using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace CHC.EF.Reverse.ConsoleApp
{
    public class CodeGenerationService
    {
        private readonly Settings _settings;
        private readonly Logger _logger;
        private readonly DatabaseSchemaReaderFactory _schemaReaderFactory;

        public CodeGenerationService(IOptions<Settings> settings, Logger logger, DatabaseSchemaReaderFactory schemaReaderFactory)
        {
            _settings = settings.Value;
            _logger = logger;
            _schemaReaderFactory = schemaReaderFactory;
        }

        public async Task Run()
        {
            try
            {
                var schemaReader = _schemaReaderFactory.Create();
                var tables = schemaReader.ReadTables();

                _logger.Info($"讀取到 {tables.Count} 個資料表。");

                var generator = new EntityGenerator(_settings, _logger);
                await generator.GenerateAsync(tables);

                _logger.Info("程式碼產生完成。");
            }
            catch (Exception ex)
            {
                _logger.Error("產生程式碼失敗", ex);
            }
        }
    }
}