// Decompiled with JetBrains decompiler
// Type: MafrecalApiV10.Models.ResumoIva
// Assembly: MafrecalApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B5E0A4AC-2A4C-4D6E-A0C6-95F4A5D08D9A
// Assembly location: D:\DevPessoal\MafrecalApi.dll

namespace MafrecalApiV10.Models
{
  public class ResumoIva
  {
    public string Modulo { get; set; }

    public string TipoDoc { get; set; }

    public string NumDoc { get; set; }

    public string Serie { get; set; }

    public string Filial { get; set; }

    public string CodIva { get; set; }

    public double TaxaIva { get; set; }

    public double ValorIncidencia { get; set; }

    public double ValorTotal { get; set; }
  }
}
