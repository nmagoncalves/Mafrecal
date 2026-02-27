
 
using Newtonsoft.Json;
using System.Dynamic;
using System.Text.Json;
 

namespace Mafrecal.WorkerService.Services
{

    public static class MapperService
    {

        public static string GetString(JsonElement el, string prop) =>
    el.TryGetProperty(prop, out var value) ? value.GetString() ?? "" : "";

        public static double GetDouble(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var value) && value.TryGetDouble(out var d) ? d : 0.0;

        public static int GetInt(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var value) && value.TryGetInt32(out var i) ? i : 0;

        public static long GetLong(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var value) && value.TryGetInt64(out var l) ? l : 0;

        public static bool GetBool(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var value) && value.ValueKind == JsonValueKind.True ? true : false;

        public static string MapFornecedor(JsonElement source, JsonElement sourceBase)
        {

            int _espacoFiscal = source.GetProperty("CountryId").GetString() switch
            {
                "PRT" => 1,
                "UE" => 2,
                _ => 3
            };


            var obj = new
            {
                Codigo = sourceBase.GetProperty("SupplierTaxId").GetString(),
                Nome = GetString(source, "SupplierName")[..Math.Min(50, GetString(source, "SupplierName").Length)],
                NumContribuinte = source.GetProperty("FederalTaxId").GetString(),
                Morada =  
                GetString(source, "AddressLine1")[..Math.Min(50, GetString(source, "AddressLine1").Length)],
                CodigoPostal = source.GetProperty("PostalCode").GetString(),
                Localidade = sourceBase.GetProperty("SupplierCity").GetString(),
                LocalidadeCodigoPostal = sourceBase.GetProperty("SupplierCity").GetString(),
                LocalOperacao = "2",
                Telefone = source.GetProperty("Telephone1").GetString(),
                Fax = source.GetProperty("Fax").GetString(),
                Email ="",// source.GetProperty("EmailAddress").GetString(),
                EnderecoWeb = source.GetProperty("WebAddress").GetString(),
                Distrito = "",
                Moeda = "EUR",
                Pais = "PT", //source.GetProperty("SupplierCountry").GetString(),
                Descricao = source.GetProperty("SupplierName").GetString(),
                TipoTerceiro = "F",
                CondPagamento = GetInt(sourceBase, "PaymentTermsCode").ToString(),
                ModPag = "NUM",
            };
 
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }

        public static string CondPagamento(JsonElement source)
        {
            var obj = new
            {
                Codigo = GetInt(source, "PaymentTermsCode"),
                Descricao =   GetString(source, "PaymentTerms"),
                Dias =  GetInt(source, "PaymentTermsDays"),
            };

            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }

        public static string MapArtigoGroup(JsonElement item)
        {

            string artigo = item.GetProperty("ItemId").ValueKind switch
            {
                JsonValueKind.String => item.GetProperty("ItemId").GetString(),
                JsonValueKind.Number => item.GetProperty("ItemId").GetInt64().ToString(),
            };

            var obj = new
            {
                Codigo = artigo,
                Descricao = GetString(item, "ItemId"),
                UnidadeBase = "UN", // GetString(item, "UnitOfSaleId"),
                Iva = Convert.ToString(GetDouble(item, "TaxRate")),
                TaxaIva = GetDouble(item, "TaxPercentage"),
            };
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }

        public static string MapArtigoFull(JsonElement item, JsonElement sourceBase)
        {

            string artigo = item.GetProperty("ItemId").ValueKind switch
            {
                JsonValueKind.String => item.GetProperty("ItemId").GetString(),
                JsonValueKind.Number => item.GetProperty("ItemId").GetInt64().ToString(),
            };


            dynamic obj = new ExpandoObject();

            obj.Codigo = artigo;
            var desc = GetString(item, "ItemPublicDescription")[..Math.Min(50, GetString(item, "ItemPublicDescription").Length)];
            obj.Descricao = desc ;
            obj.UnidadeBase = "UN";
            obj.Iva = Convert.ToString(GetDouble(sourceBase, "TaxRate"));
            obj.TaxaIva = GetDouble(sourceBase, "TaxPercentage");
            obj.Familia = Convert.ToString(GetInt(item, "FamilyId"));
            obj.FamiliaDesc = GetString(item, "FamilyDescription");
            obj.SubFamilia = Convert.ToString(GetInt(item, "ParentFamilyId"));
            obj.SubFamiliaDesc = GetString(item, "ParentFamilyDescription")[..Math.Min(50, GetString(item, "FamilyDescription").Length)];

            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }


