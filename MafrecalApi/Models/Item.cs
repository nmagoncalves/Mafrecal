// Decompiled with JetBrains decompiler
// Type: MafrecalApiV10.Models.Item
// Assembly: MafrecalApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B5E0A4AC-2A4C-4D6E-A0C6-95F4A5D08D9A
// Assembly location: D:\DevPessoal\MafrecalApi.dll

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MafrecalApiV10.Models
{
  public class Item
  {
    [Key]
    [StringLength(48)]
    public string Codigo { get; set; }

    [StringLength(50)]
    [Required]
    public string Descricao { get; set; } = "";

    [StringLength(250)]
    [DefaultValue("")]
    public string Caracteristicas { get; set; } = "";

    [StringLength(48)]
    public string CodBarras { get; set; } = "";

    [StringLength(2)]
    [DefaultValue("UN")]
    public string UnidadeBase { get; set; } = "UN";

    public string TipoArtigo { get; set; } = "0";

    public short TipoComponente { get; set; } = 0;

    public string Armazem { get; set; } = "";

    public string Marca { get; set; } = "";

    public string Familia { get; set; } = "";

    public string SubFamilia { get; set; } = "";

    public string FamiliaDesc { get; set; } = "";

    public string SubFamiliaDesc { get; set; } = "";

    [DefaultValue("0")]
    public string Iva { get; set; }

    [DefaultValue("0")]
    public float TaxaIva { get; set; }

    [DefaultValue("0")]
    public double PVP1 { get; set; } = 0.0;

    [DefaultValue("0")]
    public double PVP2 { get; set; } = 0.0;

    [DefaultValue("0")]
    public double PVP3 { get; set; } = 0.0;
  }
}
