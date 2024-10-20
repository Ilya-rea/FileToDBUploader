using System;
using System.Data.SqlClient;

namespace FileToDbUploader
{
    class Program
    {
        static void Main(string[] args)
        {
           

            string filePath = args[0];
            string connectionString = "Server=localhost;Database=WordDB;User Id=sa;Password=ILYa2001!;";
            InitializeDatabase(connectionString);
            var wordLoader = new WordLoader(connectionString);
            wordLoader.LoadWords(filePath);
            Console.WriteLine("Слова загружены.");
        }

        private static void InitializeDatabase(string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var createTableCommand = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Words' AND xtype='U')
                CREATE TABLE Words (
                    Text NVARCHAR(20) PRIMARY KEY,
                    Count INT NOT NULL
                )";

                using (var command = new SqlCommand(createTableCommand, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