        public static string MapCompraGrouped(JsonElement tx, string documento)
        {

            // TODO: tratar PU e totais liquidos de igual maneira
            var linhas = new List<dynamic>();
            foreach (var item in tx.GetProperty("BuyTransactionDetails").EnumerateArray())
            {
                linhas.Add(new
                {

                    Artigo = GetString(item, "ItemId"),
                    Marca = "",
                    Armazem = "",
                    Familia = "",
                    Descricao = GetString(item, "ItemId"),
                    Quantidade = GetDouble(item, "Quantity"),
                    PrecUnit = GetDouble(item, "UnitPrice"),
                    Iva = Convert.ToString(GetDouble(item, "TaxRate")),
                    TaxaIva = GetDouble(item, "TaxPercentage"),
                    Desconto1 = 0,
                    Desconto2 = 0.00,
                    Desconto3 = 0.00,
                    ValorIVA = GetDouble(item, "TotalTaxAmount"),
                    PrecoLiquido = GetDouble(item, "UnitPrice"),
                    TotalLiquido = GetDouble(item, "TotalNetAmount"),
                    TotalDescontoValor = GetDouble(item, "TotalLineItemDiscountAmount")

                });
            }

            var totalTaxAmounts = new List<dynamic>();
            foreach (var tax in tx.GetProperty("TotalTaxAmounts").EnumerateArray())
            {
                totalTaxAmounts.Add(new
                {
                    CodIva = GetString(tax, "TaxCode"),
                    TaxaIVA = GetDouble(tax, "TaxPercentage"),
                    Incidencia = GetDouble(tax, "TotalNetBaseTaxAmount"),
                    Valor = GetDouble(tax, "TotalTaxAmount"),
 
                });
            }

            var obj = new
            {
                TipoDoc = documento,// GetString(tx, "TransDocument"),
                Serie = GetString(tx, "InvoiceDate").Length >= 4 ? GetString(tx, "InvoiceDate").Substring(0, 4) : "",

                Fornecedor = GetString(tx, "SupplierTaxId"),
                NomeFornecedor = GetString(tx, "SupplierCompanyName")[..Math.Min(50, GetString(tx, "SupplierCompanyName").Length)],
                NumDocExterno = GetString(tx, "ContractReferenceNumber"),
                DataDoc = GetString(tx, "InvoiceDate"),
                DataIntroducao = GetString(tx, "InvoiceDate"),
                DataVenc = GetString(tx, "InvoiceDate"),
                Loja = tx.GetProperty("StoreId").GetString(),
                NumVendedor="",
                NomeVendedor = "",
                Referencia = "",
                ModPag = "NUM",
                CondPagamento = GetInt(tx, "PaymentTermsCode").ToString(), 
                Moeda="",
                LocalOperacao =2,
                EspacoFiscal = 1,
                TotalDocumento = GetDouble(tx, "TotalAmount"),
                TotalIva = GetDouble(tx, "TotalTaxAmount"),
                TotalMerc = GetDouble(tx, "TotalNetAmount"),
                TotalDesc =0,
                DescontoComercial = GetDouble(tx, "TotalPaymentDiscountAmount"),
                DescontoFinanceiro = 0,
 
                Linhas = linhas,
                ResumoIVA = totalTaxAmounts,
                Observacoes = $"Importado via Storesace - Sync {GetLong(tx, "synccounter")}"

            };
            return JsonConvert.SerializeObject(obj, Formatting.Indented);

 

        }

