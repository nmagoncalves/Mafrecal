
using BasBE100;
using CmpBE100;
using MafrecalApiV10;
using MafrecalApiV10.Models;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using NLog;
using Primavera.WebAPI.Integration;
using StdBE100;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web.Http;

namespace MafrecalApi
{
  [RoutePrefix("MafrecalApi")]
    public class ComprasApiController : ApiController
    {
        private string AppName = Assembly.GetExecutingAssembly().GetName().Name;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [Route("Teste")]
        [HttpGet]
        public IHttpActionResult Index() => Ok("Mafrecal API is running");


        [Route("Compras/Docs/CreateDocument")]
        [HttpPost]
        public IHttpActionResult PostPurchase([FromBody] Compra compra)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var ano = Convert.ToDateTime(compra.DataDoc).Year.ToString();

                var documento = new CmpBEDocumentoCompra
                {
                    Tipodoc = compra.TipoDoc,
                    NumDoc = ProductContext.MotorLE.Base.Series.ProximoNumero(
                        "C", compra.TipoDoc, ano, false),
                    Serie = compra.Serie,
                    TipoEntidade = "F",
                    Entidade = compra.Fornecedor,
                    ModoPag = "NUM",
                    Moeda = "EUR",
                    Utilizador = ProductContext.MotorLE.Contexto.UtilizadorActual,
                    NumDocExterno = compra.NumDocExterno,
                    RefDocOrig = compra.NumDoc,
                    RefSerieDocOrig = compra.Serie,
                    RefTipoDocOrig = compra.TipoDoc,
                    DescFinanceiro = Convert.ToDouble(compra.DescontoFinanceiro),
                    DescFornecedor = Convert.ToDouble(compra.DescontoComercial),
                    CalculoManual = true,
                    CondPag = compra.CondPagamento
                };

                int avisos = 5;
                documento = ProductContext.MotorLE.Compras.Documentos
                    .PreencheDadosRelacionados(documento, ref avisos);

                documento.DataDoc = Convert.ToDateTime(compra.DataDoc);
                documento.DataVenc = Convert.ToDateTime(compra.DataVenc);
                documento.DataIntroducao = Convert.ToDateTime(compra.DataIntroducao);

                double totalMercadoria = 0;

                foreach (var linha in compra.Linhas)
                {
                    if (!ProductContext.MotorLE.Base.Iva.Existe(linha.Iva) &&
                        !Helper.NovaTaxaIva(linha.Iva, linha.TaxaIva))
                    {
                        Logger.Warn($"{AppName} {MethodBase.GetCurrentMethod().Name} Não foi possível criar a taxa de iva {linha.Iva}");
                        return BadRequest($"Erro na taxa de IVA {linha.Iva}");
                    }

                    //if (!ProductContext.MotorLE.Base.Artigos.Existe(linha.Artigo))
                    //{

                    //    var resultado = Helper.NovoItem(new Item
                    //    {
                    //        Codigo = linha.Artigo,
                    //        Descricao = linha.Descricao,
                    //        Iva = linha.Iva,
                    //        TaxaIva = linha.TaxaIva,
                    //        PVP1 = linha.PrecUnit,
                    //        Marca = linha.Marca,
                    //        Familia = linha.Familia,
                    //        Armazem = linha.Armazem,
                    //        CodBarras = linha.CodBarras,
                    //        UnidadeBase = linha.UnidadeBase
                    //    });

                    //    if (!resultado.Sucesso)
                    //    {
                    //        return BadRequest($"{MethodBase.GetCurrentMethod().Name} - {resultado.Mensagem}.");
                    //    }
                    //}

                    double quantidade = Convert.ToDouble(linha.Quantidade);
                    string vazio = string.Empty;

                    ProductContext.MotorLE.Compras.Documentos.AdicionaLinha(
                        documento,
                        linha.Artigo,
                        ref quantidade,
                        ref vazio,
                        ref vazio,
                        Convert.ToDouble(linha.PrecUnit)
                    );

                    var linhaDoc = documento.Linhas.Cast<CmpBELinhaDocumentoCompra>().Last();

                    if (!double.IsNaN(linha.Desconto1))
                        linhaDoc.Desconto1 = linha.Desconto1;

                    if (!double.IsNaN(linha.Desconto2))
                        linhaDoc.Desconto2 = linha.Desconto2;

                    if (!double.IsNaN(linha.Desconto3))
                        linhaDoc.Desconto3 = linha.Desconto3;

                    if (!double.IsNaN(linha.ValorIVA))
                        linhaDoc.TotalIva = Math.Round(linha.ValorIVA, 2);

                    if (!float.TryParse(linha.Iva, out var taxaIva))
                    {
                        Logger.Warn($"{AppName} {MethodBase.GetCurrentMethod().Name} Erro na taxa de iva {linha.Iva}");
                        return BadRequest($"Erro na taxa de IVA {linha.Iva}");
                    }

                    var codIva = Helper.DaCodIva(linha.Iva);

                    if (string.IsNullOrEmpty(codIva))
                    {
                        Logger.Warn($"{AppName} {MethodBase.GetCurrentMethod().Name} Erro na taxa de iva {linha.Iva}");
                        return BadRequest($"Erro na taxa de IVA {linha.Iva}");
                    }

                    linhaDoc.CodIva = codIva;
                    linhaDoc.TaxaIva = taxaIva;
                    linhaDoc.PercIvaDedutivel = 100;
                    linhaDoc.PercIncidenciaIVA = 100;
                    linhaDoc.DescontoComercial = linha.TotalDescontoValor;
                    linhaDoc.PrecoLiquido = linha.TotalLiquido;
                    linhaDoc.TotalIliquido = linha.PrecUnit * quantidade;

                    totalMercadoria += linhaDoc.TotalIliquido;
                }

