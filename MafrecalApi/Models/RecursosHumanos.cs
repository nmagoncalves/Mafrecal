// Decompiled with JetBrains decompiler
// Type: MafrecalApiV10.Models.RecursosHumanos
// Assembly: MafrecalApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B5E0A4AC-2A4C-4D6E-A0C6-95F4A5D08D9A
// Assembly location: D:\DevPessoal\MafrecalApi.dll

using System.ComponentModel.DataAnnotations;

namespace MafrecalApiV10.Models
{
  public class RecursosHumanos
  {
    [Key]
    [StringLength(15)]
    [Required]
    public string Codigo { get; set; }
  }
}
