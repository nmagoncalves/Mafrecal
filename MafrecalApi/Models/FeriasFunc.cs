// Decompiled with JetBrains decompiler
// Type: MafrecalApiV10.Models.FeriasFunc
// Assembly: MafrecalApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B5E0A4AC-2A4C-4D6E-A0C6-95F4A5D08D9A
// Assembly location: D:\DevPessoal\MafrecalApi.dll

using System;
using System.Collections.Generic;

namespace MafrecalApiV10.Models
{
  public class FeriasFunc
  {
    public DateTime DataPedido { get; set; }

    public List<Feria> Ferias { get; set; }
  }
}
