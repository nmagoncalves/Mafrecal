using BasBE100;
using CmpBE100;
using IntBE100;
using MafrecalApiV10.Models;
using NLog;
using Primavera.WebAPI.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace MafrecalApiV10.MafrecalApi
{
    [RoutePrefix("MafrecalApi")]
    public class InternosApiController : ApiController
    {


        private string AppName = Assembly.GetExecutingAssembly().GetName().Name;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [Route("Internos/Docs/CreateDocument")]
 
        public IHttpActionResult PostIntern([FromBody] DocumentoInterno Interno)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
 
                IntBEDocumentoInterno DocInterno = new IntBEDocumentoInterno()
                {
                    Tipodoc = Interno.TipoDoc,
                    Serie = Interno.Serie,
             
                };

                ProductContext.MotorLE.Internos.Documentos.PreencheDadosRelacionados(DocInterno);
 
                DocInterno.Data = Convert.ToDateTime(Interno.DataDoc);
                DocInterno.DataVencimento = Convert.ToDateTime(Interno.DataVenc);
  
                double totalMercadoria = 0;

                foreach (var linha in Interno.Linhas)
                {
                    if (!ProductContext.MotorLE.Base.Iva.Existe(linha.Iva) &&
                        !Helper.NovaTaxaIva(linha.Iva, linha.TaxaIva))
                    {
                        Logger.Warn($"{AppName} {MethodBase.GetCurrentMethod().Name} Não foi possível criar a taxa de iva {linha.Iva}");
                        return BadRequest($"Erro na taxa de IVA {linha.Iva}");
                    }

                    if (!ProductContext.MotorLE.Base.Artigos.Existe(linha.Artigo))
                    {
                        var resultado = Helper.NovoItem(new Item
                        {
                            Codigo = linha.Artigo,
                            Descricao = linha.Descricao,
                            Iva = linha.Iva,
                            TaxaIva = linha.TaxaIva,
                            PVP1 = linha.PrecUnit,
                            Armazem = linha.Armazem,
                
                        });

                        if (!resultado.Sucesso)
                        {
                            return BadRequest($"{MethodBase.GetCurrentMethod().Name} - {resultado.Mensagem}.");
                        }
                    }

                    ProductContext.MotorLE.Internos.Documentos.AdicionaLinha(
                        DocInterno,
                        linha.Artigo,
                        Armazem: linha.Armazem,
                        PrecoUnitario: linha.PrecUnit, 
                        Quantidade: linha.Quantidade
 
                    );
 
                    var linhaDoc = DocInterno.Linhas.Cast<IntBELinhaDocumentoInterno>().Last();

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

                    linhaDoc.CodigoIva = codIva;
                    linhaDoc.TaxaIva = taxaIva;
                    linhaDoc.PercIvaDedutivel = 100;
                    linhaDoc.PercIncidenciaIVA = 100;
                    linhaDoc.PrecoLiquido = linha.TotalLiquido;
                    linhaDoc.TotalIliquido = linha.PrecUnit * linha.Quantidade;

                    totalMercadoria += linhaDoc.TotalIliquido;
                }

                if (Interno.ResumoIVA != null)
                {
                    foreach (var resumo in Interno.ResumoIVA)
                    {
                        DocInterno.ResumoIva.Insere(new BasBEResumoIva
                        {
                            Modulo = "I",
                            Tipodoc = Interno.TipoDoc.ToString(),
                            NumDoc = Convert.ToInt32(Interno.NumDoc),
                            Serie = Interno.Serie.ToString(),
                            Filial = "000",
                            CodIva = Helper.DaCodIva(resumo.TaxaIva.ToString()),
                            TaxaIva = resumo.TaxaIva,
                            Incidencia = resumo.ValorIncidencia,
                            Valor = resumo.ValorTotal
                        });
                    }
                }

                Interno.TotalMerc = totalMercadoria;
                Interno.TotalIva = Interno.TotalIva;
                Interno.TotalDocumento = Interno.TotalDocumento;
                Interno.TotalDesc = Interno.TotalDesc;

                string strAvisos = string.Empty;

                ProductContext.MotorLE.Internos.Documentos.Actualiza(DocInterno, ref strAvisos);    

                return Ok();
            }
            catch (Exception ex)
            {
                Logger.Warn($"{AppName} {MethodBase.GetCurrentMethod().Name} {ex}");
                return BadRequest($"{MethodBase.GetCurrentMethod().Name} - {ex.Message}.");
            }
        }
 
    }
}
