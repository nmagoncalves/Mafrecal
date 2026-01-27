// Decompiled with JetBrains decompiler
// Type: MafrecalApiV10.Models.Artigos
// Assembly: MafrecalApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B5E0A4AC-2A4C-4D6E-A0C6-95F4A5D08D9A
// Assembly location: D:\DevPessoal\MafrecalApi.dll

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MafrecalApiV10.Models
{
  public class Artigos
  {
    [Required]
    [StringLength(20)]
    public string Artigo { get; set; }

    [Required]
    [StringLength(48)]
    public string Descricao { get; set; }

    public string Armazem { get; set; }

    public string Marca { get; set; }

    public string Familia { get; set; }

    [StringLength(48)]
    public string CodBarras { get; set; }

    [StringLength(2)]
    [DefaultValue("UN")]
    [Required]
    public string UnidadeBase { get; set; }

    public string TipoArtigo { get; set; } = "3";

    public short TipoComponente { get; set; } = 0;

    public bool PermiteDevolucao { get; set; } = true;

    public bool? TrataNumerosSerie { get; set; }

    public string MovStock { get; set; } = "S";

    [StringLength(8)]
    [DefaultValue("0")]
    [Required]
    public double PVP1 { get; set; }

    [StringLength(8)]
    [DefaultValue("0")]
    [Required]
    public double PVP2 { get; set; }

    [StringLength(8)]
    [DefaultValue("0")]
    [Required]
    public double PVP3 { get; set; }

    public TaxasIva TaxaIva { get; set; }

    [Required]
    public string ValorIVA { get; set; }

    [Required]
    public string PrecoLiquido { get; set; }

    [Required]
    public string TotalLiquido { get; set; }
  }
}
