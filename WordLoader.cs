using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace FileToDbUploader
{
    public class WordLoader
    {
        private readonly string _connectionString;

        public WordLoader(string connectionString)
        {
            _connectionString = connectionString;
            EnsureDatabase();
        }

        public void LoadWords(string filePath)
        {
            var words = File.ReadLines(filePath)
                .SelectMany(line => line.ToLower().Split(new[] { ' ', '\r', '\n', '\t', ',', '.', '!', '?', ';', ':' }, StringSplitOptions.RemoveEmptyEntries))
                .Where(word => word.Length >= 3 && word.Length <= 20)
                .GroupBy(word => word)
                .Where(group => group.Count() >= 4)
                .ToDictionary(group => group.Key, group => group.Count());

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                foreach (var wordCount in words)
                {
                    UpdateWords(connection, wordCount.Key, wordCount.Value);
                }
            }

        }
        private void UpdateWords(SqlConnection connection, string word, int count)
        {
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    int existingCount = WordCount(connection, word, transaction);

                    if (existingCount > 0)
                    {
                        UpdateWordCount(connection, word, existingCount + count, transaction);
                    }
                    else
                    {
                        InsertNewWord(connection, word, count, transaction);
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
        private int WordCount(SqlConnection connection, string word, SqlTransaction transaction)
        {
            using (var command = new SqlCommand("SELECT Count FROM Words WHERE Word = @Word", connection, transaction))
            {
                command.Parameters.AddWithValue("@Word", word);
                var result = command.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }
        private void UpdateWordCount(SqlConnection connection, string word, int newCount, SqlTransaction transaction)
        {
            using (var command = new SqlCommand("UPDATE Words SET Count = @Count WHERE Word = @Word", connection, transaction))
            {
                command.Parameters.AddWithValue("@Count", newCount);
                command.Parameters.AddWithValue("@Word", word);
                command.ExecuteNonQuery();
            }
        }
        private void InsertNewWord(SqlConnection connection, string word, int count, SqlTransaction transaction)
        {
            using (var command = new SqlCommand("INSERT INTO Words (Word, Count) VALUES (@Word, @Count)", connection, transaction))
            {
                command.Parameters.AddWithValue("@Word", word);
                command.Parameters.AddWithValue("@Count", count);
                command.ExecuteNonQuery();
            }
        }
        private void EnsureDatabase()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand(
                    @"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Words' AND xtype='U')
                      CREATE TABLE Words (
                          Id INT IDENTITY(1,1) PRIMARY KEY,
                          Word NVARCHAR(100) NOT NULL,
                          Count INT NOT NULL
                      );", connection);
                command.ExecuteNonQuery();
            }
        }
    }
}
