
using BasBE100;
using CblBE100;
using InvBE100;
using MafrecalApiV10.Models;
using Microsoft.CSharp.RuntimeBinder;
using NLog;
using Primavera.WebAPI.Integration;
using StdBE100;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;


namespace MafrecalApiV10
{
    public static class Helper
    {
        private static string AppName = Assembly.GetExecutingAssembly().GetName().Name;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static (bool Sucesso, string Mensagem) NovoFornecedor(Fornecedor fornecedor)
        {
            try
            {
                BasBEFornecedor basBeFornecedor = new BasBEFornecedor();

                string fornecedorExistente =
                    ProductContext.MotorLE.Base.Fornecedores.ExisteContribuinte(fornecedor.NumContribuinte);

                if (!string.IsNullOrEmpty(fornecedorExistente))
                {
                    return (true, "Já existe um fornecedor com o mesmo número de contribuinte.");
                }

                basBeFornecedor.Fornecedor = fornecedor.Codigo;
                basBeFornecedor.Nome = string.IsNullOrWhiteSpace(fornecedor.Nome) ? "SEM NOME" : fornecedor.Nome;
                basBeFornecedor.EnderecoWeb = fornecedor.EnderecoWeb;
                basBeFornecedor.Email = fornecedor.Email ?? "";
                basBeFornecedor.NumContribuinte = fornecedor.NumContribuinte;
                basBeFornecedor.Telefone = fornecedor.Telefone ?? "";
                basBeFornecedor.Fax = fornecedor.Fax;
                basBeFornecedor.Morada = string.IsNullOrWhiteSpace(fornecedor.Morada) ? "SEM MORADA" : fornecedor.Morada;
                basBeFornecedor.CodigoPostal = fornecedor.CodigoPostal;
                basBeFornecedor.LocalidadeCodigoPostal = fornecedor.LocalidadeCodigoPostal;
                basBeFornecedor.Localidade = fornecedor.LocalidadeCodigoPostal;
                basBeFornecedor.Moeda = fornecedor.Moeda ?? "EUR";
                basBeFornecedor.CondPag = fornecedor.CondPagamento;
                basBeFornecedor.Pais = fornecedor.Pais ?? "PT";
                basBeFornecedor.LocalOperacao = fornecedor.LocalOperacao;
                basBeFornecedor.SegmentoTerceiro = "001";

                ProductContext.MotorLE.Base.Fornecedores.Actualiza(basBeFornecedor);

                Helper.NovaLigacaoCBL(basBeFornecedor.Fornecedor, basBeFornecedor.Nome , DateTime.Now.Year,2, "F");

                return (true, "");
            }
            catch (Exception ex)
            {
                Helper.Logger.Warn(
                    Helper.AppName + " " +
                    MethodBase.GetCurrentMethod().Name + " " +
                    fornecedor.Codigo + " " +
                    ex.ToString()
                );

                return (false, ex.Message.ToString());
            }
        }
 
