using Mafrecal.WorkerService.Data;
using System.Data.SqlClient;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Mafrecal.WorkerService.Logging
{


    public static class AppLogger
    {
        private static readonly Channel<LogEntry> _channel =
            Channel.CreateUnbounded<LogEntry>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        private static string _connectionString;
        private static bool _started = false;

        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;

            if (!_started)
            {
                _started = true;
                Task.Run(ProcessQueueAsync);
            }
        }

        public static void Info(
            string message,
            string source = null,
            dynamic sourceId=null,
            string endpoint = null,
            [CallerMemberName] string method = null)
        {
            Write("INFO", message, source, sourceId, endpoint, null, method);
            Logger.Info($"{source} {sourceId} {endpoint} {message} {method}");
        }

        public static void Error(
            string message,
            string ex = null,
            string source = null,
           dynamic sourceId = null,
            string endpoint = null,
            [CallerMemberName] string method = null)
        {
            Write("ERROR", message, source, sourceId,endpoint, ex, method);
            Logger.Error($"{source} {sourceId} {endpoint} {message} {method}");
        }

        private static void Write(
            string level,
            string msg,
            string source,
            dynamic sourceId,
            string endpoint,
            string ex = null,
            [CallerMemberName] string method = null)
        {
            var entry = new LogEntry
            {
                Level = level,
                Source = source,
                SourceId = sourceId?.ToString(),
                Method = method,
                Endpoint = endpoint,
                Message = msg,
                Exception = ex?.ToString()
            };

            _channel.Writer.TryWrite(entry);
        }

        private static async Task ProcessQueueAsync()
        {
            await foreach (var entry in _channel.Reader.ReadAllAsync())
            {
                try
                {
                    using var con = new SqlConnection(_connectionString);
                    using var cmd = con.CreateCommand();

                    cmd.CommandText = @"
                    INSERT INTO Logs (Level, Source,SourceId, Method, Endpoint, Message, Exception)
                    VALUES (@Level, @Source,@SourceId, @Method, @Endpoint, @Message, @Exception)";

                    cmd.Parameters.AddWithValue("@Level", entry.Level);
                    cmd.Parameters.AddWithValue("@Source", (object?)entry.Source ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SourceId", (object?)entry.SourceId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Method", (object?)entry.Method ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Endpoint", (object?)entry.Endpoint ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Message", entry.Message);
                    cmd.Parameters.AddWithValue("@Exception", (object?)entry.Exception ?? DBNull.Value);

                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
                catch(Exception ex) 
                {
                    Logger.Error($"{ex}");
                    // Se der erro ao gravar log, evitar loop infinito
                    await Task.Delay(100);
                }
            }
        }

    }



}
