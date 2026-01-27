using Mafrecal.WorkerService.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mafrecal.WorkerService.Helpers
{
    public class PrimaveraTokenManager
    {
        private readonly PrimaveraAuthService _auth;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private string _token;
        private DateTime _expiresAt = DateTime.MinValue;

        public PrimaveraTokenManager(PrimaveraAuthService auth)
        {
            _auth = auth;
        }

        public async Task<string> GetTokenAsync()
        {
            // Token ainda válido
            if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _expiresAt)
                return _token;

            await _semaphore.WaitAsync();
            try
            {
                // Double check (outra task pode já ter renovado)
                if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _expiresAt)
                    return _token;

                var token = await _auth.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                    throw new Exception("Falha ao obter token Primavera");

                _token = token;
                _expiresAt = DateTime.UtcNow.AddMinutes(50); // ou vindo da API

                return _token;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

}
