using Mafrecal.WorkerService.Logging;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Threading.Tasks;




namespace Mafrecal.WorkerService.Data
{


    public class SqlService
    {



        private readonly string _conn;

        public SqlService(string connectionString) => _conn = connectionString;

        public async Task<List<string>> GetStores(
            CancellationToken cancellationToken = default)
        {
            var stores = new List<string>();

            using (var conn = new SqlConnection(_conn))
            {
                await conn.OpenAsync(cancellationToken);

                using (var cmd = new SqlCommand(@"
            SELECT DISTINCT StoreId
            FROM DocumentsStores
            WHERE 1 = 1
            ", conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            if (!reader.IsDBNull(0))
                            {
                                stores.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }

            return stores;
        }



        public async Task<string> DocumentConfig(string module, string originDocument, string store, int grouped, CancellationToken cancellationToken = default)
        {

            using var conn = new SqlConnection(_conn);
            await conn.OpenAsync(cancellationToken);

            using var cmd = new SqlCommand(@"
                SELECT TipoDocDestiny
                FROM DocumentsStores
                WHERE 1=1 
                  AND Module = @Module
                  AND TipoDocOriginal = @TipoDocOriginal
                  AND StoreId = @StoreId
                  AND Grouped = @Grouped
            ", conn);


            cmd.Parameters.Add("@Module", SqlDbType.NVarChar, 10).Value = module;
            cmd.Parameters.Add("@TipoDocOriginal", SqlDbType.NVarChar, 10).Value = originDocument;
            cmd.Parameters.Add("@StoreId", SqlDbType.NVarChar).Value = store;
            cmd.Parameters.Add("@Grouped", SqlDbType.Int).Value = grouped;

            var result = await cmd.ExecuteScalarAsync(cancellationToken);

            // Handles: no row, NULL value, DBNull
            return result == null || result == DBNull.Value
                ? string.Empty
                : result.ToString();
        }

        /// <summary>
        /// Verifica se já existe um registro com o mesmo SourceEndpoint e SourceEndpointId
        /// </summary>
        public async Task<bool?> ExistsIntAsync(string sourceEndpoint, string sourceEndpointId, long synccounter, CancellationToken cancellationToken = default)
        {
            using var conn = new SqlConnection(_conn);
            await conn.OpenAsync(cancellationToken);

            using var cmd = new SqlCommand(@"
                SELECT TOP 1 Processed
                FROM Transactions
                WHERE SourceEndpoint = @sourceEndpoint
                  AND SourceEndpointId = @sourceEndpointId
                  AND SyncCounter >= @syncCounter
                ORDER BY SyncCounter DESC
            ", conn);

            cmd.Parameters.Add("@sourceEndpoint", SqlDbType.NVarChar, 50).Value = sourceEndpoint;
            cmd.Parameters.Add("@sourceEndpointId", SqlDbType.NVarChar, 100).Value = sourceEndpointId;
            cmd.Parameters.Add("@syncCounter", SqlDbType.BigInt).Value = synccounter;

            var scalar = await cmd.ExecuteScalarAsync(cancellationToken);

            if (scalar == null || scalar == DBNull.Value)
                return null;

            return Convert.ToBoolean(scalar);
        }


        /// <summary>
        /// Verifica se já existe um registro com o mesmo SourceEndpoint e SourceEndpointId
        /// </summary>
        public async Task<bool?> ExistsStringAsync(
            string sourceEndpoint,
            string sourceEndpointId,
            long synccounter,
            CancellationToken cancellationToken = default)
        {
            using var conn = new SqlConnection(_conn);
            await conn.OpenAsync(cancellationToken);

            using var cmd = new SqlCommand(@"
                    SELECT TOP 1 Processed
                    FROM Transactions
                    WHERE SourceEndpoint = @sourceEndpoint
                      AND SourceEndpointId = @sourceEndpointId
                      AND SyncCounter >= @synccounter
                ORDER BY SyncCounter DESC
                ", conn);

            cmd.Parameters.Add("@sourceEndpoint", SqlDbType.NVarChar).Value = sourceEndpoint;
            cmd.Parameters.Add("@sourceEndpointId", SqlDbType.NVarChar).Value = sourceEndpointId;
            cmd.Parameters.Add("@synccounter", SqlDbType.BigInt).Value = synccounter;

            var scalar = await cmd.ExecuteScalarAsync(cancellationToken);

            if (scalar == null || scalar == DBNull.Value)
                return null;

            return Convert.ToBoolean(scalar);
        }


        /// <summary>
        /// Verifica se já existe um registro com o mesmo SourceEndpoint e SourceEndpointId
        /// </summary>
        public async Task<bool> ProcessedAsync(string sourceEndpoint, dynamic sourceEndpointId, CancellationToken cancellationToken = default)
        {
            using var conn = new SqlConnection(_conn);
            await conn.OpenAsync();
            var checkCmd = new SqlCommand(
                "SELECT COUNT(1) FROM Transactions WHERE SourceEndpoint=@sourceEndpoint AND SourceEndpointId=@sourceEndpointId AND Processed=True", conn
            );
            checkCmd.Parameters.AddWithValue("@sourceEndpoint", sourceEndpoint);
            checkCmd.Parameters.AddWithValue("@sourceEndpointId", sourceEndpointId);

            int exists = (int)await checkCmd.ExecuteScalarAsync();
            return exists > 0;
        }

        /// <summary>
        /// Verifica se já existe um registro com o mesmo SourceEndpoint e SourceEndpointId
        /// </summary>
        public async Task<long> LastSyncCounter(
            string sourceEndpoint,
            CancellationToken cancellationToken = default)
        {
            await using var conn = new SqlConnection(_conn);
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
                SELECT TOP (1) Synccounter
                FROM Transactions
                WHERE SourceEndpoint = @sourceEndpoint
                AND Processed = 0
                ORDER BY Synccounter DESC";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@sourceEndpoint", SqlDbType.NVarChar, 255).Value = sourceEndpoint;

            var result = await cmd.ExecuteScalarAsync(cancellationToken);

            return result == null || result == DBNull.Value
                ? 0L
                : Convert.ToInt64(result);
        }




        /// <summary>
        /// Insere um novo registro caso não exista duplicado
        /// </summary>
        public async Task SaveTransactionAsync(string sourceEndpoint, dynamic sourceEndpointId, string jsonData, long syncCounter, CancellationToken cancellationToken = default)
        {
            //if (await ExistsAsync(sourceEndpoint, sourceEndpointId))
            //{
            //    AppLogger.Info($"{sourceEndpoint} {sourceEndpointId} já existe.");
            //    return;
            //}

            using var conn = new SqlConnection(_conn);
            await conn.OpenAsync();

            var insertCmd = new SqlCommand(
                @"INSERT INTO Transactions (SourceEndpointId, SourceEndpoint, JsonData, Synccounter)
              VALUES (@id, @endpoint, @json, @synccounter)", conn
            );
            insertCmd.Parameters.AddWithValue("@id", sourceEndpointId);
            insertCmd.Parameters.AddWithValue("@endpoint", sourceEndpoint);
            insertCmd.Parameters.AddWithValue("@json", jsonData);
            insertCmd.Parameters.AddWithValue("@synccounter", syncCounter);

            //            var insertCmd = new SqlCommand(
            //    @"INSERT INTO Transactions (SourceEndpointId, SourceEndpoint, JsonData)
            //              VALUES (@id, @endpoint, @json)", conn
            //);
            //            insertCmd.Parameters.AddWithValue("@id", sourceEndpointId);
            //            insertCmd.Parameters.AddWithValue("@endpoint", sourceEndpoint);
            //            insertCmd.Parameters.AddWithValue("@json", jsonData);


            await insertCmd.ExecuteNonQueryAsync();
        }

        // Marca registro como processado
        public async Task MarkAsProcessedAsync(string sourceEndpoint, dynamic sourceEndpointId, CancellationToken cancellationToken = default)
        {
            using var con = new SqlConnection(_conn);
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
            UPDATE Transactions
            SET Processed = 1, Error = NULL, ProcessedAt = GETDATE()
            WHERE SourceEndpoint = @SourceEndpoint AND SourceEndpointId = @SourceEndpointId";
            cmd.Parameters.AddWithValue("@SourceEndpoint", sourceEndpoint);
            cmd.Parameters.AddWithValue("@SourceEndpointId", Convert.ToString(sourceEndpointId));

            await con.OpenAsync(cancellationToken);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Regista erro
        public async Task MarkAsErrorAsync(string sourceEndpoint, dynamic sourceEndpointId, string error, CancellationToken cancellationToken = default)
        {
            using var con = new SqlConnection(_conn);
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
            UPDATE Transactions
            SET Processed = 0, Error = @Error
            WHERE SourceEndpoint = @SourceEndpoint AND SourceEndpointId = @SourceEndpointId";
            cmd.Parameters.AddWithValue("@SourceEndpoint", sourceEndpoint);
            cmd.Parameters.AddWithValue("@SourceEndpointId", sourceEndpointId);
            cmd.Parameters.AddWithValue("@Error", error ?? string.Empty);

            await con.OpenAsync(cancellationToken);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<List<ReprocessRequest>> GetPendingReprocessRequestsAsync(
            CancellationToken cancellationToken = default)
        {
            var list = new List<ReprocessRequest>();
            using var con = new SqlConnection(_conn);
            using var cmd = con.CreateCommand();

            cmd.CommandText = @"
                SELECT
                    r.Id,
                    r.SourceEndpoint,
                    r.SourceEndpointId,
                    t.JsonData
                FROM ReprocessRequests r
                INNER JOIN Transactions t
                    ON t.SourceEndpoint = r.SourceEndpoint
                   AND t.SourceEndpointId = r.SourceEndpointId
                WHERE r.Processed = 0
                ORDER BY r.RequestedAt";

            await con.OpenAsync(cancellationToken);

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new ReprocessRequest
                {
                    Id = reader.GetInt32(0),
                    SourceEndpoint = reader.GetString(1),
                    SourceEndpointId = reader.GetString(2),
                    JsonData = reader.GetString(3)

                });
            }

            return list;
        }

        public async Task MarkReprocessAsDoneAsync(int id, string sourceEndpoint)
        {
            using var con = new SqlConnection(_conn);
            using var cmd = con.CreateCommand();

            cmd.CommandText = @"
                UPDATE ReprocessRequests
                SET Processed = 1,
                    ProcessedAt = GETDATE(),
                    Status = 'Done',
                    Error = NULL
                WHERE SourceEndpointId = @id AND SourceEndpoint=@sourceEndpoint";

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@sourceEndpoint", sourceEndpoint);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
        public async Task MarkReprocessAsErrorAsync(int id, string error, string sourceEndpoint)
        {
            using var con = new SqlConnection(_conn);
            using var cmd = con.CreateCommand();

            cmd.CommandText = @"
                UPDATE ReprocessRequests
                SET Error = @err,  Status = 'Error'
                WHERE SourceEndpointId = @id AND SourceEndpoint=@sourceEndpoint";

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@err", error);
            cmd.Parameters.AddWithValue("@sourceEndpoint", sourceEndpoint);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task MarkReprocessAsRunningAsync(int id)
        {
            using var con = new SqlConnection(_conn);
            using var cmd = con.CreateCommand();

            cmd.CommandText = @"
                UPDATE ReprocessRequests
                SET Status = 'Running'
                WHERE SourceEndpointId = @id";

            cmd.Parameters.AddWithValue("@id", id);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }



    }





    //public class TempDatabaseService
    //    {
    //        private readonly string _connectionString;

    //        public TempDatabaseService()
    //        {
    //            _connectionString = ConfigurationManager.ConnectionStrings["TempDb"].ConnectionString;
    //        }

    //        public void Save(string endpoint, List<object> data)
    //        {
    //            using (var conn = new SqlConnection(_connectionString))
    //            {
    //                conn.Open();

    //                foreach (var item in data)
    //                {
    //                    var json = JsonConvert.SerializeObject(item);

    //                    var cmd = new SqlCommand(
    //                        @"INSERT INTO Temp_Storesace_Data
    //                      (SourceEndpoint, JsonData)
    //                      VALUES (@Endpoint, @Json)",
    //                        conn);

    //                    cmd.Parameters.AddWithValue("@Endpoint", endpoint);
    //                    cmd.Parameters.AddWithValue("@Json", json);

    //                    cmd.ExecuteNonQuery();
    //                }
    //            }
    //        }

    //        public List<(int Id, string JsonData)> GetUnprocessed()
    //        {
    //            var list = new List<(int, string)>();

    //            using (var conn = new SqlConnection(_connectionString))
    //            {
    //                conn.Open();
    //                var cmd = new SqlCommand(
    //                    "SELECT Id, JsonData FROM Temp_Storesace_Data WHERE Processed = 0",
    //                    conn
    //                );

    //                var reader = cmd.ExecuteReader();
    //                while (reader.Read())
    //                {
    //                    list.Add((reader.GetInt32(0), reader.GetString(1)));
    //                }
    //            }

    //            return list;
    //        }

    //        public void MarkAsProcessed(int id)
    //        {
    //            using (var conn = new SqlConnection(_connectionString))
    //            {
    //                conn.Open();
    //                var cmd = new SqlCommand(
    //                    "UPDATE Temp_Storesace_Data SET Processed = 1 WHERE Id = @Id",
    //                    conn
    //                );

    //                cmd.Parameters.AddWithValue("@Id", id);
    //                cmd.ExecuteNonQuery();
    //            }
    //        }
    //    }


}

