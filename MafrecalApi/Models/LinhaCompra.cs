// Decompiled with JetBrains decompiler
// Type: MafrecalApiV10.Models.LinhaCompra
// Assembly: MafrecalApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B5E0A4AC-2A4C-4D6E-A0C6-95F4A5D08D9A
// Assembly location: D:\DevPessoal\MafrecalApi.dll

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MafrecalApiV10.Models
{
  public class LinhaCompra
  {
    [Required(ErrorMessage = "O campo Artigo é obrigatório.")]
    [StringLength(48, ErrorMessage = "O campo Artigo não pode ter mais de 48 caracteres.")]
    public string Artigo { get; set; }

    [Required(ErrorMessage = "O campo Descrição é obrigatório.")]
    [StringLength(50, ErrorMessage = "O campo Descrição não pode ter mais de 50 caracteres.")]
    public string Descricao { get; set; }

    [Required(ErrorMessage = "O campo Quantidade é obrigatório.")]
    public double Quantidade { get; set; }

    [Required(ErrorMessage = "O campo Preço Unitário é obrigatório.")]
    public double PrecUnit { get; set; }

    [Required(ErrorMessage = "O campo IVA é obrigatório.")]
    public string Iva { get; set; }

    [Required(ErrorMessage = "O campo TaxaIva é obrigatório.")]
    public float TaxaIva { get; set; }

    [DefaultValue("0")]
    public double Desconto1 { get; set; }

    [DefaultValue("0")]
    public double Desconto2 { get; set; }

    [DefaultValue("0")]
    public double Desconto3 { get; set; }

    [Required(ErrorMessage = "O campo Valor de IVA é obrigatório.")]
    public double ValorIVA { get; set; }

    [Required(ErrorMessage = "O campo Preço Líquido é obrigatório.")]
    public double PrecoLiquido { get; set; }

    [Required(ErrorMessage = "O campo Total Líquido é obrigatório.")]
    public double TotalLiquido { get; set; }

    [Required(ErrorMessage = "O campo Total de Desconto é obrigatório.")]
    public double TotalDescontoValor { get; set; }

    public string CodBarras { get; set; } = "";

    public string UnidadeBase { get; set; } = "UN";

    public string Marca { get; set; } = "";

    public string Familia { get; set; } = "";

    public string Armazem { get; set; } = "";
  }
}
