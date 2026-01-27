// Decompiled with JetBrains decompiler
// Type: MafrecalApiV10.Models.Credor
// Assembly: MafrecalApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B5E0A4AC-2A4C-4D6E-A0C6-95F4A5D08D9A
// Assembly location: D:\DevPessoal\MafrecalApi.dll

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MafrecalApiV10.Models
{
  public class Credor
  {
    [Key]
    [StringLength(15)]
    public string Codigo { get; set; }

    [StringLength(50)]
    [Required]
    public string Nome { get; set; }

    [StringLength(100)]
    [Required]
    public string Descricao { get; set; }

    [Required]
    public string Morada { get; set; }

    [Required]
    public string Localidade { get; set; }

    [Required]
    public string CodigoPostal { get; set; }

    [Required]
    public string LocalidadeCodigoPostal { get; set; }

    [Required]
    public string Telefone { get; set; }

    public string Fax { get; set; }

    public string Email { get; set; }

    public string Distrito { get; set; }

    [Required]
    public string NumContribuinte { get; set; }

    [Required]
    public string Pais { get; set; }

    [DefaultValue("EUR")]
    public string Moeda { get; set; }

    [Required]
    public Enumerator.TipoEntidade TipoTerceiro { get; set; }
  }
}