                if (compra.ResumoIVA != null)
                {
                    foreach (var resumo in compra.ResumoIVA)
                    {
                        documento.ResumoIva.Insere(new BasBEResumoIva
                        {
                            Modulo = "C",
                            Tipodoc = documento.Tipodoc.ToString(),
                            NumDoc = documento.NumDoc,
                            Serie = documento.Serie.ToString(),
                            Filial = documento.Filial.ToString(),
                            CodIva = Helper.DaCodIva(resumo.TaxaIva.ToString()),
                            TaxaIva = resumo.TaxaIva,
                            Incidencia = resumo.ValorIncidencia,
                            Valor = resumo.ValorTotal
                        });
                    }
                }

                documento.TotalMerc = totalMercadoria;
                documento.TotalIva = compra.TotalIva;
                documento.TotalDocumento = compra.TotalDocumento;
                documento.TotalDesc = compra.TotalDesc;
                documento.Documento = $"{documento.Tipodoc} {documento.Serie}/{documento.NumDoc}";

                ProductContext.MotorLE.Compras.Documentos.Actualiza(documento);

                return Ok();
            }
            catch (Exception ex)
            {
                Logger.Warn($"{AppName} {MethodBase.GetCurrentMethod().Name} {ex}");
                return BadRequest($"{MethodBase.GetCurrentMethod().Name} - {ex.Message}.");
            }
        }


        [Route("Fornecedores/Actualiza")]
        [HttpPost]
        public IHttpActionResult PostProvider([FromBody] Fornecedor fornecedor)
        {
            try
            {
                if (fornecedor == null)
                {
                    return BadRequest();
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

 
                var resultado = Helper.NovoFornecedor(fornecedor);

                if (!resultado.Sucesso)
                {
                    return BadRequest(
                     $"{MethodBase.GetCurrentMethod().Name} - {resultado.Mensagem}.");
                }

                return Ok();
            }
            catch (Exception ex)
            {
                Logger.Warn($"{AppName} {MethodBase.GetCurrentMethod().Name} {ex}");

                return BadRequest(
                    $"{MethodBase.GetCurrentMethod().Name} - {ex.Message}."
                );
            }
        }


        [Route("Artigos/Actualiza")]
        [HttpPost]
        public IHttpActionResult PostItem([FromBody] Item item)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                Helper.NovoItem(item);

                var resultado = Helper.NovoItem(item);
 
                if (!resultado.Sucesso)
                {
                    return BadRequest($"{MethodBase.GetCurrentMethod().Name} - {resultado.Mensagem}.");
                }

                return Ok();
            }
            catch (Exception ex)
            {
                Logger.Warn($"{AppName} {MethodBase.GetCurrentMethod().Name} {ex}");

                return BadRequest(
                    $"{MethodBase.GetCurrentMethod().Name} - {ex.Message}."
                );
            }
        }



        [Route("Base/CondPagamento")]
        [HttpPost]
        public IHttpActionResult PostCondPagamento([FromBody] CondPagamento condPagamento)
        {
            try
            {

                if (condPagamento == null)
                {
                    return BadRequest();
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var resultado = Helper.CondPagamento(condPagamento);

                if (!resultado.Sucesso)
                {
                    return BadRequest($"{MethodBase.GetCurrentMethod().Name} - {resultado.Mensagem}.");
                }
 

                return Ok();
            }
            catch (Exception ex)
            {
                Logger.Warn($"{AppName} {MethodBase.GetCurrentMethod().Name} {ex}");

                return BadRequest(
                    $"{MethodBase.GetCurrentMethod().Name} - {ex.Message}."
                );
            }
        }
    }
}
