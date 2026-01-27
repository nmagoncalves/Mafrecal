// Decompiled with JetBrains decompiler
// Type: MafrecalApiV10.PingController
// Assembly: MafrecalApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B5E0A4AC-2A4C-4D6E-A0C6-95F4A5D08D9A
// Assembly location: D:\DevPessoal\MafrecalApi.dll

using System.Web.Http;

namespace MafrecalApiV10
{
    [RoutePrefix("MafrecalApi2")]
  public class PingController : ApiController
  {
        [Route("Teste2")]
        [HttpGet]
        public IHttpActionResult Index() => Ok("Mafrecal API is running");
  
  }
}
