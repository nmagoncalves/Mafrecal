// Decompiled with JetBrains decompiler
// Type: MafrecalApiV10.Models.Entidade
// Assembly: MafrecalApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B5E0A4AC-2A4C-4D6E-A0C6-95F4A5D08D9A
// Assembly location: D:\DevPessoal\MafrecalApi.dll

using System.ComponentModel;

namespace MafrecalApiV10.Models
{
  public class Entidade
  {
    public string Codigo { get; set; }

    public string Nome { get; set; }

    public string NumContribuinte { get; set; }

    public string Morada { get; set; }

    public string Morada2 { get; set; }

    public string Localidade { get; set; }

    public string CodigoPostal { get; set; }

    public string Pais { get; set; } = "PT";

    public string Moeda { get; set; } = "EUR";

    public string LocalidadeCodigoPostal { get; set; }

        [DefaultValue("UN")]
        public string CondPag { get; set; } = "1";

    public string LocalOperacao { get; set; }

    public string EspacoFiscal { get; set; }

    public string ModPag { get; set; }
  }
}
