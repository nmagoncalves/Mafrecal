using BasBE100;
using CmpBE100;
using ErpBS100;
using MafrecalApiV10.Models;
using NLog;
using Primavera.WebAPI.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Web.Http;
using TesBE100;
using VndBE100;

namespace MafrecalApiV10.MafrecalApi
{
    [RoutePrefix("MafrecalApi")]
    public class VendasApiController : ApiController
    {
        private string AppName = Assembly.GetExecutingAssembly().GetName().Name;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        [Route("Vendas/Docs/CreateDocument")]


        public IHttpActionResult NovoDocumentoVenda([FromBody] DocumentoVenda documento)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (documento == null)
                return BadRequest("Documento inválido.");

            if (documento.Linhas == null || documento.Linhas.Count == 0)
            {
                Logger.Warn($"{AppName} {MethodBase.GetCurrentMethod().Name} Documento sem linhas.");
                return BadRequest("O documento não contém linhas.");
            }

            try
            {

                // Verifica duplicados
                string idExistente = Helper.DaIDDocumentoVendaExt(
                documento.Tipodoc,
                documento.Referencia,
                documento.Serie);

                if (!string.IsNullOrEmpty(idExistente))
                    return BadRequest("O documento já existe.");

                VndBETabVenda TabVenda = null;

                // Cria documento
                var docVenda = new VndBEDocumentoVenda
                {
                    Tipodoc = documento.Tipodoc,
                    NumDoc = ProductContext.MotorLE.Base.Series.ProximoNumero("V", documento.Tipodoc, documento.Serie),
                    Serie = documento.Serie,
                    TipoEntidade = "C",
                    Entidade = documento.Cliente,
                    Seccao = documento.Loja,
                    ModoPag = documento.ModPag,
                    Moeda = documento.Moeda,
                    Utilizador = ProductContext.MotorLE.Contexto.UtilizadorActual,
                  
                    Assinatura = "XXXXXXX",
                    VersaoAssinatura = "1",
                    Certificado = "YYYY",

                    Requisicao = documento.Referencia
                };

                int preenche = 5;
                docVenda = ProductContext.MotorLE.Vendas.Documentos
                    .PreencheDadosRelacionados(docVenda, ref preenche);

                docVenda.DataDoc = Convert.ToDateTime(documento.DataDoc);
                docVenda.DataVenc = Convert.ToDateTime(documento.DataVenc);
                docVenda.CalculoManual = true;


                if (documento.Tipodoc.StartsWith("DV"))
                {
                    docVenda.MotivoEmissao = "001";
                    docVenda.DescricaoMotivoEmissao = "Devolução";
                }

                // Referências
                docVenda.RefDocOrig = documento.Referencia;
                docVenda.RefSerieDocOrig = documento.Serie;
                docVenda.RefTipoDocOrig = documento.Tipodoc;

                docVenda.CamposUtil["CDU_NUMDOCORIGINAL"].Valor = documento.Referencia;

                int linhaNr = 1;
                double totalMercadoria = 0;

                foreach (var linha in documento.Linhas)
                {
                    if (!ProductContext.MotorLE.Base.Iva.Existe(linha.Iva) &&
                        !Helper.NovaTaxaIva(linha.Iva, linha.TaxaIva))
                    {
                        return BadRequest($"IVA inválido: {linha.Iva}");
                    }

                    if (!ProductContext.MotorLE.Base.Artigos.Existe(linha.Artigo))
                    {
                        var resArtigo = Helper.NovoItem(new Item
                        {
                            Codigo = linha.Artigo,
                            Descricao = linha.Descricao,
                            Iva = linha.Iva,
                            TaxaIva = linha.TaxaIva,
                            PVP1 = linha.PrecUnit,
                            Marca = linha.Marca,
                            Familia = linha.Familia,
                            Armazem = linha.Armazem,
                            CodBarras = linha.CodBarras,
                            UnidadeBase = linha.UnidadeBase
                        });

                        if (!resArtigo.Sucesso)
                            return BadRequest(resArtigo.Mensagem);
                    }

                    double qtd = 1;
                    string arm = linha.Armazem;

                    ProductContext.MotorLE.Vendas.Documentos.AdicionaLinha(
                        docVenda,
                        linha.Artigo,
                        ref qtd,
                        ref arm,
                        ref arm,
                        linha.PrecUnit,
                        linha.Desconto1);

                    var linhaDoc = docVenda.Linhas.Cast<VndBELinhaDocumentoVenda>().Last();
                    linhaDoc.Desconto1 = linha.Desconto1;
                    linhaDoc.Desconto2 = linha.Desconto2;
                    linhaDoc.Desconto3 = linha.Desconto3;
                    linhaDoc.PercIvaDedutivel = 100;
                    linhaDoc.PercIncidenciaIVA = 100;
                    linhaDoc.CodIva = Helper.DaCodIva(linha.Iva);
                    linhaDoc.TaxaIva = float.Parse(linha.Iva.Trim());
                    linhaDoc.TotalIva = linha.ValorIVA;
                    linhaDoc.PrecUnit = linha.PrecUnit;
                    linhaDoc.PrecoLiquido = linha.PrecoLiquido;
                 
                    linhaDoc.TotalIliquido = linha.TotalILiquido;

                    totalMercadoria += linhaDoc.PrecoLiquido;

                    linhaNr++;
                }

                // Resumos de IVA   

                if (documento.ResumoIva != null)
                {
                    foreach (var resumo in documento.ResumoIva)
                    {
                        docVenda.ResumoIva.Insere(new BasBEResumoIva
                        {
                            Modulo = "C",
                            Tipodoc = documento.Tipodoc.ToString(),
                            NumDoc = documento.NumDoc,
                            Serie = documento.Serie.ToString(),
                            Filial = "000",
                            CodIva = Helper.DaCodIva(resumo.TaxaIva.ToString()),
                            TaxaIva = resumo.TaxaIva,
                            Incidencia = resumo.ValorIncidencia,
                            Valor = resumo.ValorTotal
                        });
                    }
                }


                // Totais
                docVenda.TotalMerc = totalMercadoria;
                docVenda.TotalIva = documento.TotalIva;
                docVenda.TotalDocumento = documento.TotalDocumento;
                docVenda.DataGravacao = DateTime.UtcNow;

                docVenda.Documento = $"{docVenda.Tipodoc} {docVenda.Serie}/{docVenda.NumDoc}";

                TabVenda = ProductContext.MotorLE.Vendas.TabVendas.Edita(docVenda.Tipodoc);
                var serie = docVenda.Serie;
                var erro = "";

                //if (!ProductContext.MotorLE.Vendas.Documentos.ValidaActualizacao(docVenda, TabVenda, ref serie, ref erro))
                //    return BadRequest($"Erro ao criar documento de venda {docVenda.Documento}: {erro}");

               string natureza = TabVenda.PagarReceber == "R" ? "C" : "D";

                var resultado = ValidaDocumentoCaixa(docVenda, documento.ResumoTipoPag, documento.DocumentoFecho, natureza);

                if (!resultado.sucesso)
                {
                    return BadRequest(resultado.mensagem);
                }

                ProductContext.MotorLE.Vendas.Documentos.Actualiza(docVenda);

                resultado = NovoDocumentoCaixa(docVenda, documento.ResumoTipoPag, documento.DocumentoFecho, natureza);

                if (!resultado.sucesso)
                {

                    return BadRequest(resultado.mensagem);
                }

                return Ok();


            }
            catch (Exception ex)
            {
                Logger.Warn($"{AppName} {MethodBase.GetCurrentMethod().Name} {ex}");
                return BadRequest($"Erro ao criar documento de venda {documento.Tipodoc} {documento.Serie}/{documento.NumDoc}.{ex.Message}");
            }
        }

