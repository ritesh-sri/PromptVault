using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using PromptVault.Models;

namespace PromptVault.Services
{
    public class DatabaseService
    {
        private readonly string connectionString;
        private readonly string dbPath;

        public DatabaseService()
        {
            // Store database in user's AppData folder
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PromptVault"
            );

            if (!Directory.Exists(appDataPath))
                Directory.CreateDirectory(appDataPath);

            dbPath = Path.Combine(appDataPath, "prompts.db");
            connectionString = $"Data Source={dbPath};Version=3;";

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Create Prompts table
                string createPromptsTable = @"
                    CREATE TABLE IF NOT EXISTS Prompts (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Content TEXT NOT NULL,
                        AIProvider TEXT NOT NULL,
                        ModelVersion TEXT NOT NULL,
                        IsFavorite INTEGER DEFAULT 0,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL,
                        UsageCount INTEGER DEFAULT 0
                    )";

                // Create Tags table
                string createTagsTable = @"
                    CREATE TABLE IF NOT EXISTS Tags (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT UNIQUE NOT NULL,
                        Color TEXT NOT NULL
                    )";

                // Create PromptTags junction table
                string createPromptTagsTable = @"
                    CREATE TABLE IF NOT EXISTS PromptTags (
                        PromptId INTEGER NOT NULL,
                        TagId INTEGER NOT NULL,
                        PRIMARY KEY (PromptId, TagId),
                        FOREIGN KEY (PromptId) REFERENCES Prompts(Id) ON DELETE CASCADE,
                        FOREIGN KEY (TagId) REFERENCES Tags(Id) ON DELETE CASCADE
                    )";

                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = createPromptsTable;
                    command.ExecuteNonQuery();

                    command.CommandText = createTagsTable;
                    command.ExecuteNonQuery();

                    command.CommandText = createPromptTagsTable;
                    command.ExecuteNonQuery();
                }

                // Insert default tags if not exists
                InsertDefaultTags(connection);

