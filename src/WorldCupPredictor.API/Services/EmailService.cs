using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using WorldCupPredictor.API.Data;
using WorldCupPredictor.API.Models;

namespace WorldCupPredictor.API.Services;

public class EmailService(AppDbContext db, IConfiguration config, ILogger<EmailService> logger) : IEmailService
{
    public async Task<int> SendGiveawayNotificationAsync(Giveaway giveaway)
    {
        var testOverride = config["Email:TestOverrideEmail"];

        var users = string.IsNullOrWhiteSpace(testOverride)
            ? await db.Users
                .Where(u => !string.IsNullOrEmpty(u.Email))
                .Select(u => new { u.Name, u.Email })
                .ToListAsync()
            : [new { Name = "Predictor", Email = testOverride }];

        if (users.Count == 0) return 0;

        var host = config["Email:Host"] ?? throw new InvalidOperationException("Email:Host not configured.");
        var port = int.Parse(config["Email:Port"] ?? "587");
        var username = config["Email:Username"] ?? throw new InvalidOperationException("Email:Username not configured.");
        var password = config["Email:Password"] ?? throw new InvalidOperationException("Email:Password not configured.");
        var fromAddress = config["Email:FromAddress"] ?? username;
        var fromName = config["Email:FromName"] ?? "Predict The Champion";
        var appUrl = config["Email:AppUrl"] ?? "http://localhost:4200";

        var match = giveaway.Match;
        var bdTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Bangladesh Standard Time" : "Asia/Dhaka");
        var matchDate = match.MatchDate.HasValue
            ? TimeZoneInfo.ConvertTimeFromUtc(match.MatchDate.Value, bdTimeZone)
                          .ToString("MMM dd, yyyy hh:mm tt") + " BST"
            : "TBD";

        int sent = 0;

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(username, password);

        foreach (var user in users)
        {
            try
            {
                var message = BuildEmail(fromAddress, fromName, user.Email!, user.Name,
                    match, giveaway.Prize, matchDate, appUrl);

                await client.SendAsync(message);
                sent++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send giveaway email to {Email}", user.Email);
            }
        }

        await client.DisconnectAsync(true);
        return sent;
    }

