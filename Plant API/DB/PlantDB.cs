using Npgsql;
using Plant_API.Constants;
using Plant_API.Models;

namespace Plant_API.DB;

public class PlantDB
{
    private readonly string? _connStr = DefaultConstants.DefaultConnection;

    public async Task AddSearchHistoryAsync(string? plantName, string userId)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        await using (var createCmd = new NpgsqlCommand(
                         $@"CREATE TABLE IF NOT EXISTS search_history_{userId} (
               id SERIAL PRIMARY KEY,
               query_text TEXT NOT NULL,
               created_at TIMESTAMP NOT NULL DEFAULT NOW()
           );", conn))
        {
            await createCmd.ExecuteNonQueryAsync();
        }

        await using (var insertCmd = new NpgsqlCommand(
                         $@"INSERT INTO search_history_{userId} (query_text, created_at)
           VALUES (@q, NOW());", conn))
        {
            insertCmd.Parameters.AddWithValue("q", plantName!);
            await insertCmd.ExecuteNonQueryAsync();
        }
    }


    public async Task CropSearchHistoryAsync(string userId, int maxCount = 20)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        await using (var createCmd = new NpgsqlCommand(
                         $@"CREATE TABLE IF NOT EXISTS search_history_{userId} (
               id SERIAL PRIMARY KEY,
               query_text TEXT NOT NULL,
               created_at TIMESTAMP NOT NULL DEFAULT NOW()
           );", conn))
        {
            await createCmd.ExecuteNonQueryAsync();
        }

        await using (var cmd = new NpgsqlCommand(
                         $@"DELETE FROM search_history_{userId}
           WHERE id NOT IN (
               SELECT id FROM search_history_{userId}
               ORDER BY created_at DESC
               LIMIT @maxCount
           );", conn))
        {
            cmd.Parameters.AddWithValue("maxCount", maxCount);
            await cmd.ExecuteNonQueryAsync();
        }
    }


    public async Task<IEnumerable<SearchHistory>> GetSearchHistoryAsync(string userId)
    {
        var list = new List<SearchHistory>();
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        await using (var createCmd = new NpgsqlCommand(
                         $@"CREATE TABLE IF NOT EXISTS search_history_{userId} (
               id SERIAL PRIMARY KEY,
               query_text TEXT NOT NULL,
               created_at TIMESTAMP NOT NULL DEFAULT NOW()
           );", conn))
        {
            await createCmd.ExecuteNonQueryAsync();
        }

        await using (var cmd = new NpgsqlCommand(
                         $@"SELECT id, query_text, created_at
           FROM search_history_{userId}
           ORDER BY created_at DESC
           LIMIT 20;", conn))
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(new SearchHistory
                {
                    Id = reader.GetInt32(0),
                    PlantName = reader.GetString(1),
                    CreatedAt = reader.GetDateTime(2)
                });
        }

        return list;
    }


    public async Task AddFavouriteAsync(string plantName, string userId)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        var lowercasedInput = plantName.ToLower();
        var p = char.ToUpper(lowercasedInput[0]) + lowercasedInput.Substring(1);
        await using (var createHistoryCmd = new NpgsqlCommand(
                         $@"CREATE TABLE IF NOT EXISTS search_history_{userId} (
                   id SERIAL PRIMARY KEY,
                   query_text TEXT NOT NULL,
                   created_at TIMESTAMP NOT NULL DEFAULT NOW()
               );", conn))
        {
            await createHistoryCmd.ExecuteNonQueryAsync();
        }

        await using (var createFavCmd = new NpgsqlCommand(
                         $@"CREATE TABLE IF NOT EXISTS favourites_{userId} (
                   id SERIAL PRIMARY KEY,
                   plant_name TEXT NOT NULL,
                   added_at TIMESTAMP NOT NULL DEFAULT NOW()
               );", conn))
        {
            await createFavCmd.ExecuteNonQueryAsync();
        }

        await using (var checkDupCmd = new NpgsqlCommand(
                         $@"SELECT EXISTS (
                   SELECT 1 FROM favourites_{userId}
                   WHERE LOWER(plant_name) = LOWER(@p)
               );", conn))
        {
            checkDupCmd.Parameters.AddWithValue("p", p);
            var alreadyFav = (bool)(await checkDupCmd.ExecuteScalarAsync())!;
            if (alreadyFav)
                throw new InvalidOperationException(
                    "Ця рослина вже є у ваших улюблених.");
        }

        await using (var checkHistoryCmd = new NpgsqlCommand(
                         $@"SELECT EXISTS (
                   SELECT 1 FROM search_history_{userId}
                   WHERE LOWER(query_text) = LOWER(@p)
               );", conn))
        {
            checkHistoryCmd.Parameters.AddWithValue("p", p);
            var wasSearched = (bool)(await checkHistoryCmd.ExecuteScalarAsync())!;
            if (!wasSearched)
                throw new InvalidOperationException(
                    "Додати в улюблені можна лише ті рослини, які вже шукали.");
        }


        await using (var insertCmd = new NpgsqlCommand(
                         $@"INSERT INTO favourites_{userId} (plant_name, added_at)
               VALUES (@p, NOW());", conn))
        {
            insertCmd.Parameters.AddWithValue("p", p);
            await insertCmd.ExecuteNonQueryAsync();
        }
    }

    public async Task<IEnumerable<Favourite>> GetFavouritesAsync(string userId)
    {
        var list = new List<Favourite>();
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        await using (var createCmd = new NpgsqlCommand(
                         $@"CREATE TABLE IF NOT EXISTS favourites_{userId} (
               id SERIAL PRIMARY KEY,
               plant_name TEXT NOT NULL,
               added_at TIMESTAMP NOT NULL DEFAULT NOW()
           );", conn))
        {
            await createCmd.ExecuteNonQueryAsync();
        }

        await using (var cmd = new NpgsqlCommand(
                         $@"SELECT id, plant_name, added_at
           FROM favourites_{userId}
           ORDER BY added_at DESC;", conn))
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(new Favourite
                {
                    Id = reader.GetInt32(0),
                    FavPlantName = reader.GetString(1),
                    AddedAt = reader.GetDateTime(2)
                });
        }

        return list;
    }

    public async Task DeleteFavouriteAsync(string plantName, string userId)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        await using (var createCmd = new NpgsqlCommand(
                         $@"CREATE TABLE IF NOT EXISTS favourites_{userId} (
                   id SERIAL PRIMARY KEY,
                   plant_name TEXT NOT NULL,
                   added_at TIMESTAMP NOT NULL DEFAULT NOW()
               );", conn))
        {
            await createCmd.ExecuteNonQueryAsync();
        }

        await using (var checkCmd = new NpgsqlCommand(
                         $@"SELECT EXISTS (
                   SELECT 1 FROM favourites_{userId}
                   WHERE LOWER(plant_name) = LOWER(@p)
               );", conn))
        {
            checkCmd.Parameters.AddWithValue("p", plantName);
            var exists = (bool)(await checkCmd.ExecuteScalarAsync())!;
            if (!exists)
                throw new InvalidOperationException(
                    "Немає такої рослини у списку ваших улюблених.");
        }

        await using (var delCmd = new NpgsqlCommand(
                         $@"DELETE FROM favourites_{userId}
               WHERE LOWER(plant_name) = LOWER(@p);", conn))
        {
            delCmd.Parameters.AddWithValue("p", plantName);
            await delCmd.ExecuteNonQueryAsync();
        }
    }
}