        public static string MapCompraFull(JsonElement tx, string documento)
        {
 

            var linhas = new List<dynamic>();
            foreach (var item in tx.GetProperty("BuyTransactionDetails").EnumerateArray())
            {
                // TODO: tratar Id como ItemId
                string artigo = item.GetProperty("ItemId").ValueKind switch
                {
                    JsonValueKind.String => item.GetProperty("ItemId").GetString(),
                    JsonValueKind.Number => item.GetProperty("ItemId").GetInt64().ToString(),
                };

                linhas.Add(new
                {

                    Artigo = artigo,
                    Marca = "",
                    Armazem = "",
                    Familia = "",
                    Descricao =    string.IsNullOrEmpty(GetString(item, "ProductDescription"))
                    ? "sem descricação"
                    : GetString(item, "ProductDescription")[..Math.Min(50, GetString(item, "ProductDescription").Length)]
                    ,
                    Quantidade = GetDouble(item, "Quantity"),
                    PrecUnit = GetDouble(item, "UnitPrice"),
                    Iva = Convert.ToString(GetDouble(item, "TaxPercentage")),
                    TaxaIva = GetDouble(item, "TaxPercentage"),
                    Desconto1 = GetDouble(item, "DiscountPercent"),
                    Desconto2 = 0.00,
                    Desconto3 = 0.00,
                    ValorIVA = GetDouble(item, "TotalTaxAmount"),
                    PrecoLiquido = GetDouble(item, "UnitPrice"),
                    TotalLiquido = GetDouble(item, "TotalNetAmount"),
                    TotalDescontoValor = GetDouble(item, "TotalLineItemDiscountAmount")

                });
            }

            var totalTaxAmounts = new List<dynamic>();
            foreach (var tax in tx.GetProperty("TotalTaxAmounts").EnumerateArray())
            {
                totalTaxAmounts.Add(new
                {
                    CodIva = GetString(tax, "TaxCode"),
                    TaxaIVA = GetDouble(tax, "TaxPercentage"),
                    Incidencia = GetDouble(tax, "TotalNetBaseTaxAmount"),
                    Valor = GetDouble(tax, "TotalTaxAmount"),

                });
            }

            // TODO: verificar total    
            var obj = new
            {
                TipoDoc = documento,// GetString(tx, "TransDocument"),
                Serie = GetString(tx, "InvoiceDate").Length >= 4  ? GetString(tx, "InvoiceDate").Substring(0, 4) : "",
                Fornecedor = GetString(tx, "SupplierTaxId"),
                NomeFornecedor = GetString(tx, "SupplierCompanyName")[..Math.Min(50, GetString(tx, "SupplierCompanyName").Length)],
                NumDocExterno = GetString(tx, "ContractReferenceNumber"),
                DataDoc = GetString(tx, "InvoiceDate"),
                DataIntroducao = GetString(tx, "InvoiceDate"),
                DataVenc = GetString(tx, "InvoiceDate"),
                Loja = tx.GetProperty("StoreId").GetString(),
                NumVendedor = "",
                NomeVendedor = "",
                Referencia = "",
                CondPagamento = GetInt(tx, "PaymentTermsCode").ToString(),
                ModPag = "NUM",
                Moeda = "",
                LocalOperacao = 0,
                EspacoFiscal = 1,
                TotalDocumento = GetDouble(tx, "TotalAmount"),
                TotalDesconto = GetDouble(tx, "TotalTaxAmount"),
                TotalIva = GetDouble(tx, "TotalTaxAmount"),
                TotalMerc = GetDouble(tx, "TotalNetAmount"),
                TotalDesc = GetDouble(tx, "TotalLineItemDiscountAmount"),
                DescontoComercial = GetDouble(tx, "TotalPaymentDiscountAmount"),
                DescontoFinanceiro = 0,

                Linhas = linhas,
                ResumoIVA = totalTaxAmounts,
                Observacoes = $"Importado via Storesace - Sync {GetLong(tx, "synccounter")}"

            };
            return JsonConvert.SerializeObject(obj, Formatting.Indented);

        }


        public static string MapCliente(JsonElement source)
        {

            int _espacoFiscal = source.GetProperty("CountryId").GetString() switch
            {
                "PRT" => 1,
                "UE" => 2,
                _ => 3
            };


            var obj = new
            {
                Codigo = source.GetProperty("FederalTaxId").GetString(),
                Nome = source.GetProperty("OrganizationName").GetString(),
                NumContribuinte = source.GetProperty("FederalTaxId").GetString(),
                Morada = source.GetProperty("AddressLine1").GetString(),
                CodigoPostal = source.GetProperty("PostalCode").GetString(),
                Localidade = source.GetProperty("PostalCodeName").GetString(),
                Pais = source.GetProperty("ISOCountryId").GetString(),
                LocalOperacao = "2" 
            };

            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }

