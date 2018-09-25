using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;


namespace AdventureWorks2014SalesReport.Controllers
{
    public class ReportController : ApiController
    {
        private AdventureWorks2014DataContext AdventureWorks2014DataContext1 = new AdventureWorks2014DataContext();

        // GET
        public HttpResponseMessage Get(DateTime StartDate, DateTime EndDate, String Mode = "Preview")
        {
            //Let's go get the data.
            var Orders = from SalesOrderHeader in AdventureWorks2014DataContext1.SalesOrderHeaders
                               where SalesOrderHeader.DueDate >= StartDate
                               && SalesOrderHeader.DueDate <= EndDate
                               select new
                               {
                                   SoldAt = SalesOrderHeader.Customer.Store.Name,
                                   SoldTo = SalesOrderHeader.Customer.Person.FirstName + ' ' + SalesOrderHeader.Customer.Person.LastName,
                                   AccountNumber = SalesOrderHeader.AccountNumber,
                                   InvoiceNumber = SalesOrderHeader.SalesOrderNumber,
                                   CustomerPO = SalesOrderHeader.PurchaseOrderNumber,
                                   OrderDate = SalesOrderHeader.OrderDate,
                                   DueDate = SalesOrderHeader.DueDate,
                                   InvoiceTotal = SalesOrderHeader.TotalDue,
                                   ProductNumber = SalesOrderHeader.SalesOrderDetail.SpecialOfferProduct.Product.ProductNumber,
                                   OrderQty = SalesOrderHeader.SalesOrderDetail.OrderQty,
                                   UnitNet = SalesOrderHeader.SalesOrderDetail.UnitPrice,
                                   LineTotal = SalesOrderHeader.SalesOrderDetail.LineTotal
                               };

            //Same API endpoint for the preview of the report (JSON response), and export (Excel File Download).
            if (Mode == "Export")
            {
                //Let's kick the results out to an excel file.
                var pkg = new ExcelPackage();
                var wbk = pkg.Workbook;
                var sheet = wbk.Worksheets.Add("Invoice Data");

                var normalStyle = "Normal";
                var acctStyle = wbk.CreateAccountingFormat();
                var dateStyle = wbk.CreateDateFormat();

                //Turn our results into a list of CustomerInvoice for export 
                List<CustomerInvoice> Invoices = new List<CustomerInvoice>();
                foreach (var Row in Orders.ToList())
                {
                    CustomerInvoice thisInvoice = new CustomerInvoice();
                    thisInvoice.SoldAt = Row.SoldAt;
                    thisInvoice.SoldTo = Row.SoldTo;
                    thisInvoice.AccountNumber = Row.AccountNumber;
                    thisInvoice.InvoiceNumber = Row.InvoiceNumber;
                    thisInvoice.CustomerPO = Row.CustomerPO;
                    thisInvoice.OrderDate = Row.OrderDate;
                    thisInvoice.DueDate = Row.DueDate;
                    thisInvoice.InvoiceTotal = Row.InvoiceTotal;
                    thisInvoice.ProductNumber = Row.ProductNumber;
                    thisInvoice.OrderQty = Row.OrderQty;
                    thisInvoice.UnitNet = Row.UnitNet;
                    thisInvoice.LineTotal = Row.LineTotal;
                    Invoices.Add(thisInvoice);
                }

                //Build the column headers/styles
                var columns = new[]
                {
                    new Column { Title = "Sold At", Style = normalStyle, Action = i => i.SoldAt, },
                    new Column { Title = "Sold To", Style = normalStyle, Action = i => i.SoldTo, },
                    new Column { Title = "Account Number", Style = normalStyle, Action = i => i.AccountNumber, },
                    new Column { Title = "Invoice #", Style = normalStyle, Action = i => i.InvoiceNumber, },
                    new Column { Title = "Customer PO #", Style = normalStyle, Action = i => i.CustomerPO, },
                    new Column { Title = "Order Date", Style = dateStyle, Action = i => i.OrderDate, },
                    new Column { Title = "Due Date", Style = dateStyle, Action = i => i.DueDate, },
                    new Column { Title = "Invoice Total", Style = acctStyle, Action = i => i.InvoiceTotal, },
                    new Column { Title = "Product Number", Style = normalStyle, Action = i => i.ProductNumber, },
                    new Column { Title = "Order Qty", Style = normalStyle, Action = i => i.OrderQty, },
                    new Column { Title = "Unit Net", Style = acctStyle, Action = i => i.UnitNet, },
                    new Column { Title = "Line Total", Style = acctStyle, Action = i => i.LineTotal, },
                };

                sheet.SaveData(columns, Invoices);

                var bytes = pkg.GetAsByteArray();
                Stream ResponseFile = new MemoryStream(bytes);

                //Let's send back the response now.
                HttpResponseMessage SuccessResponse = new HttpResponseMessage();
                SuccessResponse.Content = new StreamContent(ResponseFile);
                SuccessResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                SuccessResponse.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = "Invoices"+StartDate.ToString("yyyyMMdd")+"to"+EndDate.ToString("yyyyMMdd")+".xlsx"
                };
                SuccessResponse.StatusCode = HttpStatusCode.OK;
                return SuccessResponse;
            }
            else
            {
                //Serialize the first 15 results to JSON.
                String Results = JsonConvert.SerializeObject(
                    Orders.Take(15)
                    );

                //Let's send back the response now.
                HttpResponseMessage SuccessResponse = new HttpResponseMessage();
                SuccessResponse.Content = new StringContent(Results);
                SuccessResponse.StatusCode = HttpStatusCode.OK;
                return SuccessResponse;
            }
        }

        public class CustomerInvoice
        {
            public string SoldAt { get; set; }
            public string SoldTo { get; set; }
            public string AccountNumber { get; set; }
            public string InvoiceNumber { get; set; }
            public string CustomerPO { get; set; }
            public DateTime OrderDate { get; set; }
            public DateTime DueDate { get; set; }
            public decimal InvoiceTotal { get; set; }
            public string ProductNumber { get; set; }
            public int OrderQty { get; set; }
            public decimal UnitNet { get; set; }
            public decimal LineTotal { get; set; }
        }

        private class Column : SpreadsheetBuilder.ColumnTemplate<CustomerInvoice> { }

    }
}
