// Decompiled with JetBrains decompiler
// Type: MafrecalApiV10.Models.Compra
// Assembly: MafrecalApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B5E0A4AC-2A4C-4D6E-A0C6-95F4A5D08D9A
// Assembly location: D:\DevPessoal\MafrecalApi.dll

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MafrecalApiV10.Models
{
  public class DocumentoInterno
    {
    [StringLength(5, ErrorMessage = "O campo Tipo de Documento não pode ter mais de 4 caracteres.")]
    [Required(ErrorMessage = "O campo Tipo de Documento é obrigatório.")]
    public string TipoDoc { get; set; }
 
    public string NumDoc { get; set; }
 
    [Required(ErrorMessage = "O campo Série é obrigatório.")]
    public string Serie { get; set; }

    [Required(ErrorMessage = "O campo Loja é obrigatório.")]
    public string Loja { get; set; }

    [Required(ErrorMessage = "O campo Data do Documento é obrigatório.")]
    public string DataDoc { get; set; }

    [Required(ErrorMessage = "O campo Data de Vencimento é obrigatório.")]
    public string DataVenc { get; set; }

    [Required(ErrorMessage = "O campo Data de Introdução é obrigatório.")]
    public string DataIntroducao { get; set; }

 
    [Required(ErrorMessage = "O campo Linhas do Documento é obrigatório.")]
    public List<LinhaInterno> Linhas { get; set; }

    [Required(ErrorMessage = "O campo Total do Documento é obrigatório.")]
    public double TotalDocumento { get; set; }

    [Required(ErrorMessage = "O campo Total de Mercadoria é obrigatório.")]
    public double TotalMerc { get; set; }

    public double TotalIva { get; set; }

    [Required(ErrorMessage = "O campo Total de Descontos é obrigatório.")]
    public double TotalDesc { get; set; }

    [Required(ErrorMessage = "O Resumo de IVA é obrigatório.")]
    public List<ResumoIva> ResumoIVA { get; set; }

  
  }
}
