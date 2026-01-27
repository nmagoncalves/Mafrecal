// Decompiled with JetBrains decompiler
// Type: MafrecalApiV10.Models.Fornecedor
// Assembly: MafrecalApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B5E0A4AC-2A4C-4D6E-A0C6-95F4A5D08D9A
// Assembly location: D:\DevPessoal\MafrecalApi.dll

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MafrecalApiV10.Models
{
  public class Fornecedor
  {
    [Key]
    [StringLength(15, ErrorMessage = "O campo Código não pode ter mais de 15 caracteres.")]
    [Required(ErrorMessage = "O campo Código é obrigatório.")]
    public string Codigo { get; set; }

    [StringLength(50, ErrorMessage = "O campo Nome não pode ter mais de 50 caracteres.")]
    [Required(ErrorMessage = "O campo Nome é obrigatório.")]
    public string Nome { get; set; }

        [StringLength(50, ErrorMessage = "O campo Morada não pode ter mais de 50 caracteres.")]
    public string Morada { get; set; }

    [Required(ErrorMessage = "O campo Localidade é obrigatório.")]

    public string Localidade { get; set; }

    [Required(ErrorMessage = "O campo Código Postal é obrigatório.")]
    public string CodigoPostal { get; set; }


    [Required(ErrorMessage = "O campo Localidade do Código Postal é obrigatório.")]
    public string LocalidadeCodigoPostal { get; set; }

    public string Telefone { get; set; }

    public string Fax { get; set; }

    public string Email { get; set; }

    public string EnderecoWeb { get; set; }

    public string LocalOperacao { get; set; }

    public string Distrito { get; set; }

    [Required(ErrorMessage = "O campo Número de Contribuinte é obrigatório.")]
    public string NumContribuinte { get; set; }

    [Required(ErrorMessage = "O campo País é obrigatório.")]
    [DefaultValue("PT")]
    public string Pais { get; set; }

    [DefaultValue("EUR")]
    public string Moeda { get; set; }

    [Required(ErrorMessage = "O campo Tipo de Terceiro é obrigatório.")]
    [RegularExpression("[I|C|D|F]", ErrorMessage = "O campo Tipo de Terceiro deve conter um valor válido: I, C, D ou F.")]
    public Enumerator.TipoEntidade TipoTerceiro { get; set; }

    public string CondPagamento { get; set; }
  }
}