        public static (bool Sucesso, string Mensagem) NovoItem(Item item)
        {
            try
            {
                var artigo = new BasBEArtigo();

                //// Família
                if (!string.IsNullOrEmpty(item.Familia) &&
                    !ProductContext.MotorLE.Base.Familias.Existe(item.Familia) &&
                    !NovaFamilia(item.Familia, item.FamiliaDesc))
                {
                    return (false, $"Erro ao criar Família {item.Familia}");
                }

                //// Subfamília
                if (!string.IsNullOrEmpty(item.SubFamilia) &&
                    !ProductContext.MotorLE.Base.Familias.ExisteSubFamilia(item.Familia, item.SubFamilia) &&
                    !NovaSubFamilia(item.Familia, item.FamiliaDesc, item.SubFamilia, item.SubFamiliaDesc))
                {
                    return (false, $"Erro ao criar SubFamília {item.SubFamilia}");
                }

                // IVA
                if (!float.TryParse(item.Iva, out var taxaIva))
                {
                    return (false, $"Taxa de IVA inválida: {item.Iva}");
                }

                if (!ProductContext.MotorLE.Base.Iva.Existe(item.Iva) &&
                    !NovaTaxaIva(item.Iva, taxaIva))
                {
                    return (false, $"Erro ao criar taxa de IVA {item.Iva}");
                }

                // Marca
                //if (!string.IsNullOrEmpty(item.Marca) &&
                //    !ProductContext.MotorLE.Base.Marcas.Existe(item.Marca) &&
                //    !NovaMarca(item.Marca, string.Empty))
                //{
                //    return (false, $"Erro ao criar Marca {item.Marca}");
                //}

                // Artigo
                if (!ProductContext.MotorLE.Base.Artigos.Existe(item.Codigo))
                {
                    artigo.Artigo = item.Codigo;
                    artigo.Descricao = item.Descricao;
                    artigo.Caracteristicas = item.Caracteristicas;
                    artigo.CodBarras = item.CodBarras;
                    artigo.IVA = item.Iva;
                    artigo.Marca = item.Marca;
                   // artigo.Familia = item.Familia;
                    artigo.SujeitoDevolucao = true;
                  


                    artigo.UnidadeBase = item.UnidadeBase;
                    artigo.UnidadeCompra = item.UnidadeBase;
                    artigo.UnidadeVenda = item.UnidadeBase;
                    artigo.UnidadeSaida = item.UnidadeBase;

                    artigo.MovStock = "N";
                    artigo.ArmazemSugestao = item.Armazem;
                    artigo.DeduzIVA = false;
                    artigo.PercIncidenciaIVA = 100;
                    artigo.PercIvaDedutivel = 100;
                }
                else
                {
                    artigo = ProductContext.MotorLE.Base.Artigos.Edita(item.Codigo);
                    artigo.EmModoEdicao = true;

                    artigo.Caracteristicas = item.Caracteristicas;
                    artigo.CodBarras = item.CodBarras;
                    artigo.IVA = item.Iva;
                    artigo.Marca = item.Marca;
                    //  artigo.Familia = item.Familia;
                    artigo.SujeitoDevolucao = true;

                    if (!ProductContext.MotorLE.Base.Artigos
                            .ExistemDocumentosCertificadosArtigo(item.Codigo))
                    {
                        artigo.Descricao = item.Descricao;
                    }

                    if (ProductContext.MotorLE.Base.Artigos
                            .ExistemMovimentos(item.Codigo, "CVSN") <= 0)
                    {
                        artigo.UnidadeBase = item.UnidadeBase;
                    }
                }

                ProductContext.MotorLE.Base.Artigos.Actualiza(artigo);

                // Preços
                BasBEArtigoMoeda artigoMoeda;

                if (!ProductContext.MotorLE.Base.ArtigosPrecos
                        .Existe(item.Codigo, "EUR", item.UnidadeBase))
                {
                    artigoMoeda = new BasBEArtigoMoeda
                    {
                        Artigo = item.Codigo,
                        Moeda = "EUR",
                        Unidade = item.UnidadeBase,
                        PVP1 = item.PVP1,
                        PVP2 = item.PVP2,
                        PVP3 = item.PVP3
                    };
                }
                else
                {
                    artigoMoeda = ProductContext.MotorLE.Base.ArtigosPrecos
                        .Edita(item.Codigo, "EUR", item.UnidadeBase);

                    artigoMoeda.EmModoEdicao = artigo.EmModoEdicao;
                    artigoMoeda.PVP1 = item.PVP1;
                    artigoMoeda.PVP2 = item.PVP2;
                    artigoMoeda.PVP3 = item.PVP3;
                }

                ProductContext.MotorLE.Base.ArtigosPrecos.Actualiza(artigoMoeda);

                return (true, "Item criado/atualizado com sucesso.");
            }
            catch (Exception ex)
            {
                Logger.Warn($"{AppName} {MethodBase.GetCurrentMethod().Name} {ex} - {item?.Codigo}");
                return (false, $"Ocorreu um erro ao criar/atualizar o item. {ex.Message.ToString()}");
            }
        }