        [Route("Clientes/Actualiza")]
        [HttpPost]
        public IHttpActionResult PostCustomer([FromBody] Entidade cliente)
        {
            try
            {
                if (cliente == null)
                {
                    return BadRequest();
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }


                var resultado = Helper.NovoCliente(cliente);

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


        public static (bool sucesso, string mensagem) NovoDocumentoCaixa(VndBEDocumentoVenda DocumentoOrigem, List<FechoCaixa> DocumentoFecho, string tipoDocumentoFecho, string natureza)
        {

            TesBEDocumentoTesouraria DocumentoTes;
            TesBELinhaDocTesouraria LinhaTes;


            try
            {

                DocumentoTes = new TesBEDocumentoTesouraria
                {
                     //IdDocOrigem = DocumentoOrigem.ID,
                    Filial = ProductContext.MotorLE.Base.Filiais.CodigoFilial.Length > 0 ? ProductContext.MotorLE.Base.Filiais.CodigoFilial : "000",
                    TipoLancamento = DocumentoOrigem.TipoLancamento,
                    Tipodoc = tipoDocumentoFecho,
                    Entidade = "",
                    TipoEntidade = "",
                    Serie = Convert.ToDateTime(DocumentoOrigem.DataDoc).Year.ToString(), // BSO.Base.Series.DaSerieDefeito("B", Documento.TipoDoc, Documento.Data),
                    Moeda = DocumentoOrigem.Moeda,
                    ModuloOrigem = "B",
                   
                    ContaOrigem = DocumentoFecho[0].Caixa,
                    ContaDestino = "",
                    Data = Convert.ToDateTime(DocumentoOrigem.DataDoc),
                    DataIntroducao = Convert.ToDateTime(DocumentoOrigem.DataDoc),

                    Cambio = DocumentoOrigem.Cambio,
                    CambioMBase = DocumentoOrigem.CambioMBase,
                    CambioMAlt = DocumentoOrigem.CambioMAlt,

                };

                if (natureza == "C")
                {
                    DocumentoTes.TotalDebito = 0;
                    DocumentoTes.TotalCredito = DocumentoFecho.Sum(x => x.Valor);
                }
                else
                {
                    DocumentoTes.TotalDebito = DocumentoFecho.Sum(x => Math.Abs(x.Valor));
                    DocumentoTes.TotalCredito = 0;
                     
                }
                foreach (FechoCaixa Linha in DocumentoFecho)
                {
                    LinhaTes = new TesBELinhaDocTesouraria
                    {
                        Entidade = "",
                        TipoEntidade = "",
                        DataMovimento = DocumentoOrigem.DataDoc,
                        DataValor = DocumentoOrigem.DataDoc,
                 
                        Conta = Linha.Caixa,
                        Moeda = DocumentoOrigem.Moeda,
                        Cambio = DocumentoOrigem.Cambio,
                        CambioMBase = DocumentoOrigem.CambioMBase,
                        CambioMAlt = DocumentoOrigem.CambioMAlt,
                        Descricao = Linha.TipoPagamento,
                        Natureza = natureza,
                        IVA = "00",
                        AnaliticaCBL = "",
                        CCustoCBL = ""
                    };

                    if (natureza == "C")
                    {
                        LinhaTes.Debito = 0;
                        LinhaTes.Credito =  Linha.Valor;
                        LinhaTes.MovimentoBancario = Linha.TipoPagamento;
                    }
                    else
                    {
                        LinhaTes.Debito = Math.Abs(Linha.Valor) ;
                        LinhaTes.Credito = 0;
                        LinhaTes.MovimentoBancario = $"{Linha.TipoPagamento}D";
                    }

                    DocumentoTes.Linhas.Insere(LinhaTes);
                }


                ProductContext.MotorLE.Tesouraria.Documentos.Actualiza(DocumentoTes);

                return (true, "Documento de Fecho de Caixa criado com sucesso.");

            }
            catch (Exception ex)
            {

                return (false, $"Erro ao gerar documento de tesouraria com origem em {DocumentoOrigem.Documento} {ex.Message}");
            }
            finally
            {
                DocumentoTes = null;
                LinhaTes = null;

            }

        }

        public static (bool sucesso, string mensagem) ValidaDocumentoCaixa(VndBEDocumentoVenda DocumentoOrigem, List<FechoCaixa> DocumentoFecho, string tipoDocumentoFecho, string natureza)
        {

            TesBEDocumentoTesouraria DocumentoTes;
            TesBELinhaDocTesouraria LinhaTes;
 
            try
            {
                if (DocumentoFecho != null)
                {
                    DocumentoTes = new TesBEDocumentoTesouraria
                    {
                       // IdDocOrigem = DocumentoOrigem.ID,
                        Filial = ProductContext.MotorLE.Base.Filiais.CodigoFilial.Length > 0 ? ProductContext.MotorLE.Base.Filiais.CodigoFilial : "000",
                        TipoLancamento = DocumentoOrigem.TipoLancamento,
                        Tipodoc = tipoDocumentoFecho,
                        Entidade = "",
                        TipoEntidade = "",
                        Serie = Convert.ToDateTime(DocumentoOrigem.DataDoc).Year.ToString(), // BSO.Base.Series.DaSerieDefeito("B", Documento.TipoDoc, Documento.Data),
                        Moeda = DocumentoOrigem.Moeda,
                        ModuloOrigem = "B",
                        ContaOrigem = DocumentoFecho[0].Caixa,
                        ContaDestino = "",
                        Data = Convert.ToDateTime(DocumentoOrigem.DataDoc),
                        DataIntroducao = Convert.ToDateTime(DocumentoOrigem.DataDoc),

                        Cambio = DocumentoOrigem.Cambio,
                        CambioMBase = DocumentoOrigem.CambioMBase,
                        CambioMAlt = DocumentoOrigem.CambioMAlt,

                    };

                    if (natureza=="C")
                    {
                        DocumentoTes.TotalDebito = 0;
                        DocumentoTes.TotalCredito = DocumentoFecho.Sum(x => x.Valor);
                    }else
                    {
                        DocumentoTes.TotalDebito = DocumentoFecho.Sum(x => Math.Abs(x.Valor));
                        DocumentoTes.TotalCredito = 0;
                    }

                    foreach (FechoCaixa Linha in DocumentoFecho)
                    {
                        LinhaTes = new TesBELinhaDocTesouraria
                        {
                            Entidade = "",
                            TipoEntidade = "",
                            DataMovimento = DocumentoOrigem.DataDoc,
                            DataValor = DocumentoOrigem.DataDoc,
                            MovimentoBancario = Linha.TipoPagamento,
                            Conta = Linha.Caixa,
                            Moeda = DocumentoOrigem.Moeda,
                            Cambio = DocumentoOrigem.Cambio,
                            CambioMBase = DocumentoOrigem.CambioMBase,
                            CambioMAlt = DocumentoOrigem.CambioMAlt,
                            Descricao = Linha.TipoPagamento,
                            Natureza = natureza,
                            Debito = 0,
                            Credito = Linha.Valor,
                            AnaliticaCBL = "",
                            CCustoCBL = ""
                        };

                        if (natureza == "C")
                        {
                            LinhaTes.Debito = 0;
                            LinhaTes.Credito = Linha.Valor;
                            LinhaTes.MovimentoBancario = $"{Linha.TipoPagamento}";
                        }
                        else
                        {
                            LinhaTes.Debito = Math.Abs(Linha.Valor);
                            LinhaTes.Credito = 0;
                            LinhaTes.MovimentoBancario = $"{Linha.TipoPagamento}D";
                        }


                        DocumentoTes.Linhas.Insere(LinhaTes);
                    }

                    string erro = "";

                    if (!ProductContext.MotorLE.Tesouraria.Documentos.ValidaActualizacao(DocumentoTes, ref erro))
                        return (false, $"Documento de Fecho de Caixa com erro {erro}");

                    return (true, "Documento de Fecho de Caixa criado com sucesso.");
                }

                return (false, $"Documento de Fecho de Caixa inválido.");

            }
            catch (Exception ex)
            {

                return (false, $"Erro ao gerar documento de tesouraria com origem em {DocumentoOrigem.Documento} {ex.Message}");
            }
            finally
            {
                DocumentoTes = null;
                LinhaTes = null;

            }

        }

    }
}
