// Decompiled with JetBrains decompiler
// Type: MafrecalApiV10.Models.Feria
// Assembly: MafrecalApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B5E0A4AC-2A4C-4D6E-A0C6-95F4A5D08D9A
// Assembly location: D:\DevPessoal\MafrecalApi.dll

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MafrecalApiV10.Models
{
  public class Feria
  {
    [Key]
    [StringLength(15)]
    [Required]
    public string Codigo { get; set; }

    [Key]
    [Required]
    public string Ano { get; set; }

    [Key]
    [Column(Order = 2)]
    [Required]
    public string DataFeria { get; set; }

    public string EstadoGozo { get; set; }

    public string Duracao { get; set; }

    public string Marcado { get; set; }
  }
}
