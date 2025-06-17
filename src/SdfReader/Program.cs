using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using Newtonsoft.Json;

namespace SdfReader
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: SdfReader.exe <path-to-sdf-file>");
                Environment.Exit(1);
                return;
            }

            string sdfPath = args[0];
            
            if (!File.Exists(sdfPath))
            {
                Console.WriteLine($"Error: File not found: {sdfPath}");
                Environment.Exit(1);
                return;
            }

            try
            {
                var result = new Dictionary<string, object>();
                
                string connectionString = $"Data Source={sdfPath};";
                
                using (var connection = new SqlCeConnection(connectionString))
                {
                    connection.Open();
                    
                    // Get all table names
                    var tableNames = GetTableNames(connection);
                    
                    foreach (string tableName in tableNames)
                    {
                        var tableData = GetTableData(connection, tableName);
                        result[tableName] = tableData;
                    }
                }
                
                // Output JSON to console
                string json = JsonConvert.SerializeObject(result, Formatting.Indented);
                Console.WriteLine(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }
        
        static List<string> GetTableNames(SqlCeConnection connection)
        {
            var tableNames = new List<string>();
            
            using (var command = new SqlCeCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'TABLE'", connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    tableNames.Add(reader.GetString(0));
                }
            }
            
            return tableNames;
        }
        
        static List<Dictionary<string, object>> GetTableData(SqlCeConnection connection, string tableName)
        {
            var rows = new List<Dictionary<string, object>>();
            
            using (var command = new SqlCeCommand($"SELECT * FROM [{tableName}]", connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var row = new Dictionary<string, object>();
                    
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string columnName = reader.GetName(i);
                        object value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        row[columnName] = value;
                    }
                    
                    rows.Add(row);
                }
            }
            
            return rows;
        }
    }
}