
using Microsoft.Extensions.Configuration;
using MimeKit;
using MailKit.Net.Smtp;

namespace MonitorBolsaDeTrabajo.Services
{
    public class EmailService: IEmailService
{
    private readonly IConfiguration _configuration;
    
    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task SendNewOfertasEmailAsync(List<Models.Oferta> ofertas)
    {
        if (!ofertas.Any())
            return;
        
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            "Monitor Bolsa Trabajo UNLP", 
            _configuration["EmailSettings:SenderEmail"]));
        message.To.Add(new MailboxAddress(
            "Destinatario", 
            _configuration["EmailSettings:RecipientEmail"]));
        message.Subject = $"Nuevas ofertas de trabajo disponibles ({ofertas.Count})";
        
        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = BuildEmailHtml(ofertas),
            TextBody = BuildEmailText(ofertas)
        };
        
        message.Body = bodyBuilder.ToMessageBody();
        
        if (!int.TryParse(_configuration["EmailSettings:SmtpPort"], out int smtpPort))
        {
            throw new InvalidOperationException($"Invalid SMTP port: {_configuration["EmailSettings:SmtpPort"]}");
        }

        using var client = new SmtpClient();
        await client.ConnectAsync(
            _configuration["EmailSettings:SmtpServer"],
            smtpPort,
            MailKit.Security.SecureSocketOptions.StartTls);
        
        await client.AuthenticateAsync(
            _configuration["EmailSettings:SenderEmail"],
            _configuration["EmailSettings:SenderPassword"]);
        
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
    
    private string BuildEmailHtml(List<Models.Oferta> ofertas)
    {
        var html = @"
        <html>
        <head>
            <style>
                body { font-family: Arial, sans-serif; }
                .oferta { border: 1px solid #ddd; padding: 15px; margin: 10px 0; border-radius: 5px; }
                .titulo { color: #2c3e50; font-size: 18px; font-weight: bold; }
                .empresa { color: #7f8c8d; font-style: italic; }
                .link { color: #3498db; text-decoration: none; }
            </style>
        </head>
        <body>
            <h2>¡Nuevas ofertas de trabajo disponibles!</h2>
            <p>Se han encontrado " + ofertas.Count + @" nuevas ofertas en la Bolsa de Trabajo UNLP.</p>";
        
        foreach (var oferta in ofertas)
        {
            html += $@"
            <div class='oferta'>
                <div class='titulo'>{oferta.Titulo}</div>
                <p>{oferta.Titulo}</p>
                <a class='link' href='{oferta.Link}'>Ver oferta completa</a>
            </div>";
        }
        
        html += @"
            <hr>
            <p><small>Este email fue enviado automáticamente por el sistema de monitoreo.</small></p>
        </body>
        </html>";
        
        return html;
    }
    
    private string BuildEmailText(List<Models.Oferta> ofertas)
    {
        var text = $"¡Nuevas ofertas de trabajo disponibles!\n\nSe han encontrado {ofertas.Count} nuevas ofertas:\n\n";
        
        foreach (var oferta in ofertas)
        {
            text += $"{oferta.Titulo}\n";
            text += $"Enlace: {oferta.Link}\n";
            text += "-------------------\n";
        }
        
        return text;
    }
}
}