using System;
using System.Windows.Forms;
using BusOnDemand.WinForms.Core;
using BusOnDemand.WinForms.Forms;

namespace BusOnDemand.WinForms
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            while (true)
            {
                UserSession session;
                using (var login = new FrmDangNhap())
                {
                    if (login.ShowDialog() != DialogResult.OK || login.AuthenticatedSession == null)
                        break;
                    session = login.AuthenticatedSession;
                }

                GroupingScheduler.Start();
                try
                {
                    var main = new FrmMain(session);
                    Application.Run(main);
                    if (!main.LoggedOut)
                        break;
                }
                finally
                {
                    GroupingScheduler.Stop();
                }
            }
        }
    }
}
