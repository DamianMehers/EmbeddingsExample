using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using SQLite;
using static SQLite.SQLite3;

namespace EmbeddingsExample.SqliteExtensions;

internal class Demo
{
  // nomic-bert.embedding_length from https://ollama.com/library/nomic-embed-text/blobs/970aa74c0a90
  const int EmbeddingLength = 768;

  const string LibraryPath = "e_sqlite3";


  internal async Task InitializeDatabase()
  {
    var databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Demo.db");

    if(File.Exists(databasePath))
    {
      File.Delete(databasePath);
    }

    // open the database in read/write mode and create it if it doesn't exist
    var sqliteFlags = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create;
    var connection = new SQLiteAsyncConnection(databasePath, sqliteFlags);

    var extension = GetExtensionName();

    await LoadExtension(connection, extension);

    await connection.CreateTableAsync<Model>();

    await connection.ExecuteAsync($"CREATE VIRTUAL TABLE IF NOT EXISTS vec_items USING vec0(embedding float[{EmbeddingLength}]);");


    // Newspaper headlines from the 1800s
    var headlines = new List<string>
    {
        "Gold Discovered in California!",
        "The Great Fire of London Bridge",
        "First Steam-Powered Train Debuts",
        "The Telegraph Revolutionizes Communication",
        "The First Telephone Call by Alexander Graham Bell",
        "The Irish Potato Famine Strikes",
        "Darwin Publishes 'On the Origin of Species'",
        "The American Civil War Begins",
        "Eiffel Tower Construction Announced",
        "The Light Bulb Invented by Edison"
    };


    // Create and insert models
    for (int i = 0; i < headlines.Count; i++)
    {
      var text = headlines[i];
      Console.WriteLine($"{i} Generating embedding for {text}");
      var model = new Model { Title = text };
      await connection.InsertAsync(model);
      var embedding = await GenerateEmbedding(text);
      await AddEmbedding(connection, model, embedding);
    }

    // Now search
    Console.WriteLine("Searching ...");
    var searchEmbedding = await GenerateEmbedding("disaster");
    var results = await SearchForEmbedding(connection, searchEmbedding, k: 3);
    foreach (var result in results)
    {
      Console.WriteLine($"Found: {result.Title}");
    }
  }

  private string GetExtensionName()
  {

    // Could also look at RuntimeInformation.ProcessArchitecture;
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      return "SqliteExtensions/winx86/vec0";
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
      return "SqliteExtensions/macosarm/vec0";
    }
    else
    {
      throw new NotSupportedException("Unsupported platform");
    }
  }


  [DllImport(LibraryPath, EntryPoint = "sqlite3_load_extension", CallingConvention = CallingConvention.Cdecl)]
  public static extern Result LoadExtension(SafeHandle db, [MarshalAs(UnmanagedType.LPStr)] string filename, int entry, int msg);
  private async Task LoadExtension(SQLiteAsyncConnection connection, string extension)
  {
    await connection.EnableLoadExtensionAsync(true);

    var connectionWithLock = connection.GetConnection();
    using var theLock = connectionWithLock.Lock();
    var handle = connectionWithLock.Handle;
    var result = LoadExtension(handle, extension, 0, 0);
    if (result != Result.OK)
    {
      throw new Exception("Failed to load extension: " + result);
    }
  }

  private async Task<double[]> GenerateEmbedding(string text)
  {
    var client = new HttpClient();
    var requestBody = new
    {
      model = "nomic-embed-text",
      prompt = text
    };

    var json = JsonSerializer.Serialize(requestBody);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await client.PostAsync("http://localhost:11434/api/embeddings", content);
    var responseStream = await response.Content.ReadAsStreamAsync();

    var result = await JsonSerializer.DeserializeAsync<EmbeddingResponse>(responseStream);
    return result?.Embedding ?? [];
  }

  private async Task AddEmbedding(SQLiteAsyncConnection connection,  Model model, double[] embedding)
  {
    var sql = "INSERT INTO vec_items (rowid, embedding) VALUES (?, ?)";
    var json = JsonSerializer.Serialize(embedding);
    var parameters = new object[] { model.Id, json };
    await connection.ExecuteAsync(sql, parameters);
  }

  private async Task<List<Model>> SearchForEmbedding(SQLiteAsyncConnection connection, double[] embedding, int k)
  {
    var json = JsonSerializer.Serialize(embedding);
    var sql = $"SELECT Model.* FROM vec_items LEFT JOIN Model ON Model.Id = vec_items.rowid  WHERE embedding MATCH ? AND k=? order by distance";
    var parameters = new object[] { json, k };
    var result = await connection.QueryAsync<Model>(sql, parameters);
    return result;

  }



  public class EmbeddingResponse
  {
    [JsonPropertyName("embedding")]
    public double[] Embedding { get; set; } = [];
  }




  class Model
  {
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Title { get; set; } = "";

  }
}