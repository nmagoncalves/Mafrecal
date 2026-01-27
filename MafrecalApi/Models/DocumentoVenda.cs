// Decompiled with JetBrains decompiler
// Type: MafrecalApiV10.Models.DocumentoVenda
// Assembly: MafrecalApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B5E0A4AC-2A4C-4D6E-A0C6-95F4A5D08D9A
// Assembly location: D:\DevPessoal\MafrecalApi.dll

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MafrecalApiV10.Models
{
  public class DocumentoVenda
  {
    [Required]
    public string Cliente { get; set; }

    public string Modulo { get; set; }

    public string TipoMov { get; set; }

    public string TipoEntidade { get; set; }

    [Required]
    public string Tipodoc { get; set; }

    [Required]
    public string Serie { get; set; }

    [Required]
    public int NumDoc { get; set; }

    public string Seccao { get; set; }

    [Required]
    public string Loja { get; set; }

    public string NumVendedor { get; set; }

    public string NomeVendedor { get; set; }

        [StringLength(26, ErrorMessage = "O campo Referencia não pode ter mais de 26 caracteres.")]
        public string Referencia { get; set; }

    public string ArmazemOrigem { get; set; }

        [Required(ErrorMessage = "O campo Data do Documento é obrigatório.")]
        public string DataDoc { get; set; }

        [Required(ErrorMessage = "O campo Data de Vencimento é obrigatório.")]
        public string DataVenc { get; set; }

 
        public string RefDocOrig { get; set; }

    public string RefTipoDocOrig { get; set; }

    public string RefSerieDocOrig { get; set; }
        [StringLength(26, ErrorMessage = "O campo Referencia não pode ter mais de 26 caracteres.")]
        public string RefDocOrigFT { get; set; }

    public string RefTipoDocOrigFT { get; set; }

    public string RefSerieDocOrigFT { get; set; }

        [Required]
        public string ModPag { get; set; } = "NUM";

   
    public string Moeda { get; set; } ="EUR";

        public string Observacoes { get; set; }

    [Required]
    public string LocalOperacao { get; set; }

    [Required]
    public string EspacoFiscal { get; set; }

    public string Assinatura { get; set; }

    public string Certificado { get; set; }

    public bool? EfectuaRetencao { get; set; } = new bool?(false);

    [Required]
    public double TotalDocumento { get; set; }

    [Required]
    public double TotalDesconto { get; set; }

    [Required]
    public double TotalIva { get; set; }

    [Required]
    public double TotalMerc { get; set; }

    [Required]
    public double TotalDesc { get; set; }

    [Required]
    public double DescontoComercial { get; set; }

    [Required]
    public double DescontoFinanceiro { get; set; }

    [Required]
     public List<ResumoIva> ResumoIva { get; set; }

    [Required]
    public List<LinhaVenda> Linhas { get; set; }
    public string DocumentoFecho { get; set; }
    public List<FechoCaixa> ResumoTipoPag { get; set; }
    }
}
