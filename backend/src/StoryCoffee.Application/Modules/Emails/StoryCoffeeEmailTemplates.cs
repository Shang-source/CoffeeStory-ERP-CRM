using System.Globalization;
using System.Net;

namespace StoryCoffee.Application.Emails;

public sealed record RenderedEmail(string TextBody, string HtmlBody);

public static class StoryCoffeeEmailTemplates
{
    private const string BrandGreen = "#007A3D";
    private const string Ink = "#1f2933";
    private const string Muted = "#52616b";
    private const string Border = "#d8e1e8";
    private const string Surface = "#f5f9fb";

    public static RenderedEmail CustomerInvite(string contactName, string loginUrl, string email, string temporaryPassword)
    {
        var safeContactName = string.IsNullOrWhiteSpace(contactName) ? "there" : contactName.Trim();
        var text = $"""
            Hello {safeContactName},

            Your StoryCoffee customer portal account has been created.

            Login URL: {loginUrl}
            Email: {email}
            Temporary password: {temporaryPassword}

            Please change your password after your first login. Until you change it, this temporary password will remain valid.
            """;

        var html = Layout(
            "Welcome to StoryCoffee",
            $"""
            <p>Hello {Html(safeContactName)},</p>
            <p>Your StoryCoffee customer portal account has been created.</p>
            <div class="summary">
              <div><span>Login URL</span><strong><a href="{Html(loginUrl)}">{Html(loginUrl)}</a></strong></div>
              <div><span>Email</span><strong>{Html(email)}</strong></div>
              <div><span>Temporary password</span><strong>{Html(temporaryPassword)}</strong></div>
            </div>
            <p class="muted">Please change your password after your first login. Until you change it, this temporary password will remain valid.</p>
            """);

        return new RenderedEmail(text, html);
    }

    public static RenderedEmail Invoice(string invoiceNumber, string accountNumber, string customerName, decimal amountDue, DateTimeOffset dueDate)
    {
        var amount = Money(amountDue);
        var due = Date(dueDate);
        var text = $"""
            Your StoryCoffee invoice {invoiceNumber} is attached.

            Customer: {customerName}
            Amount due: {amount}
            Payment due: {due}

            Please use your account number as the payment reference: {accountNumber}

            Bank: ASB
            Account number: 12-3077-0789998-00
            """;

        var html = Layout(
            $"Invoice {invoiceNumber}",
            $"""
            <p>Your StoryCoffee invoice is attached as a PDF.</p>
            <div class="hero">
              <span>Amount due</span>
              <strong>{Html(amount)}</strong>
              <small>Due on {Html(due)}</small>
            </div>
            <div class="summary">
              <div><span>Invoice number</span><strong>{Html(invoiceNumber)}</strong></div>
              <div><span>Customer</span><strong>{Html(customerName)}</strong></div>
              <div><span>Payment reference</span><strong>{Html(accountNumber)}</strong></div>
            </div>
            <p><strong>Payment details</strong><br>Bank: ASB<br>Account number: 12-3077-0789998-00</p>
            """);

        return new RenderedEmail(text, html);
    }

    public static RenderedEmail Statement(string statementNumber, string accountNumber, string customerName, decimal totalOutstanding, DateTimeOffset statementDate)
    {
        var amount = Money(totalOutstanding);
        var statementDateText = Date(statementDate);
        var paymentReference = accountNumber;
        var text = $"""
            Your StoryCoffee statement {statementNumber} is attached.

            Customer: {customerName}
            Total outstanding: {amount}
            Statement date: {statementDateText}

            Please use your account number as the payment reference: {paymentReference}

            Account name: reborn Edge Limited
            Bank: ASB
            Account number: 12-3077-0789998-00
            """;

        var html = Layout(
            $"Statement {statementNumber}",
            $"""
            <p>Your StoryCoffee statement is attached as a PDF.</p>
            <div class="hero">
              <span>Total outstanding</span>
              <strong>{Html(amount)}</strong>
              <small>Statement date {Html(statementDateText)}</small>
            </div>
            <div class="summary">
              <div><span>Statement number</span><strong>{Html(statementNumber)}</strong></div>
              <div><span>Customer</span><strong>{Html(customerName)}</strong></div>
              <div><span>Payment reference</span><strong>{Html(paymentReference)}</strong></div>
            </div>
            <p><strong>Payment details</strong><br>Account name: reborn Edge Limited<br>Bank: ASB<br>Account number: 12-3077-0789998-00</p>
            """);

        return new RenderedEmail(text, html);
    }

    private static string Layout(string title, string body)
    {
        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{{Html(title)}}</title>
              <style>
                body { margin:0; padding:0; background:#eef4f6; color:{{Ink}}; font-family:Arial, Helvetica, sans-serif; }
                .wrap { max-width:640px; margin:0 auto; padding:28px 16px; }
                .card { background:#ffffff; border:1px solid {{Border}}; border-radius:18px; overflow:hidden; box-shadow:0 10px 30px rgba(31,41,51,.08); }
                .brand { padding:28px 32px; border-bottom:1px solid {{Border}}; }
                .brand-row { display:flex; align-items:center; justify-content:space-between; gap:16px; }
                .brand-name { font-size:28px; font-weight:800; letter-spacing:.2px; color:{{Ink}}; }
                .badge { display:inline-block; padding:7px 11px; border-radius:999px; background:#e7f5ee; color:{{BrandGreen}}; font-size:13px; font-weight:700; }
                .content { padding:30px 32px 34px; font-size:16px; line-height:1.6; }
                h1 { margin:0 0 12px; font-size:26px; line-height:1.2; }
                p { margin:0 0 18px; }
                a { color:{{BrandGreen}}; }
                .muted { color:{{Muted}}; }
                .hero { margin:22px 0; padding:22px; background:{{Surface}}; border:1px solid {{Border}}; border-radius:14px; text-align:center; }
                .hero span, .summary span { display:block; color:{{Muted}}; font-size:13px; text-transform:uppercase; letter-spacing:.08em; }
                .hero strong { display:block; margin:8px 0 4px; font-size:36px; line-height:1; }
                .hero small { color:{{Muted}}; font-size:15px; }
                .summary { margin:20px 0; border:1px solid {{Border}}; border-radius:14px; overflow:hidden; }
                .summary div { padding:14px 16px; border-bottom:1px solid {{Border}}; }
                .summary div:last-child { border-bottom:0; }
                .summary strong { display:block; margin-top:3px; }
                .footer { padding:18px 32px; border-top:1px solid {{Border}}; color:{{Muted}}; font-size:13px; background:#fbfcfd; }
              </style>
            </head>
            <body>
              <div class="wrap">
                <div class="card">
                  <div class="brand">
                    <div class="brand-row">
                      <div class="brand-name">Story Coffee Roasters</div>
                      <div class="badge">StoryCoffee</div>
                    </div>
                  </div>
                  <div class="content">
                    <h1>{{Html(title)}}</h1>
                    {{body}}
                  </div>
                  <div class="footer">
                    Story Coffee Roasters · PO BOX 9065, New Market, Auckland 1149 · www.storycoffee.co.nz
                  </div>
                </div>
              </div>
            </body>
            </html>
            """;
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private static string Money(decimal value)
    {
        return string.Create(CultureInfo.InvariantCulture, $"${value:F2} NZD");
    }

    private static string Date(DateTimeOffset value)
    {
        return value.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
    }
}