        public static (bool Sucesso, string Mensagem) CondPagamento(Models.CondPagamento condPagamento)
        {
            try
            {
                if (ProductContext.MotorLE.Base.CondsPagamento.Existe(condPagamento.Codigo))
                {
                    return (true, "Condição de pagamento já existente.");
                }

                var mensagem = string.Empty;

                var entidade = new BasBECondPagamento
                {
                    CondPag = condPagamento.Codigo,
                    Descricao = condPagamento.Descricao,
                    Meses30Dias = true,
                    DiasVencimento = condPagamento.Dias
                };

                if (!ProductContext.MotorLE.Base.CondsPagamento
                        .ValidaActualizacao(entidade, ref mensagem))
                {
                    return (false, mensagem);
                }

                ProductContext.MotorLE.Base.CondsPagamento
                    .Actualiza(entidade, string.Empty);

                return (true, "Condição de pagamento criada com sucesso.");
            }
            catch (Exception ex)
            {
                Logger.Warn(
                    $"{AppName} {MethodBase.GetCurrentMethod().Name} {ex}"
                );

                return (false, $"Ocorreu um erro ao criar a condição de pagamento. {ex.Message.ToString()}");
            }
        }

        public static string DaCodIva(string taxaIva)
        {
            try
            {
                var lista = ProductContext.MotorLE.Consulta(
                    $"SELECT Iva FROM Iva WITH (NOLOCK) WHERE Taxa = {taxaIva}"
                );

                if (lista == null || lista.NumLinhas() <= 0)
                {
                    return string.Empty;
                }

                return lista.Valor("Iva")?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                Logger.Warn($"{AppName} {MethodBase.GetCurrentMethod().Name} {ex}");
                return string.Empty;
            }
        }

