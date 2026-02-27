using BasBE100;
using CblBE100;
using CmpBE100;
using ErpBS100;
using MafrecalApiV10.Models;
using NLog;
using Primavera.WebAPI.Integration;
using StdBE100;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                    //linhaDoc.Desconto1 = linha.Desconto1;
                    //linhaDoc.Desconto2 = linha.Desconto2;
                    //linhaDoc.Desconto3 = linha.Desconto3;
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


                resultado = IntegraMovimentoContabilidade(docVenda);

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


        public static (bool sucesso, string mensagem) IntegraMovimentoContabilidade(VndBEDocumentoVenda DocumentoOrigem )
        {
 
            if (DocumentoOrigem.TotalDocumento == 0 || DocumentoOrigem.CBLEstado == 1)
                return (true, "Documento sem movimento ou já integrado.");

            string aviso = string.Empty;

            //if (!ProductContext.MotorLE.Base.LigacaoCBL.BDDocLigaCBL("V", DocumentoOrigem.Tipodoc))
            //    return (false, $"O tipo de documento {DocumentoOrigem.Tipodoc} não está configurado para integrar na contabilidade.");


            if (!ProductContext.MotorLE.Base.LigacaoCBL.IntegraDocumentoLogCBL("V",
                                                  DocumentoOrigem.Tipodoc,
                                                  DocumentoOrigem.Serie,
                                                  DocumentoOrigem.NumDoc,
                                                  DocumentoOrigem.Filial,
                                                  -1,
                                                  ref aviso,
                                                  true))
            {
            
                return (false, $"Não vou possível integrar o documento {DocumentoOrigem.Tipodoc} {DocumentoOrigem.Serie}/{DocumentoOrigem.NumDoc} na contabilidade. ");
            }


            //var resultado =  CorrigeMovimentoContabilidade(DocumentoOrigem);
            //if (!resultado.sucesso)
            //{
            //  return (false, resultado.mensagem);
            //}

            return (true, "Documento sem movimento ou já integrado.");

        }

 
        public static (bool sucesso, string mensagem) CorrigeMovimentoContabilidade(VndBEDocumentoVenda DocumentoOrigem)
        {
            CblBEDocumento DocumentoCBL = null;

            try
            {
 
            DocumentoCBL = new CblBEDocumento();
            DocumentoCBL = (CblBEDocumento)ProductContext.MotorLE.Base.LigacaoCBL.DescodificaDocumentoLog("V",
                                                                              DocumentoOrigem.Tipodoc,
                                                                              DocumentoOrigem.Serie,
                                                                              DocumentoOrigem.NumDoc,
                                                                              DocumentoOrigem.Filial,
                                                                                          -1);

             string contaBase = "7111112";

             decimal debito = DocumentoCBL.LinhasGeral
            .Where(l => l.Natureza == "D")
            .Sum(l => l.Valor);

            decimal credito = DocumentoCBL.LinhasGeral
                .Where(l => l.Natureza == "C")
                .Sum(l => l.Valor);

            decimal balance = 0;
            balance = credito - debito;

             Logger.Warn($"Balance: {balance}");

         if (balance == 0)
                return (true, "");

            CblBELinhaDocGeral Linha = DocumentoCBL.LinhasGeral.FirstOrDefault(l => l.Conta == contaBase);

            if (Linha == null)
                return (false, $"Conta {contaBase} não encontrada.");

            decimal valorFinal = Linha.Valor - balance;
            string avisos = string.Empty;

            DocumentoCBL.LinhasGeral.GetEditaID(Linha.ID).Valor = valorFinal;
            DocumentoCBL.LinhasGeral.GetEditaID(Linha.ID).ValorAlt = valorFinal;
            DocumentoCBL.LinhasGeral.GetEditaID(Linha.ID).ValorOrigem = valorFinal;
             DocumentoCBL.Rascunho = false;


           VndBEDocumentoVenda DocumentoVenda;

            if (ProductContext.MotorLE.Base.LigacaoCBL.IntegraDocumentoCBL(DocumentoCBL, "000", ref avisos))
            {
                DocumentoVenda = ProductContext.MotorLE.Vendas.Documentos.EditaID(DocumentoOrigem.ID);
                DocumentoVenda.CBLEstado = 1;
                DocumentoVenda.CBLDiario = DocumentoCBL.Diario;
                DocumentoVenda.CBLNumDiario = DocumentoCBL.NumDiario;
                DocumentoVenda.IDCabecMovCbl = DocumentoCBL.ID;
                DocumentoVenda.CBLAno = DocumentoCBL.Ano;

                ProductContext.MotorLE.Vendas.Documentos.Actualiza(DocumentoVenda);

                if (avisos.Length > 0)
                    return (true, avisos);
            }
 
            return (true, "Documento sem movimento ou já integrado.");

            }
            catch (Exception ex)
            {

                return (false, ex.Message);
            }

        }


        //private static bool CorrigeMovimentoContabilidade2(ErpBS MotorPrimavera,
        //                                      VndBEDocumentoVenda Documento)
        //{
        //    string diario = string.Empty;
        //    string tipoLancamento = string.Empty;
        //    string natureza = string.Empty;
        //    CblBELinhaDocGeral Linha = null;
        //    CblBELinhaDocCentros LinhaCC = null;
        //    CblBEDocumento DocumentoCBL = null;
        //    VndBEDocumentoVenda DocumentoComercial = null;


        //    try
        //    {
        //        string documento = Documento.Tipodoc + " Nº " + Documento.NumDoc + "/" + Documento.Serie;
        //        #region CBL
        //        DocumentoCBL = new CblBEDocumento();
        //        DocumentoCBL = (CblBEDocumento)MotorPrimavera.Base.LigacaoCBL.DescodificaDocumentoLog("V",
        //                                                                          Documento.Tipodoc,
        //                                                                          Documento.Serie,
        //                                                                          Documento.NumDoc,
        //                                                                          Documento.Filial,
        //                                                                                      -1);








        //        decimal credit = 0;
        //        decimal debit = 0;
        //        decimal balance = 0;
        //        decimal total = 0;
        //        string localOperacao = string.Empty;
        //        string contaBaseDebito = "68888";
        //        string analiticaDebito = "916888801";
        //        string analiticaInversaDebito = "918116";
        //        string contaBaseCredito = "7888";
        //        string analiticaCredito = "91788801";
        //        string analiticaInversaCredito = "918117";
        //        int lote = 0;
        //        bool isBalancePositive = false;


        //        foreach (CblBELinhaDocGeral linha in DocumentoCBL.LinhasGeral)
        //        {
        //            if (linha.Natureza == "D")
        //            {
        //                debit += linha.Valor;
        //            }
        //            if (linha.Natureza == "C")
        //            {
        //                credit += linha.Valor;
        //            }
        //            if (lote != linha.Lote)
        //            {
        //                lote = linha.Lote + 1;
        //            }
        //        }

        //        foreach (CblBELinhaDocGeral LinhasContabilidade in DocumentoCBL.LinhasGeral)
        //        {
        //            if (LinhasContabilidade.TipoEntidade == "C")
        //            {
        //                localOperacao = LinhasContabilidade.LocalOperacao;
        //                break;
        //            }

        //        }
        //        // 7121 e da 7111 para as faturas e da 71711 e 71722
        //        //CblBELinhaDocGeral LinhaContabilidade;
        //        //foreach (CblBELinhaDocGeral LinhasContabilidade in DocumentoCBL.LinhasGeral)
        //        //{
        //        //    if (LinhasContabilidade.get_Conta() == "71721")
        //        //    {
        //        //        LinhaContabilidade = LinhasContabilidade;
        //        //        //IdLinha = LinhasContabilidade.get_ID();
        //        //        break;
        //        //    }
        //        //}
        //        CblBELinhaDocCentros LinhaCentros = new CblBELinhaDocCentros();
        //        foreach (CblBELinhaDocCentros LinhasCentros in DocumentoCBL.LinhasCentros)
        //        {
        //            if (Documento.Tipodoc == "FT" || Documento.Tipodoc == "FS")
        //            {
        //                if ((LinhasCentros.ContaOrigem == "7111"
        //                    || LinhasCentros.ContaOrigem == "7112"
        //                    || LinhasCentros.ContaOrigem == "7113"
        //                    || LinhasCentros.ContaOrigem == "7121"
        //                    || LinhasCentros.ContaOrigem == "7122"
        //                    || LinhasCentros.ContaOrigem == "7123"
        //                    ) && LinhasCentros.TipoLinha == "O")
        //                {
        //                    LinhaCentros = LinhasCentros;
        //                    //IdLinha = LinhasContabilidade.get_ID();
        //                    break;
        //                }
        //            }
        //            if (Documento.Tipodoc == "NC")
        //            {
        //                if ((LinhasCentros.ContaOrigem == "71711"
        //                    || LinhasCentros.ContaOrigem == "71712"
        //                    || LinhasCentros.ContaOrigem == "71713"
        //                    || LinhasCentros.ContaOrigem == "71721"
        //                    || LinhasCentros.ContaOrigem == "71722"
        //                    || LinhasCentros.ContaOrigem == "71723") && LinhasCentros.TipoLinha == "O")
        //                {
        //                    LinhaCentros = LinhasCentros;
        //                    //IdLinha = LinhasContabilidade.get_ID();
        //                    break;
        //                }
        //            }
        //        }
        //        //if (string.IsNullOrEmpty(IdLinha))
        //        //{
        //        //    Logger.Warn("{0} - A linha da entidade não foi encontrada para o documento {1}", MethodBase.GetCurrentMethod().Name, Documento.get_Tipodoc() + Documento.get_Serie() + Documento.get_NumDoc());
        //        //    return false;
        //        //}
        //        //Linha = DocumentoCBL.LinhasGeral.EditaID[IdLinha];
        //        balance = credit - debit;

        //        if (balance >= 0)
        //        {
        //            isBalancePositive = true;
        //            natureza = "D";
        //        }
        //        else
        //        {
        //            natureza = "C";
        //        }

        //        switch (Documento.Tipodoc)
        //        {
        //            case "FT":
        //                //     natureza = "D";
        //                //   total = Linha.get_Valor() + balance;
        //                break;
        //            case "FS":
        //                //     natureza = "D";
        //                //  total = Linha.get_Valor() + balance;
        //                break;
        //            case "NC":
        //                //     natureza = "C";
        //                // total = Linha.get_Valor() - balance;
        //                break;
        //        }

        //        if (balance != 0)
        //        {
        //            balance = System.Math.Abs(balance);
        //            //Linha.set_Valor(total);
        //            //Linha.set_ValorAlt(total);
        //            //Linha.set_ValorOrigem(total);
        //            //Linha.set_ValorIncIVA(total);
        //            //Linha.set_ValorIncIVAAlt(total);
        //            //Linha.set_ValorIncIVAOrigem(total);
        //            Linha = new CblBELinhaDocGeral();
        //            Linha.TipoLinha = ("F");
        //            Linha.Lote = (Convert.ToInt16(lote));
        //            Linha.TipoOperacao = (0);
        //            Linha.LocalOperacao = (localOperacao);

        //            if (isBalancePositive)
        //            {
        //                Linha.Conta = (contaBaseDebito);
        //            }
        //            else
        //            {
        //                Linha.Conta = (contaBaseCredito);
        //            }


        //            Linha.Natureza = (natureza);
        //            Linha.Moeda = (DocumentoCBL.Moeda);
        //            //  Linha.set_TipoEntidade("F");
        //            //Linha.set_Entidade("F");
        //            Linha.Valor = (Convert.ToDecimal(balance));
        //            Linha.ValorAlt = (Convert.ToDecimal(balance));
        //            Linha.ValorOrigem = (Convert.ToDecimal(balance));
        //            Linha.ValorIncIVA = (balance);
        //            Linha.ValorIncIVAAlt = (balance);
        //            Linha.ValorIncIVAOrigem = (balance);
        //            Linha.Cambio = (1);
        //            Linha.CambioMAlt = (1);
        //            Linha.CambioOrigem = (1);
        //            Linha.Descricao = (documento);
        //            //   Linha.set_ReflexaoAnalitica(true);
        //            DocumentoCBL.LinhasGeral.Insere(Linha);
        //            ///
        //            Linha = new CblBELinhaDocGeral();
        //            Linha.TipoLinha = ("A");
        //            Linha.Lote = (Convert.ToInt16(lote));
        //            // Linha.set_LocalOperacao(localOperacao);
        //            Linha.TipoOperacao = (0);
        //            if (isBalancePositive)
        //            {
        //                Linha.Conta = (analiticaDebito);
        //                Linha.ContaOrigem = (contaBaseDebito);
        //            }
        //            else
        //            {
        //                Linha.Conta = (analiticaCredito);
        //                Linha.ContaOrigem = (contaBaseCredito);
        //            }
        //            Linha.Natureza = (natureza);
        //            Linha.Moeda = (DocumentoCBL.Moeda);
        //            //  Liset_TipoEntidade("F");
        //            //Linht_Entidade("F");
        //            Linha.Valor = (Convert.ToDecimal(balance));
        //            Linha.ValorAlt = (Convert.ToDecimal(balance));
        //            Linha.ValorOrigem = (Convert.ToDecimal(balance));
        //            Linha.ValorIncIVA = (balance);
        //            Linha.ValorIncIVAAlt = (balance);
        //            Linha.ValorIncIVAOrigem = (balance);
        //            Linha.Cambio = (1);
        //            Linha.CambioMAlt = (1);
        //            Linha.CambioOrigem = (1);
        //            Linha.Descricao = (documento);
        //            Linha.ReflexaoAnalitica = (true);
        //            DocumentoCBL.LinhasGeral.Insere(Linha);
        //            ////
        //            Linha = new CblBELinhaDocGeral();
        //            Linha.Lote = (Convert.ToInt16(lote));
        //            Linha.TipoLinha = ("A");
        //            //    ha.set_LocalOperacao(localOperacao);
        //            Linha.TipoOperacao = (0);
        //            if (isBalancePositive)
        //            {
        //                Linha.Conta = (analiticaInversaDebito);
        //                Linha.ContaOrigem = (contaBaseDebito);
        //            }
        //            else
        //            {
        //                Linha.Conta = (analiticaInversaCredito);
        //                Linha.ContaOrigem = (contaBaseCredito);
        //            }
        //            Linha.Natureza = (natureza == "D" ? "C" : "D");
        //            Linha.Moeda = (DocumentoCBL.Moeda);
        //            //  Liset_TipoEntidade("F");
        //            //Linht_Entidade("F");
        //            Linha.Valor = (Convert.ToDecimal(balance));
        //            Linha.ValorAlt = (Convert.ToDecimal(balance));
        //            Linha.ValorOrigem = (Convert.ToDecimal(balance));
        //            Linha.ValorIncIVA = (balance);
        //            Linha.ValorIncIVAAlt = (balance);
        //            Linha.ValorIncIVAOrigem = (balance);
        //            Linha.Cambio = (1);
        //            Linha.CambioMAlt = (1);
        //            Linha.CambioOrigem = (1);
        //            Linha.Descricao = (documento);
        //            Linha.ReflexaoAnalitica = (true);
        //            DocumentoCBL.LinhasGeral.Insere(Linha);
        //            ///
        //            LinhaCC = new CblBELinhaDocCentros();
        //            LinhaCC.Lote = Convert.ToInt16(lote);
        //            LinhaCC.TipoLinha = ("O");
        //            LinhaCC.Centro = (LinhaCentros.Centro);
        //            if (isBalancePositive)
        //            {
        //                LinhaCC.ContaOrigem = (contaBaseDebito);
        //            }
        //            else
        //            {
        //                LinhaCC.ContaOrigem = (contaBaseCredito);
        //            }
        //            LinhaCC.Percentagem = (100);
        //            //    Linha.set_Linha("F");
        //            LinhaCC.Natureza = (natureza);
        //            LinhaCC.Moeda = (DocumentoCBL.Moeda);
        //            //  Linht_TipoEntidade("F");
        //            //Linha.Entidade("F");
        //            LinhaCC.Valor = (Convert.ToDecimal(balance));
        //            LinhaCC.ValorAlt = (Convert.ToDecimal(balance));
        //            LinhaCC.ValorOrigem = (Convert.ToDecimal(balance));
        //            LinhaCC.Cambio = (1);
        //            LinhaCC.CambioMAlt = (1);
        //            LinhaCC.CambioOrigem = (1);
        //            LinhaCC.Descricao = (documento);

        //            DocumentoCBL.LinhasCentros.Insere(LinhaCC);
        //            DocumentoCBL.Rascunho = false;

        //        }
        //        // CblBELinhasDocCentros linhasCentros = DocumentoCBL.LinhasCentros;
        //        //  DocumentoCBL.LinhasCentros.RemoveTodos();
        //        //foreach (CblBELinhaDocCentros linhasCentro in linhasCentros)
        //        //{
        //        //    CblBELinhaDocCentros beLinhaDocCentros = linhasCentro;
        //        //    ref CblBELinhaDocCentros local = ref beLinhaDocCentros;
        //        //    DocumentoCBL.LinhasCentros.Insere(ref local);
        //        //}
        //        //                MotorPrimavera.Contabilidade.Documentos.BalanceiaDiferencasArredondamento(DocumentoCBL);
        //        string avisos = "";

        //        if (MotorPrimavera.Base.LigacaoCBL.IntegraDocumentoCBL(DocumentoCBL, "000", ref avisos))
        //        {
        //            Logger.Warn("{0} - Documento corrigido na CBL {1}", MethodBase.GetCurrentMethod().Name, Documento.Tipodoc
        //                + Documento.Serie
        //                + Documento.NumDoc);
        //            DocumentoComercial = MotorPrimavera.Vendas.Documentos.EditaID(Documento.ID);
        //            DocumentoComercial.CBLEstado = 1;
        //            DocumentoComercial.CBLDiario = DocumentoCBL.Diario;
        //            DocumentoComercial.CBLNumDiario = DocumentoCBL.NumDiario;
        //            DocumentoComercial.IDCabecMovCbl = DocumentoCBL.ID;
        //            DocumentoComercial.CBLAno = DocumentoCBL.Ano;
        //            MotorPrimavera.Vendas.Documentos.Actualiza(DocumentoComercial);

        //            if (avisos.Length > 0)
        //            {
        //                Logger.Warn("{0} - Erro no documento {1}", MethodBase.GetCurrentMethod().Name, Documento.Tipodoc
        //                    + Documento.Serie
        //                    + Documento.NumDoc);
        //                return false;
        //            }
        //        }
        //        #endregion
        //        return true;
        //    }
        //    catch (Exception e)
        //    {
        //        Logger.Error("{0} - Erro no documento {1} ", MethodBase.GetCurrentMethod().Name, Documento.Tipodoc
        //                    + Documento.Serie
        //                    + Documento.NumDoc
        //        + "\n" +
        //         e.ToString());
        //        throw new Exception(e.Message);
        //        return false;
        //    }
        //    finally
        //    {

        //        Helper.ReleaseCom(DocumentoComercial);
        //        Helper.ReleaseCom(Linha);
        //        Helper.ReleaseCom(LinhaCC);
        //        Helper.ReleaseCom(DocumentoCBL);

        //        DocumentoCBL = null;
        //        Linha = null;
        //        LinhaCC = null;
        //        DocumentoComercial = null;

        //        GC.Collect();
        //        GC.WaitForPendingFinalizers();
        //    }
        //}




    }
}