    private static MimeMessage BuildEmail(
        string fromAddress, string fromName,
        string toEmail, string toName,
        Match match, string prize, string matchDate, string appUrl)
    {
        var homeTeam = match.HomeTeam?.Name ?? "TBD";
        var awayTeam = match.AwayTeam?.Name ?? "TBD";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = $"🎁 New Giveaway: {homeTeam} vs {awayTeam} — Win {prize}!";

        var body = new BodyBuilder
        {
            HtmlBody = $"""
            <!DOCTYPE html>
            <html>
            <head>
              <meta charset="utf-8"/>
              <meta name="viewport" content="width=device-width, initial-scale=1"/>
            </head>
            <body style="margin:0;padding:0;background:#f0f4f8;font-family:'Segoe UI',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f0f4f8;padding:32px 0;">
                <tr><td align="center">
                  <table width="560" cellpadding="0" cellspacing="0" style="max-width:560px;width:100%;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.1);">

                    <!-- Header -->
                    <tr>
                      <td style="background:linear-gradient(135deg,#1a237e 0%,#1565c0 100%);padding:32px 24px;text-align:center;">
                        <div style="font-size:2.5rem;margin-bottom:8px;">⚽</div>
                        <div style="display:inline-block;background:linear-gradient(135deg,#f5c518,#e6a800);color:#1a1a1a;font-size:11px;font-weight:800;letter-spacing:0.12em;text-transform:uppercase;padding:5px 14px;border-radius:20px;margin-bottom:12px;">🎁 New Giveaway Alert</div>
                        <h1 style="margin:0;color:#ffffff;font-size:1.8rem;font-weight:900;letter-spacing:-0.02em;">Win {prize}!</h1>
                      </td>
                    </tr>

                    <!-- Match card -->
                    <tr>
                      <td style="background:#ffffff;padding:28px 24px;">
                        <p style="margin:0 0 20px;color:#444;font-size:0.95rem;text-align:center;">
                          Hey <strong>{toName}</strong>, a new giveaway is open on <strong>Predict The Champion</strong>.
                          Predict the score correctly and you could win!
                        </p>

                        <table width="100%" cellpadding="0" cellspacing="0" style="background:linear-gradient(135deg,#1a237e,#283593);border-radius:12px;overflow:hidden;margin-bottom:20px;">
                          <tr>
                            <td style="padding:20px;text-align:center;">
                              <p style="margin:0 0 14px;color:rgba(255,255,255,0.7);font-size:0.75rem;font-weight:700;text-transform:uppercase;letter-spacing:0.1em;">Match</p>
                              <table width="100%" cellpadding="0" cellspacing="0">
                                <tr>
                                  <td style="text-align:center;color:#ffffff;font-weight:800;font-size:1.1rem;text-transform:uppercase;letter-spacing:0.05em;">{homeTeam}</td>
                                  <td style="text-align:center;width:60px;">
                                    <span style="display:inline-block;background:rgba(255,255,255,0.12);border:1.5px solid rgba(255,255,255,0.25);color:rgba(255,255,255,0.8);font-size:0.75rem;font-weight:900;width:36px;height:36px;line-height:36px;border-radius:50%;text-align:center;">VS</span>
                                  </td>
                                  <td style="text-align:center;color:#ffffff;font-weight:800;font-size:1.1rem;text-transform:uppercase;letter-spacing:0.05em;">{awayTeam}</td>
                                </tr>
                              </table>
                              <p style="margin:14px 0 0;color:rgba(255,255,255,0.6);font-size:0.8rem;">🕐 {matchDate}</p>
                            </td>
                          </tr>
                        </table>

                        <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:24px;">
                          <tr>
                            <td width="50%" style="padding:12px;text-align:center;background:#f8f9ff;border-radius:10px 0 0 10px;border:1px solid #e8ecff;">
                              <div style="font-size:0.7rem;color:#aaa;font-weight:700;text-transform:uppercase;letter-spacing:0.08em;margin-bottom:4px;">Prize</div>
                              <div style="font-size:1rem;font-weight:800;color:#1a237e;">🏅 {prize}</div>
                            </td>
                            <td width="50%" style="padding:12px;text-align:center;background:#f8f9ff;border-radius:0 10px 10px 0;border:1px solid #e8ecff;border-left:none;">
                              <div style="font-size:0.7rem;color:#aaa;font-weight:700;text-transform:uppercase;letter-spacing:0.08em;margin-bottom:4px;">Draw Type</div>
                              <div style="font-size:1rem;font-weight:800;color:#1a237e;">🎯 Correct Score</div>
                            </td>
                          </tr>
                        </table>

                        <div style="text-align:center;">
                          <a href="{appUrl}/giveaway"
                             style="display:inline-block;padding:14px 36px;background:linear-gradient(135deg,#f5c518,#e6a800);color:#1a1a1a;font-weight:800;font-size:0.95rem;border-radius:12px;text-decoration:none;box-shadow:0 3px 10px rgba(245,197,24,0.4);">
                            ⚽ Enter Giveaway Now
                          </a>
                        </div>
                      </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                      <td style="background:#f8f9ff;padding:16px 24px;text-align:center;border-top:1px solid #e8ecff;">
                        <p style="margin:0;font-size:0.75rem;color:#aaa;">
                          One entry per account · 1 winner drawn from correct predictions<br/>
                          <a href="{appUrl}" style="color:#1a237e;text-decoration:none;font-weight:600;">Predict The Champion</a>
                        </p>
                      </td>
                    </tr>

                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """
        };

        message.Body = body.ToMessageBody();
        return message;
    }
}
