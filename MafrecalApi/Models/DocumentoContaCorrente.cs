// Decompiled with JetBrains decompiler
// Type: MafrecalApiV10.Models.DocumentoContaCorrente
// Assembly: MafrecalApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B5E0A4AC-2A4C-4D6E-A0C6-95F4A5D08D9A
// Assembly location: D:\DevPessoal\MafrecalApi.dll

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MafrecalApiV10.Models
{
  public class DocumentoContaCorrente
  {
    [Required]
    public Entidade Entidade { get; set; }

    public string Modulo { get; set; }

    public string TipoMov { get; set; }

    [Required]
    public string TipoOperacao { get; set; }

    public string TipoEntidade { get; set; } = "C";

    [Required]
    public string TipoDoc { get; set; }

    [Required]
    public int NumDoc { get; set; }

    [Required]
    public string Serie { get; set; }

    public bool PreDatado { get; set; }

    public string NumeroCheque { get; set; }

    public string BalcaoCheque { get; set; }

    public bool ClienteFinal { get; set; }

    public string ClienteOrigem { get; set; }

    public string ClienteDestino { get; set; }

    public double ValorTransferencia { get; set; }

    [Required]
    public double ValorLiquidacao { get; set; }

    public double ValorDesconto { get; set; }

    [Required]
    public DateTime Data { get; set; }

    [Required]
    public List<LinhaContaCorrente> Linhas { get; set; }

    [Required]
    public ContaCaixa ContaCaixa { get; set; }

    [Required]
    public ContaBancaria ContaBancaria { get; set; }

    [Required]
    public CondPagamento ModoPag { get; set; }
  }
}
