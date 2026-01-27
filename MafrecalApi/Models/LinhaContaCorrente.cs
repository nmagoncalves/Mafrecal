// Decompiled with JetBrains decompiler
// Type: MafrecalApiV10.Models.LinhaContaCorrente
// Assembly: MafrecalApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B5E0A4AC-2A4C-4D6E-A0C6-95F4A5D08D9A
// Assembly location: D:\DevPessoal\MafrecalApi.dll

using System;

namespace MafrecalApiV10.Models
{
  public class LinhaContaCorrente
  {
    public string TipoDoc { get; set; }

    public int NumDoc { get; set; }

    public string Serie { get; set; }

    public DateTime Data { get; set; }

    public double Valor { get; set; }

    public double ValorIVA { get; set; }

    public string CodIVA { get; set; }

    public double? TaxaIva { get; set; }

    public string Descricao { get; set; }
  }
}