        public static string MapVendaGrouped(JsonElement tx, string documentoVenda, string documentFecho, string caixa)
        {

            var linhas = new List<dynamic>();
            var acumuladoIva = new Dictionary<int, dynamic>();
           var totalTenderLines = new Dictionary<string, dynamic>();

            double precoLiquido = 0;
            double valorIVA = 0;

            foreach (var item in tx.GetProperty("SaleTransactionDetails").EnumerateArray())
            {

                var totalLiquido = Helpers.General.AdjustIfEndsWith5At3Decimals(GetDouble(item, "TotalNetBaseTaxAmount"));
                var totalIva = Helpers.General.AdjustIfEndsWith5At3Decimals(GetDouble(item, "TotalTaxAmount"));


                if (totalLiquido.Adjusted && totalIva.Adjusted)
                {
                    precoLiquido = totalLiquido.NewValue;
                    valorIVA = GetDouble(item, "TotalTaxAmount") + 0.005;
                }
                else
                {
                    precoLiquido = GetDouble(item, "TotalNetBaseTaxAmount");
                    valorIVA = GetDouble(item, "TotalTaxAmount");
                }

                    linhas.Add(new
                    {

                        Artigo = GetString(item, "ItemId"),
                        Marca = "",
                        Armazem = "",
                        Familia = "",
                        Descricao = GetString(item, "ItemId"),
                        Quantidade = GetDouble(item, "Quantity"),
                        PrecUnit = GetDouble(item, "TotalAmount"),
                        Iva = Convert.ToString(GetDouble(item, "TaxRate")),
                        TaxaIva = GetDouble(item, "TaxPercentage"),
                        Desconto1 = 0,
                        Desconto2 = 0.00,
                        Desconto3 = 0.00,
                        ValorIVA = valorIVA, // somar o valor do IVA para ficar com o valor total do IVA - GetDouble(item, "TotalTaxAmount"),
                        PrecoLiquido = precoLiquido, // diminuir o valor do IVA para ficar com o preço líquido - GetDouble(item, "TotalNetBaseTaxAmount")
                        TotalLiquido = GetDouble(item, "TotalNetAmount"),
                        TotalILiquido = GetDouble(item, "TotalAmount"),
                        TotalDescontoValor = GetDouble(item, "TotalLineItemDiscountAmount")

                    });

                var codIva = GetInt(item, "TaxRate");
  
                    if (!acumuladoIva.ContainsKey(codIva))
                    {
                        acumuladoIva[codIva] = new
                        {
                            CodIva = codIva,
                            TaxaIVA = GetDouble(item, "TaxPercentage"),
                            Incidencia = precoLiquido,// GetDouble(item, "TotalNetBaseTaxAmount"),
                            Valor = valorIVA//GetDouble(item, "TotalTaxAmount")
                        };
                    }
                    else
                    {
                        var atual = acumuladoIva[codIva];

                         acumuladoIva[codIva] = new
                        {
                            CodIva = atual.CodIva,
                            TaxaIVA = atual.TaxaIVA,
                            Incidencia = atual.Incidencia + precoLiquido,// GetDouble(item, "TotalNetBaseTaxAmount"),
                            Valor = atual.Valor + valorIVA //GetDouble(item, "TotalTaxAmount")
                         };
                    }
 
 
            }

            string tipoRececao;

            foreach (var tender in tx.GetProperty("TenderlineItems").EnumerateArray())
            {

                int tenderId = GetInt(tender, "TenderId");
                switch (tenderId)
                {
                    case 1:   // Dinheiro
                    case 100: // Dinheiro Manual
                        tipoRececao = "NUM";
                        break;

                    case 3:   // Cartão de Débito
                    case 4:   // Cartão de Crédito
                    case 20:  // MBWay
                    case 105: // Multibanco Manual
                        tipoRececao = "MB";
                        break;

                    default:
                        tipoRececao = "NUM";
                        break;
                }


                if (!totalTenderLines.ContainsKey(tipoRececao))
                {
                    totalTenderLines[tipoRececao] = new
                    {
                        TipoPagamento = tipoRececao,
                        Valor = GetDouble(tender, "PaymentAmount"),
                        Caixa = caixa
                    };
                }
                else
                {
 
                    var atual = totalTenderLines[tipoRececao];

                    totalTenderLines[tipoRececao] = new
                    {
                        TipoPagamento = tipoRececao,
                        Valor = atual.Valor + GetDouble(tender, "PaymentAmount"),
                        Caixa = caixa
                    };

                }

            }
 
            var obj = new
            {
                TipoDoc = documentoVenda,
                Serie =GetString(tx, "InvoiceDate").Length >= 4 ? GetString(tx, "InvoiceDate").Substring(0, 4) : "",
                Cliente = GetString(tx, "CustomerTaxId") == "" ? "VD": GetString(tx, "CustomerTaxId"),
                NomeCliente = GetString(tx, "SupplierCompanyName")[..Math.Min(50, GetString(tx, "SupplierCompanyName").Length)],
                DataDoc = GetString(tx, "InvoiceDate"),
                DataIntroducao = GetString(tx, "InvoiceDate"),
                DataVenc = GetString(tx, "InvoiceDate"),
                Loja = tx.GetProperty("StoreId").GetString(),
                NumVendedor = "",
                NomeVendedor = "",
                Referencia =  GetString(tx, "ContractReferenceNumber"),
                ModPag = "NUM",
                CondPagamento = GetInt(tx, "PaymentTermsCode").ToString(),
                Moeda = "",
                LocalOperacao = 2,
                EspacoFiscal = 1,
                TotalDocumento = GetDouble(tx, "TotalAmount"),
                TotalIva = GetDouble(tx, "TotalTaxAmount"),
                TotalMerc = GetDouble(tx, "TotalNetAmount"),
                TotalDesc = 0,
                //DescontoComercial = GetDouble(tx, "TotalPaymentDiscountAmount"),
                DescontoFinanceiro = 0,

                Linhas = linhas,
                ResumoIva = acumuladoIva.Values.ToList(),
                ResumoTipoPag = totalTenderLines.Values.ToList(),
                Observacoes = $"Importado via Storesace - Sync {GetLong(tx, "synccounter")}",
                DocumentoFecho = documentFecho

            };
            
            
            return JsonConvert.SerializeObject(obj, Formatting.Indented);


        }

