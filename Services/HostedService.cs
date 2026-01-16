using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Zoolirante_Open_Minded.Models;
using Zoolirante_Open_Minded.Services;

public class EmailSchedulerService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private Timer? _timer;

    public EmailSchedulerService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(SendPendingEmails, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        return Task.CompletedTask;
    }

    private void SendPendingEmails(object? state)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZooliranteDatabaseContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var now = DateTime.Now;
        var pending = db.PendingEmails
            .Where(e => !e.Sent && e.ScheduledTime <= now)
            .ToList();

        foreach (var item in pending)
        {
            var user = db.Users.Find(item.UserId);
            var ticket = db.EntranceTicket.Find(item.TicketId);
            if (user != null && ticket != null)
            {
                emailService.SendTicketConfirmationAsync(user, ticket);
                item.Sent = true;
            }
        }

        db.SaveChanges();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
