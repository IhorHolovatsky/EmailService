using System;
using System.Threading.Tasks;

namespace EmailService.App.Repositories
{
    public interface IMonitoringRepository
    {
        Task InitAsync(string envrionment, string appName);
        Task SendImAliveAsync(string envrionment, string appName, DateTime dateTime);
    }
}