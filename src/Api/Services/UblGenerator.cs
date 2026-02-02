using System.Xml.Linq;
using ErpCloud.Api.Entities;

namespace ErpCloud.Api.Services;

/// <summary>
/// Generates UBL-TR 2.1 compliant XML from Invoice entity
/// </summary>
public class UblGenerator
{
    private static readonly XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace ubl = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";

    public string GenerateInvoiceXml(Invoice invoice, Party supplier, Party customer, Branch branch)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(ubl + "Invoice",
                new XAttribute(XNamespace.Xmlns + "cac", cac),
                new XAttribute(XNamespace.Xmlns + "cbc", cbc),
                
                // UBL Version
                new XElement(cbc + "UBLVersionID", "2.1"),
                new XElement(cbc + "CustomizationID", "TR1.2"),
                
                // Invoice metadata
                new XElement(cbc + "ID", invoice.InvoiceNo),
                new XElement(cbc + "UUID", invoice.Id.ToString()),
                new XElement(cbc + "IssueDate", invoice.IssueDate.ToString("yyyy-MM-dd")),
                new XElement(cbc + "IssueTime", invoice.CreatedAt.ToString("HH:mm:ss")),
                new XElement(cbc + "InvoiceTypeCode", invoice.Type == "SALES" ? "SATIS" : "ALIS"),
                new XElement(cbc + "DocumentCurrencyCode", invoice.Currency),
                
                // Note
                invoice.Note != null ? new XElement(cbc + "Note", invoice.Note) : null,
                
                // Supplier Party (Satıcı)
                new XElement(cac + "AccountingSupplierParty",
                    new XElement(cac + "Party",
                        new XElement(cac + "PartyIdentification",
                            new XElement(cbc + "ID", supplier.Code)
                        ),
                        new XElement(cac + "PartyName",
                            new XElement(cbc + "Name", supplier.Name)
                        ),
                        new XElement(cac + "PostalAddress",
                            new XElement(cbc + "CityName", "İstanbul"), // Default
                            new XElement(cbc + "Country",
                                new XElement(cbc + "Name", "Türkiye")
                            )
                        ),
                        !string.IsNullOrEmpty(supplier.TaxNumber) ?
                            new XElement(cac + "PartyTaxScheme",
                                new XElement(cac + "TaxScheme",
                                    new XElement(cbc + "ID", supplier.TaxNumber)
                                )
                            ) : null
                    )
                ),
                
                // Customer Party (Alıcı)
                new XElement(cac + "AccountingCustomerParty",
                    new XElement(cac + "Party",
                        new XElement(cac + "PartyIdentification",
                            new XElement(cbc + "ID", customer.Code)
                        ),
                        new XElement(cac + "PartyName",
                            new XElement(cbc + "Name", customer.Name)
                        ),
                        new XElement(cac + "PostalAddress",
                            new XElement(cbc + "CityName", "İstanbul"),
                            new XElement(cbc + "Country",
                                new XElement(cbc + "Name", "Türkiye")
                            )
                        ),
                        !string.IsNullOrEmpty(customer.TaxNumber) ?
                            new XElement(cac + "PartyTaxScheme",
                                new XElement(cac + "TaxScheme",
                                    new XElement(cbc + "ID", customer.TaxNumber)
                                )
                            ) : null
                    )
                ),
                
                // Tax Total
                new XElement(cac + "TaxTotal",
                    new XElement(cbc + "TaxAmount", 
                        new XAttribute("currencyID", invoice.Currency),
                        invoice.VatTotal.ToString("F2")
                    ),
                    new XElement(cac + "TaxSubtotal",
                        new XElement(cbc + "TaxableAmount",
                            new XAttribute("currencyID", invoice.Currency),
                            invoice.Subtotal.ToString("F2")
                        ),
                        new XElement(cbc + "TaxAmount",
                            new XAttribute("currencyID", invoice.Currency),
                            invoice.VatTotal.ToString("F2")
                        ),
                        new XElement(cac + "TaxCategory",
                            new XElement(cbc + "TaxExemptionReason", "KDV"),
                            new XElement(cac + "TaxScheme",
                                new XElement(cbc + "Name", "KDV"),
                                new XElement(cbc + "TaxTypeCode", "0015")
                            )
                        )
                    )
                ),
                
                // Legal Monetary Total
                new XElement(cac + "LegalMonetaryTotal",
                    new XElement(cbc + "LineExtensionAmount",
                        new XAttribute("currencyID", invoice.Currency),
                        invoice.Subtotal.ToString("F2")
                    ),
                    new XElement(cbc + "TaxExclusiveAmount",
                        new XAttribute("currencyID", invoice.Currency),
                        invoice.Subtotal.ToString("F2")
                    ),
                    new XElement(cbc + "TaxInclusiveAmount",
                        new XAttribute("currencyID", invoice.Currency),
                        invoice.GrandTotal.ToString("F2")
                    ),
                    new XElement(cbc + "PayableAmount",
                        new XAttribute("currencyID", invoice.Currency),
                        invoice.GrandTotal.ToString("F2")
                    )
                ),
                
                // Invoice Lines
                invoice.Lines.Select((line, index) => 
                    new XElement(cac + "InvoiceLine",
                        new XElement(cbc + "ID", (index + 1).ToString()),
                        new XElement(cbc + "InvoicedQuantity",
                            new XAttribute("unitCode", "C62"), // C62 = piece
                            line.Qty?.ToString("F3") ?? "1.000"
                        ),
                        new XElement(cbc + "LineExtensionAmount",
                            new XAttribute("currencyID", invoice.Currency),
                            line.LineTotal.ToString("F2")
                        ),
                        new XElement(cac + "TaxTotal",
                            new XElement(cbc + "TaxAmount",
                                new XAttribute("currencyID", invoice.Currency),
                                line.VatAmount.ToString("F2")
                            ),
                            new XElement(cac + "TaxSubtotal",
                                new XElement(cbc + "TaxableAmount",
                                    new XAttribute("currencyID", invoice.Currency),
                                    line.LineTotal.ToString("F2")
                                ),
                                new XElement(cbc + "TaxAmount",
                                    new XAttribute("currencyID", invoice.Currency),
                                    line.VatAmount.ToString("F2")
                                ),
                                new XElement(cbc + "Percent", line.VatRate.ToString("F2")),
                                new XElement(cac + "TaxCategory",
                                    new XElement(cac + "TaxScheme",
                                        new XElement(cbc + "Name", "KDV"),
                                        new XElement(cbc + "TaxTypeCode", "0015")
                                    )
                                )
                            )
                        ),
                        new XElement(cac + "Item",
                            new XElement(cbc + "Name", line.Description)
                        ),
                        new XElement(cac + "Price",
                            new XElement(cbc + "PriceAmount",
                                new XAttribute("currencyID", invoice.Currency),
                                line.UnitPrice?.ToString("F2") ?? "0.00"
                            )
                        )
                    )
                ).ToArray()
            )
        );

        return doc.ToString(SaveOptions.None);
    }
}
