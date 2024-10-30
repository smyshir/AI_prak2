﻿using CsvHelper.Configuration;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SecondTaskAI
{
    internal static class CsvHelper
    {
        internal const char delimiter = ',';
        static private CsvConfiguration config = new CsvConfiguration(CultureInfo.CurrentCulture) {
            Delimiter = $"{delimiter}", Encoding = Encoding.UTF8 
        };
        public static void SetCsvConfiguration(CsvConfiguration c) => config = c;
        internal static List<string> ReadCsvFile(string path, string columnName)
        {
            if(!File.Exists(path)) 
            {
                File.Create(path);
                return null;
            }
            List<string> result = new List<string>();

            using (var fileReader = File.OpenText(path))
            using (var csvResult = new global::CsvHelper.CsvReader(fileReader, config))
            {
                csvResult.Read();
                csvResult.ReadHeader();
                while (csvResult.Read())
                    result.Add(csvResult.GetField<string>(columnName));
            }
            return result;
        }
    }
}