        public static bool NovaLigacaoCBL(string codigo, string nomeEntidade, int ano, int tabela, string tipoEndidade)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigo))
                {
                    return false;
                }

                string conta;

                if (tabela==1)
                {
                    conta = ProductContext.MotorLE.Base.Clientes.DaProximoNumeroCBL();
                  
                }
                else
                {
                    conta = ProductContext.MotorLE.Base.Fornecedores.DaProximoNumeroCBL();
                }


                    var lista = ProductContext.MotorLE.Consulta(
                        $"SELECT Entidade FROM dbo.CnfTabLigCBL WITH (NOLOCK) " +
                        $"WHERE Tabela ={tabela} AND Ano = {ano} AND Plano = '001' AND Entidade = '{codigo}'"
                    );

                if (!lista.Vazia())
                    return true;
               

                //if (lista.NumLinhas() > 0)
                //{
                //    var sqlUpdate =
                //        "UPDATE dbo.CnfTabLigCBL SET Conta = '" + conta + "' " +
                //        $"WHERE Tabela = {tabela} AND Coluna = 1 AND Ano = {ano} " +
                //        $"AND Plano = '001' AND Entidade = '{codigo}'";

                //    return ProductContext.MotorLE.DSO.ExecuteSQL(sqlUpdate) == -1;
                //}

                var sqlInsert =
                    "INSERT INTO dbo.CnfTabLigCBL " +
                    "([Id],[Tabela],[Ano],[Plano],[Entidade],[Coluna],[Conta]) " +
                    $"VALUES (NEWID(), {tabela}, '{ano}', '001', '{codigo}', 1, '{conta}')";

                var resultado = ProductContext.MotorLE.DSO.ExecuteSQL(sqlInsert);

                Logger.Warn($"LIGAÇÃO CBL CRIADA {codigo}");

                NovaContaCBL(conta, tipoEndidade, nomeEntidade, ano);

                return resultado == -1;
            }
            catch (Exception ex)
            {
                Logger.Warn(
                    $"{AppName} {MethodBase.GetCurrentMethod().Name} {ex}"
                );
                return false;
            }
        }

        private static bool NovaContaCBL(string codigo, string tipoEntidade, string nomeEntidade, int ano)
        {

            try
            {
                string contaBase = "21111";
                if (tipoEntidade == "F")
                {
                    contaBase = "22111";
                }
                string contaComposta = $"{contaBase}{codigo}";

                if (ProductContext.MotorLE.Contabilidade.PlanoContas.Existe(ano, contaComposta))
                    return true;

                CblBEConta NovaConta = new CblBEConta();

                NovaConta.Conta = contaComposta;
                NovaConta.Descricao = nomeEntidade;
                NovaConta.TipoEntidade = tipoEntidade;
                NovaConta.TipoConta = "M";

                string avisos = string.Empty;

                ProductContext.MotorLE.Contabilidade.PlanoContas.Actualiza(NovaConta, ref avisos);

                Logger.Warn($"CONTA CBL CRIADA {codigo}");

                return true;
            }
            catch (Exception ex)
            {

                Logger.Warn(
                     $"{AppName} {MethodBase.GetCurrentMethod().Name} {ex}"
                    );
                return false;
            }
        }

        public static bool NovaTaxaIva(string codigo, float taxa)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigo))
                {
                    Logger.Warn(
                        $"{MethodBase.GetCurrentMethod().Name} Valores não preenchidos corretamente ou em falta."
                    );
                    return false;
                }

                if (ProductContext.MotorLE.Base.Iva.Existe(codigo))
                {
                    return true;
                }

                var iva = new BasBEIva
                {
                    IVA = codigo,
                    Taxa = taxa,
                    Descricao = $"IVA à taxa legal de {taxa}%"
                };

                if (taxa == 0)
                {
                    iva.CodigoMotivoIsencao = "M05";
                    iva.MotivoIsencao = "Isento Artigo 14º do CIVA (ou similar)";
                }

                var mensagem = string.Empty;

                if (ProductContext.MotorLE.Base.Iva.ValidaActualizacao(iva, ref mensagem))
                {
                    ProductContext.MotorLE.Base.Iva.Actualiza(iva, string.Empty);
                    return true;
                }

                Logger.Warn(
                    $"{AppName} {MethodBase.GetCurrentMethod().Name} Erro ao adicionar nova Taxa de IVA {mensagem}"
                );
                return false;
            }
            catch (Exception ex)
            {
                Logger.Warn(
                    $"{AppName} {MethodBase.GetCurrentMethod().Name} {ex}"
                );
                return false;
            }
        }

        public static bool NovaMarca(string marca, string descricao)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(marca))
                {
                    return false;
                }

                if (ProductContext.MotorLE.Base.Marcas.Existe(marca))
                {
                    return true;
                }

                var entidade = new BasBEMarca
                {
                    Marca = marca.Left(10),
                    Descricao = descricao
                };

                var mensagem = string.Empty;

                if (ProductContext.MotorLE.Base.Marcas
                    .ValidaActualizacao(entidade, ref mensagem))
                {
                    ProductContext.MotorLE.Base.Marcas.Actualiza(ref entidade);
                    return true;
                }

                Logger.Warn(
                    $"{AppName} {MethodBase.GetCurrentMethod().Name} {mensagem}"
                );
                return false;
            }
            catch (Exception ex)
            {
                Logger.Warn(
                    $"{AppName} {MethodBase.GetCurrentMethod().Name} {ex}"
                );
                return false;
            }
        }
        public static bool NovaFamilia(string familia, string descricao)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(familia))
                {
                    return false;
                }

                if (ProductContext.MotorLE.Base.Familias.Existe(familia))
                {
                    return true;
                }

                var entidade = new BasBEFamilia
                {
                    Familia = familia.Left(10),
                    Descricao = descricao?.Left(10)
                };

                var mensagem = string.Empty;

                if (ProductContext.MotorLE.Base.Familias
                    .ValidaActualizacao(entidade, ref mensagem))
                {
                    ProductContext.MotorLE.Base.Familias.Actualiza(entidade, string.Empty);
                    return true;
                }

                Logger.Warn(
                    $"{AppName} {MethodBase.GetCurrentMethod().Name} " +
                    $"Erro ao adicionar nova Família {mensagem}"
                );

                return false;
            }
            catch (Exception ex)
            {
                Logger.Warn(
                    $"{AppName} {MethodBase.GetCurrentMethod().Name} {ex}"
                );
                return false;
            }
        }

        public static bool NovaSubFamilia(
            string familia,
            string familiaDesc,
            string subFamilia,
            string subFamiliaDesc)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(familia))
                {
                    return false;
                }

                if (ProductContext.MotorLE.Base.Familias
                    .ExisteSubFamilia(familia, subFamilia))
                {
                    return true;
                }

                var subFamiliaEntity = new BasBESubFamilia
                {
                    SubFamilia = subFamilia.Left(10),
                    Descricao = subFamiliaDesc?.Left(10)
                };

                ProductContext.MotorLE.Base.Familias
                    .ActualizaSubFamilias(familia, subFamiliaEntity, string.Empty);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn(
                    $"{AppName} {MethodBase.GetCurrentMethod().Name} {ex}"
                );
                return false;
            }
        }

        public static (bool Sucesso, string Mensagem) NovoCliente(Entidade o)
        {
            if (o == null || string.IsNullOrWhiteSpace(o.Codigo))
                return (false, $"Erro ao criar Cliente. Código vazio.");

            BasBECliente cliente = null;
            GenericPropertyFill<Entidade, BasBECliente> fillObject = null;
            string erro = string.Empty;

            try
            {
                cliente = new BasBECliente();

                fillObject = new GenericPropertyFill<Entidade, BasBECliente>
                {
                    ObjectoOrigem = o,
                    ObjectoDestino = cliente
                };
                fillObject.Fill();

                if (ProductContext.MotorLE.Base.Clientes.Existe(o.Codigo))
                    return (true, "");

                cliente.Cliente = o.Codigo;

                cliente.Nome = string.IsNullOrWhiteSpace(o.Nome)
                    ? "Nome desconhecido"
                    : Truncate(o.Nome, 50);

                cliente.CondPag = string.IsNullOrWhiteSpace(cliente.CondPag)
                 ? "1"
                 : cliente.CondPag;

                cliente.Moeda = string.IsNullOrWhiteSpace(cliente.Moeda)
                    ? ProductContext.MotorLE.Contexto.MoedaBase
                    : cliente.Moeda;

                cliente.NumContribuinte = string.IsNullOrWhiteSpace(cliente.NumContribuinte)
                    ? "999999990"
                    : Truncate(cliente.NumContribuinte, 9);
 
                if (!string.IsNullOrWhiteSpace(o.Morada))
                {
                    cliente.Morada = Truncate(o.Morada, 50);

                    if (o.Morada.Length > 50)
                        cliente.Morada2 = o.Morada.Substring(50);
                }

                var resultPais = NovoPais(cliente.Pais);

                cliente.LocalOperacao = o.LocalOperacao;
                cliente.SegmentoTerceiro = "001";

                if (!resultPais.Sucesso)
                    return (false, resultPais.Mensagem);


                if (!ProductContext.MotorLE.Base.Clientes.ValidaActualizacao(cliente, ref erro))
                    return (false, $"Erro ao criar Cliente {cliente.Cliente}.{erro}");
 

                ProductContext.MotorLE.Base.Clientes.Actualiza(cliente);

                Helper.NovaLigacaoCBL(cliente.Cliente, cliente.Nome, 2025, 1, "C");
 
                return (true,"");
            }
            catch (Exception ex)
            {
                Logger.Warn($"{AppName} {MethodBase.GetCurrentMethod().Name} {ex}");
                return (false, $"Erro ao criar Cliente {cliente.Cliente}.{ex.Message.ToString()}");
            }
            finally
            {
                cliente = null;
                fillObject = null;
            }
        }

        public static (bool Sucesso, string Mensagem) NovoPais(string pais)
        {
            if (string.IsNullOrWhiteSpace(pais))
                return (false, $"Erro ao criar Pais.");

            BasBEPais paisEntity = null;
            string erro = string.Empty;

            try
            {
                if (ProductContext.MotorLE.Base.Paises.Existe(pais))
                    return (true,"");

                paisEntity = new BasBEPais
                {
                    Pais = pais,
                    Descricao = pais
                };

                if (!ProductContext.MotorLE.Base.Paises.ValidaActualizacao(paisEntity, ref erro))
                {
                    Logger.Warn($"{AppName} {MethodBase.GetCurrentMethod().Name} {erro}");
                    return (false, $" Erro ao criar País {paisEntity.Pais} {erro}");
                }

                ProductContext.MotorLE.Base.Paises.Actualiza(paisEntity);
                return (true,"");
            }
            catch (Exception ex)
            {
                Logger.Warn($"{AppName} {MethodBase.GetCurrentMethod().Name} {ex}");
                return (false, $" Erro ao criar País {paisEntity.Pais} {ex.Message.ToString()}");
            }
            finally
            {
                paisEntity = null;
            }
        }


        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value.Length <= maxLength
                ? value
                : value.Substring(0, maxLength);
        }


        public static string DaIDDocumentoVendaExt(string TipoDoc, string NumDoc, string Serie)
        {
            StringBuilder query = new StringBuilder();
            try
            {

                query.AppendLine("SELECT TOP 1 Id FROM CabecDoc  WITH (NOLOCK) ");
                query.AppendLine("WHERE RefTipoDocOrig = '" + TipoDoc + "'");
                query.AppendLine("AND Serie = '" + Serie + "'");
                query.AppendLine("AND RefDocOrig = '" + NumDoc + "'");

                StdBELista result = ProductContext.MotorLE.Consulta(query.ToString());
                return result != null && result.NumLinhas() > 0 ? result.Valor("id").ToString() : string.Empty;

            }
            catch (Exception e)
            {
                Logger.Warn($"{AppName} {MethodBase.GetCurrentMethod().Name} {e.ToString()}");
                return string.Empty;
            }
            finally
            {
                if (query != null) { query = null; }
            }
        }


        #region "Utilities"
        public class GenericPropertyFill<TModelOrigem, TModelDestino>
        {
            private TModelOrigem pObjectoOrigem;
            private TModelDestino pObjectoDestino;
            public TModelDestino Fill()
            {
                try
                {
                    Type tModelType1 = ObjectoOrigem.GetType();
                    Type tModelType2 = ObjectoDestino.GetType();
                    PropertyInfo[] Propriedades = pObjectoOrigem.GetType().GetProperties();
                    foreach (PropertyInfo p in Propriedades)
                    {
                        if (ValidProperty(ObjectoDestino, p.Name))
                        {
                            SetPropertyValue(ObjectoDestino, ObjectoDestino.GetType(), p.Name, p.GetValue(pObjectoOrigem, null));
                        }
                    }
                    return ObjectoDestino;
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message.ToString());
                }
            }

            public TModelOrigem ObjectoOrigem
            {
                get => pObjectoOrigem;
                set => pObjectoOrigem = value;
            }
            public TModelDestino ObjectoDestino
            {
                get => pObjectoDestino;
                set => pObjectoDestino = value;
            }
        }
        public static bool ValidProperty(object o, string p)
        {
            try
            {
                Type type = o.GetType();
                if (type.GetProperty(p) == null)
                {
                    return false;
                }
                if (!type.GetProperty(p).CanWrite)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public static bool PropertyCanWrite(object o, string p)
        {
            try
            {
                Type type = o.GetType();
                return type.GetProperty(p) != null;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public static void SetPropertyValue(object inputObject, Type type, string propertyName, object propertyVal)
        {
            try
            {
                var targetType = IsNullableType(type.GetProperty(propertyName).PropertyType) ? Nullable.GetUnderlyingType(type.GetProperty(propertyName).PropertyType) : type.GetProperty(propertyName).PropertyType;
                propertyVal = Convert.ChangeType(propertyVal, targetType);
                type.GetProperty(propertyName).SetValue(inputObject, propertyVal, null);
            }
            catch (Exception ex)
            {
                throw new Exception("Erro na conversão da propriedade [" + propertyName + "]. O valor " + propertyVal + " não corresponde ao tipo do campo." + ex.Message);
            }
        }
        private static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
        }

        /// <summary>
        /// Convert a base 10 number to a different base.
        /// </summary>
        /// <param name = "number">Number to convert</param>
        /// <param name = "toBase">Base to use</param>
        /// <returns>String</returns>
        public static string NumberToBase(long number, int toBase)
        {
            const string base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            if (number == 0) return "0";
            if (number < 0) throw new ArgumentOutOfRangeException("number", number, "Number cannot be negative");
            var baseChars = base36.ToCharArray();
            var result = new Stack<char>();
            while (number != 0)
            {
                result.Push(baseChars[number % toBase]);
                number /= toBase;
            }
            return new string(result.ToArray());
        }

        public static string ConvertPdfToBase64(string filePath)
        {
            if (!File.Exists(filePath))
                return $"O PDF não existe {filePath}";

            byte[] pdfBytes = File.ReadAllBytes(filePath);
            return Convert.ToBase64String(pdfBytes);
        }
        #endregion
    }
}