        public static string MapInterno(JsonElement tx, string documentoInterno)
        {

            var linhas = new List<dynamic>();
            var acumuladoIva = new Dictionary<int, dynamic>();
 
            foreach (var item in tx.GetProperty("TransactionDetails").EnumerateArray())
            {
                linhas.Add(new
                {

                    Artigo = GetString(item, "ItemId"),
                    Marca = "",
                    Armazem = "",
                    Familia = "",
                    Descricao = GetString(item, "ItemId"),
                    Quantidade = GetDouble(item, "Quantity"),
                    PrecUnit = GetDouble(item, "UnitPrice"),
                    Iva = Convert.ToString(GetDouble(item, "TaxRate")),
                    TaxaIva = GetDouble(item, "TaxPercentage"),
                    Desconto1 = 0,
                    Desconto2 = 0.00,
                    Desconto3 = 0.00,
                    ValorIVA = GetDouble(item, "TotalTaxAmount"),
                    PrecoLiquido = GetDouble(item, "UnitPrice"),
                    TotalLiquido = GetDouble(item, "TotalNetAmount"),
                    TotalDescontoValor = GetDouble(item, "TotalLineItemDiscountAmount")

                });

                var codIva = GetInt(item, "TaxRate");

                if (!acumuladoIva.ContainsKey(codIva))
                {
                    acumuladoIva[codIva] = new
                    {
                        CodIva = codIva,
                        TaxaIVA = GetDouble(item, "TaxPercentage"),
                        Incidencia = GetDouble(item, "TotalNetBaseTaxAmount"),
                        Valor = GetDouble(item, "TotalTaxAmount")
                    };
                }
                else
                {
                    var atual = acumuladoIva[codIva];

                    acumuladoIva[codIva] = new
                    {
                        CodIva = atual.CodIva,
                        TaxaIVA = atual.TaxaIVA,
                        Incidencia = atual.Incidencia + GetDouble(item, "TotalNetBaseTaxAmount"),
                        Valor = atual.Valor + GetDouble(item, "TotalTaxAmount")
                    };
                }


            }

            var obj = new
            {
                TipoDoc = documentoInterno,
                Serie = GetString(tx, "InvoiceDate").Length >= 4 ? GetString(tx, "InvoiceDate").Substring(0, 4) : "",
                Cliente = "",
                NomeCliente = "",
                DataDoc = GetString(tx, "InvoiceDate"),
                DataIntroducao = GetString(tx, "InvoiceDate"),
                DataVenc = GetString(tx, "InvoiceDate"),
                Loja = tx.GetProperty("StoreId").GetString(),
                Referencia = GetString(tx, "ContractReferenceNumber"),
                Moeda = "",
                TotalDocumento = GetDouble(tx, "TotalAmount"),
                TotalIva = GetDouble(tx, "TotalTaxAmount"),
                TotalMerc = GetDouble(tx, "TotalNetAmount"),
                TotalDesc = 0,
                //DescontoComercial = GetDouble(tx, "TotalPaymentDiscountAmount"),
                DescontoFinanceiro = 0,

                Linhas = linhas,
                ResumoIva = acumuladoIva.Values.ToList(),
           
                Observacoes = $"Importado via Storesace - Sync {GetLong(tx, "synccounter")}",
 

            };


            return JsonConvert.SerializeObject(obj, Formatting.Indented);


        }


        public static string MapStore(JsonElement item)
        {
            var obj = new
            {
                Loja = item.GetProperty("StoreId").GetString(),
 
            };
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }

    }


}