                // Insert starter prompts if database is empty
                if (GetPromptCount(connection) == 0)
                {
                    InsertStarterPrompts(connection);
                }
            }
        }

        private void InsertDefaultTags(SQLiteConnection connection)
        {
            var defaultTags = Tag.GetDefaultTags();

            using (var command = new SQLiteCommand(connection))
            {
                foreach (var tag in defaultTags)
                {
                    command.CommandText = @"
                        INSERT OR IGNORE INTO Tags (Name, Color) 
                        VALUES (@name, @color)";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@name", tag.Name);
                    command.Parameters.AddWithValue("@color", tag.Color);
                    command.ExecuteNonQuery();
                }
            }
        }

        private int GetPromptCount(SQLiteConnection connection)
        {
            using (var command = new SQLiteCommand("SELECT COUNT(*) FROM Prompts", connection))
            {
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private void InsertStarterPrompts(SQLiteConnection connection)
        {
            var starterPrompts = new List<Prompt>
            {
                new Prompt
                {
                    Title = "Code Review Assistant",
                    Content = "Review the following code for:\n- Bugs and potential errors\n- Security vulnerabilities\n- Performance issues\n- Code style and best practices\n- Suggestions for improvement\n\nProvide detailed feedback with specific line numbers and explanations.",
                    AIProvider = AIProvider.Claude,
                    ModelVersion = ModelVersion.Claude35Sonnet,
                    Tags = new List<string> { "Coding", "Review" }
                },
                new Prompt
                {
                    Title = "Blog Post Generator",
                    Content = "Write a comprehensive blog post about [TOPIC]. Include:\n- Engaging introduction with hook\n- 5-7 main sections with subheadings\n- SEO-optimized content with keywords\n- Actionable takeaways\n- Compelling conclusion with CTA\n- Meta description (150-160 chars)",
                    AIProvider = AIProvider.ChatGPT,
                    ModelVersion = ModelVersion.GPT4,
                    Tags = new List<string> { "Writing", "SEO" }
                },
                new Prompt
                {
                    Title = "Data Analysis Expert",
                    Content = "Analyze the provided dataset and:\n1. Identify key patterns and trends\n2. Calculate relevant statistics\n3. Detect anomalies or outliers\n4. Provide actionable insights\n5. Suggest visualizations\n6. Recommend next steps\n\nPresent findings in a clear, executive-friendly format.",
                    AIProvider = AIProvider.ChatGPT,
                    ModelVersion = ModelVersion.GPT4,
                    Tags = new List<string> { "Analysis" }
                },
                new Prompt
                {
                    Title = "Professional Email Writer",
                    Content = "Compose a professional email with the following requirements:\n- Purpose: [STATE PURPOSE]\n- Recipient: [ROLE/NAME]\n- Tone: [Professional/Friendly/Formal]\n- Key points to include: [LIST POINTS]\n\nEnsure clarity, appropriate tone, and proper email etiquette.",
                    AIProvider = AIProvider.Claude,
                    ModelVersion = ModelVersion.Claude3Sonnet,
                    Tags = new List<string> { "Email", "Writing" }
                },
                new Prompt
                {
                    Title = "Bug Debugging Assistant",
                    Content = "Help debug this issue:\n\nError Message: [PASTE ERROR]\nCode Context: [PASTE CODE]\nExpected Behavior: [DESCRIBE]\nActual Behavior: [DESCRIBE]\n\nProvide:\n1. Root cause analysis\n2. Step-by-step solution\n3. Code fix with explanation\n4. Prevention tips",
                    AIProvider = AIProvider.ChatGPT,
                    ModelVersion = ModelVersion.GPT4,
                    Tags = new List<string> { "Coding", "Debug" }
                }
            };

            foreach (var prompt in starterPrompts)
            {
                AddPrompt(prompt, connection);
            }
        }

        public int AddPrompt(Prompt prompt, SQLiteConnection existingConnection = null)
        {
            bool shouldCloseConnection = existingConnection == null;
            var connection = existingConnection ?? new SQLiteConnection(connectionString);

            try
            {
                if (shouldCloseConnection)
                    connection.Open();

                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"
                        INSERT INTO Prompts (Title, Content, AIProvider, ModelVersion, 
                                            IsFavorite, CreatedAt, UpdatedAt, UsageCount)
                        VALUES (@title, @content, @provider, @version, 
                                @favorite, @created, @updated, @usage);
                        SELECT last_insert_rowid();";

                    command.Parameters.AddWithValue("@title", prompt.Title);
                    command.Parameters.AddWithValue("@content", prompt.Content);
                    command.Parameters.AddWithValue("@provider", prompt.AIProvider);
                    command.Parameters.AddWithValue("@version", prompt.ModelVersion);
                    command.Parameters.AddWithValue("@favorite", prompt.IsFavorite ? 1 : 0);
                    command.Parameters.AddWithValue("@created", prompt.CreatedAt.ToString("o"));
                    command.Parameters.AddWithValue("@updated", prompt.UpdatedAt.ToString("o"));
                    command.Parameters.AddWithValue("@usage", prompt.UsageCount);

                    int promptId = Convert.ToInt32(command.ExecuteScalar());

                    // Add tags
                    foreach (var tagName in prompt.Tags)
                    {
                        int tagId = GetOrCreateTag(tagName, connection);
                        LinkPromptToTag(promptId, tagId, connection);
                    }

                    return promptId;
                }
            }
            finally
            {
                if (shouldCloseConnection)
                    connection?.Close();
            }
        }

        private int GetOrCreateTag(string tagName, SQLiteConnection connection)
        {
            using (var command = new SQLiteCommand(connection))
            {
                // Try to get existing tag
                command.CommandText = "SELECT Id FROM Tags WHERE Name = @name";
                command.Parameters.AddWithValue("@name", tagName);
                var result = command.ExecuteScalar();

                if (result != null)
                    return Convert.ToInt32(result);

                // Create new tag with random color
                command.CommandText = @"
                    INSERT INTO Tags (Name, Color) VALUES (@name, @color);
                    SELECT last_insert_rowid();";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@name", tagName);
                command.Parameters.AddWithValue("@color", "#9E9E9E"); // Default gray
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private void LinkPromptToTag(int promptId, int tagId, SQLiteConnection connection)
        {
            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = @"
                    INSERT OR IGNORE INTO PromptTags (PromptId, TagId) 
                    VALUES (@promptId, @tagId)";
                command.Parameters.AddWithValue("@promptId", promptId);
                command.Parameters.AddWithValue("@tagId", tagId);
                command.ExecuteNonQuery();
            }
        }

        public List<Prompt> GetAllPrompts()
        {
            var prompts = new List<Prompt>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(@"
                    SELECT Id, Title, Content, AIProvider, ModelVersion, 
                           IsFavorite, CreatedAt, UpdatedAt, UsageCount
                    FROM Prompts
                    ORDER BY UpdatedAt DESC", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var prompt = new Prompt
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Content = reader.GetString(2),
                                AIProvider = reader.GetString(3),
                                ModelVersion = reader.GetString(4),
                                IsFavorite = reader.GetInt32(5) == 1,
                                CreatedAt = DateTime.Parse(reader.GetString(6)),
                                UpdatedAt = DateTime.Parse(reader.GetString(7)),
                                UsageCount = reader.GetInt32(8)
                            };

                            // Load tags
                            prompt.Tags = GetPromptTags(prompt.Id, connection);
                            prompts.Add(prompt);
                        }
                    }
                }
            }

            return prompts;
        }

        private List<string> GetPromptTags(int promptId, SQLiteConnection connection)
        {
            var tags = new List<string>();

            using (var command = new SQLiteCommand(@"
                SELECT t.Name
                FROM Tags t
                INNER JOIN PromptTags pt ON t.Id = pt.TagId
                WHERE pt.PromptId = @promptId", connection))
            {
                command.Parameters.AddWithValue("@promptId", promptId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tags.Add(reader.GetString(0));
                    }
                }
            }

            return tags;
        }

        public void UpdatePrompt(Prompt prompt)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"
                        UPDATE Prompts 
                        SET Title = @title, Content = @content, AIProvider = @provider,
                            ModelVersion = @version, IsFavorite = @favorite, 
                            UpdatedAt = @updated, UsageCount = @usage
                        WHERE Id = @id";

                    command.Parameters.AddWithValue("@id", prompt.Id);
                    command.Parameters.AddWithValue("@title", prompt.Title);
                    command.Parameters.AddWithValue("@content", prompt.Content);
                    command.Parameters.AddWithValue("@provider", prompt.AIProvider);
                    command.Parameters.AddWithValue("@version", prompt.ModelVersion);
                    command.Parameters.AddWithValue("@favorite", prompt.IsFavorite ? 1 : 0);
                    command.Parameters.AddWithValue("@updated", DateTime.Now.ToString("o"));
                    command.Parameters.AddWithValue("@usage", prompt.UsageCount);

                    command.ExecuteNonQuery();

                    // Update tags
                    command.CommandText = "DELETE FROM PromptTags WHERE PromptId = @id";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@id", prompt.Id);
                    command.ExecuteNonQuery();

                    foreach (var tagName in prompt.Tags)
                    {
                        int tagId = GetOrCreateTag(tagName, connection);
                        LinkPromptToTag(prompt.Id, tagId, connection);
                    }
                }
            }
        }

        public void DeletePrompt(int promptId)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "DELETE FROM Prompts WHERE Id = @id";
                    command.Parameters.AddWithValue("@id", promptId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public string GetDatabasePath()
        {
            return dbPath;
        }
    }
}