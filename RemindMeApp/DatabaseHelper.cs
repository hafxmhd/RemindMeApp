
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;

namespace RemindMeApp
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "RemindMeApp");

            Directory.CreateDirectory(folder);

            string databasePath = Path.Combine(folder, "remindme.db");

            _connectionString =
                $"Data Source={databasePath}";

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Tasks
                (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Description TEXT,
                    DueAt TEXT NOT NULL,
                    IsCompleted INTEGER NOT NULL DEFAULT 0
                );";

            command.ExecuteNonQuery();
        }

        public int SaveTask(ReminderTask task)
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO Tasks
                    (Title, Description, DueAt, IsCompleted)
                VALUES
                    ($title, $description, $dueAt, 0);

                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue(
                "$title", task.Title);

            command.Parameters.AddWithValue(
                "$description", task.Description);

            command.Parameters.AddWithValue(
                "$dueAt", task.DueAt.ToString(
                    "O", CultureInfo.InvariantCulture));

            task.Id = Convert.ToInt32(
                command.ExecuteScalar());

            return task.Id;
        }

        public List<ReminderTask> GetTasks()
        {
            var tasks = new List<ReminderTask>();

            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, Title, Description, DueAt, IsCompleted
                FROM Tasks
                ORDER BY DueAt;";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                tasks.Add(new ReminderTask
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.IsDBNull(2)
                        ? string.Empty
                        : reader.GetString(2),
                    DueAt = DateTime.Parse(
                        reader.GetString(3),
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind),
                    IsCompleted = reader.GetInt32(4) == 1
                });
            }

            return tasks;
        }


        public void MarkTaskCompleted(int taskId)
        {
            using var connection =
                new Microsoft.Data.Sqlite.SqliteConnection(
                    _connectionString);

            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText =
                "UPDATE Tasks SET IsCompleted = 1 WHERE Id = $id";

            command.Parameters.AddWithValue("$id", taskId);
            command.ExecuteNonQuery();
        }

        public void UpdateTaskDueAt(int taskId, DateTime dueAt)
        {
            using var connection =
                new Microsoft.Data.Sqlite.SqliteConnection(
                    _connectionString);

            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText =
                "UPDATE Tasks SET DueAt = $dueAt WHERE Id = $id";

            command.Parameters.AddWithValue(
                "$dueAt", dueAt.ToString(
                    "O", System.Globalization.CultureInfo.InvariantCulture));

            command.Parameters.AddWithValue("$id", taskId);
            command.ExecuteNonQuery();
        }

    }
}
