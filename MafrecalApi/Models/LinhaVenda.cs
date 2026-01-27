// Decompiled with JetBrains decompiler
// Type: MafrecalApiV10.Models.LinhaVenda
// Assembly: MafrecalApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B5E0A4AC-2A4C-4D6E-A0C6-95F4A5D08D9A
// Assembly location: D:\DevPessoal\MafrecalApi.dll

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MafrecalApiV10.Models
{
  public class LinhaVenda
  {
    [Required]
    [StringLength(48)]
    public string Artigo { get; set; }

    
    [StringLength(48)]
    public string Armazem { get; set; }

    [Required]
    [StringLength(50)]
    public string Descricao { get; set; }

    [Required]
    public double Quantidade { get; set; }

    [Required]
    public double PrecUnit { get; set; }

        [Required(ErrorMessage = "O campo IVA é obrigatório.")]
        public string Iva { get; set; }

        [Required(ErrorMessage = "O campo TaxaIva é obrigatório.")]
        public float TaxaIva { get; set; }

        [DefaultValue("0")]
    [Required]
    public double Desconto1 { get; set; }

    [DefaultValue("0")]
    [Required]
    public double Desconto2 { get; set; }

    [DefaultValue("0")]
    [Required]
    public double Desconto3 { get; set; }

    [Required]
    public double ValorIVA { get; set; }

    [Required]
    public double PrecoLiquido { get; set; }

    [Required]
    public double TotalLiquido { get; set; }

        [Required]
        public double TotalILiquido { get; set; }

        public string Marca { get; set; } = "";

    public string Familia { get; set; } = "";

    [Required]
    public string UnidadeBase { get; set; } = "UN";

    public string CodBarras { get; set; } = "";

    [Required]
    public double TotalDescontoValor { get; set; }
  }
}
