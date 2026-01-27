using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mafrecal.WorkerService.Helpers
{
    using Mafrecal.WorkerService.Services;
    using System.Collections.Generic;
    using System.Text.Json;

    public static class JsonHelper
    {
        /// <summary>
        /// Extrai todas as mensagens de erro do ModelState em JSON.
        /// </summary>
        /// <param name="json">O JSON retornado pela API</param>
        /// <returns>Lista de mensagens de erro</returns>
        public static List<string> ExtractModelStateErrors(string json)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(json))
                return errors;

            try
            {
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("ModelState", out JsonElement modelState))
                {
                    foreach (var property in modelState.EnumerateObject())
                    {
                        foreach (var error in property.Value.EnumerateArray())
                        {
                            var msg = error.GetString();
                            if (!string.IsNullOrWhiteSpace(msg))
                                errors.Add(msg);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Se não for JSON válido ou não tiver ModelState, retorna vazio
            }

            return errors;
        }

        public static string BuildErrorMessage(PrimaveraResponse result)
        {
            var errors = JsonHelper.ExtractModelStateErrors(result.ResponseContent);
            return errors.Any()
                ? string.Join(" | ", errors)
                : result.ResponseContent;
        }

        public static JsonElement ExtractFirstResult(JsonDocument doc)
        {
            var root = doc.RootElement;

            // Caso 1: resposta com campo "results"
            if (root.TryGetProperty("results", out var results) &&
                results.ValueKind == JsonValueKind.Array &&
                results.GetArrayLength() > 0)
            {
                return results[0].Clone();
            }

            // Caso 2: resposta é um único objeto
            if (root.ValueKind == JsonValueKind.Object)
            {
                return root.Clone();
            }

            // Caso 3: resposta é um array (não é o teu caso, mas deixo suporte)
            if (root.ValueKind == JsonValueKind.Array &&
                root.GetArrayLength() > 0)
            {
                return root[0].Clone();
            }

            return new JsonElement();
        }


    }

}
