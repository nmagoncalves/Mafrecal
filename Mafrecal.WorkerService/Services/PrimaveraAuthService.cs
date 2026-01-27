using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;


namespace Mafrecal.WorkerService.Services
{
    using Mafrecal.WorkerService.Logging;
    using Newtonsoft.Json.Linq;
    using RestSharp;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class PrimaveraAuthService
    {
        private readonly HttpClient _client = new();
 
        private readonly string _authUrl, _username, _password, _company, _instance, _line, _url;
   

        private string _cachedToken;
        private DateTime _tokenExpiration;

        public PrimaveraAuthService(string  url, string authUrl, string username, string password, string company, string instance, string line)
        {
            _url = url;
            _authUrl = authUrl;
            _username = username;
            _password = password;
            _company = company;
            _instance = instance;
            _line = line;
        }

        public async Task<string> GetTokenAsync()
        {
            // 1. Se já existe token e ainda está válido → devolve
            if (!string.IsNullOrEmpty(_cachedToken) &&
                DateTime.UtcNow < _tokenExpiration)
            {
                return _cachedToken;
            }

            //  2. Token expirado → pedir novo token
            var baseUrl = _url;
            var clientToken = "rRcuz0euJRp4CbsdQ1ST3JqDG8h3FeWZN3koQbV0yHXAxuXTi5iO3imalvUHY2syGYHWOExUnHycC38vymaCAUtlSwkWya2gSmFZJaQ0U8UiHrNAefdMZeMVI7KPk-g84b6VHoie5PCa5KCII2y2pFQ2P1N61q8zfuhWU-91GDwfg652_aQExPh4NIFw_FnFDKPE4ZSnHSz_1dtV3kNbzOE7aumzdISAdslzxH4pRAvEp_EoagFHDJQEct-gUHYUFSIgxkhoSesvgdAtxqrhpdQHuE3gqIg08Pyzgld6clAS39qjPT2Yvw3wjzDDSukSMXicXXg6i8ZqRhZZEVIqE7t7rSyIEQzJWSd-qdNbHZ5eprVizWO2LxnkbFJvboLBnJI9Hepq3ljkMO9YWbmt4MYFglPSaDkqGv4L0jO_1vkd0YYA3QuPr6CGpdya9Vi4LZIkjBJ1mgbjH7pdFtSiNaXsjxPhD-KnfuDO7tk1DUkEUWs3q8nqv54OVJrSFBxd7KuMhXq9Ito-G8XDSjOQEfZUsU21pPDi_wyJPXAyPCBipYTxfqPpLj6A-hBtNHuW";

            var client = new RestClient(baseUrl);
            var request = new RestRequest("WebApi/token", Method.Post);

            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddHeader("Authorization", $"Bearer {clientToken}");
            request.AddParameter("username", _username);
            request.AddParameter("password", _password);
            request.AddParameter("company", _company);
            request.AddParameter("instance", _instance);
            request.AddParameter("line", _line);
            request.AddParameter("grant_type", "password");

            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                var token = JsonSerializer.Deserialize<Token>(response.Content);

                _cachedToken = token?.access_token;

                //  Token expira em 1 hora (Primavera default)
                _tokenExpiration = DateTime.UtcNow.AddHours(1);
                AppLogger.Info("Token Primavera renovado com sucesso", source: "Mafrecal.WorkerService", endpoint: "");

                return _cachedToken;
            }
            else
            {
                AppLogger.Error(
                 "Erro ao obter token do Primavera",
                 ex: response.Content,
                 source: "Mafrecal.WorkerService",
                 endpoint: "");

                return string.Empty;
            }
        }
    }

    public class Token
    {
        public string access_token { get; set; }
    }





}
