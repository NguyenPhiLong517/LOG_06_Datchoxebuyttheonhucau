using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;

namespace BusOnDemand.WinForms.Core
{
    // UC04: scheduler for grouping pending demand requests while the WinForms application is running.
    public static class GroupingScheduler
    {
        private static Timer timer;
        private static DateTime lastRun = DateTime.MinValue;
        private static int running;
        public static string LastError { get; private set; }

        public static void Start()
        {
            if (timer != null) return;
            timer = new Timer(_ => Tick(), null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        public static void Stop()
        {
            var old = Interlocked.Exchange(ref timer, null);
            if (old != null) old.Dispose();
        }

        private static void Tick()
        {
            if (Interlocked.Exchange(ref running, 1) != 0) return;
            try
            {
                int interval = Db.ReadConfigInt("CHU_KY_JOB_PHUT", 15);
                if (lastRun == DateTime.MinValue || DateTime.Now - lastRun >= TimeSpan.FromMinutes(interval))
                {
                    RunOnce();
                    lastRun = DateTime.Now;
                    LastError = null;
                }
            }
            catch (Exception ex) { LastError = ex.Message; }
            finally { Interlocked.Exchange(ref running, 0); }
        }

        // Public for controlled test/debugging; the scheduler itself calls it according to CHU_KY_JOB_PHUT.
        public static void RunOnce()
        {
            int minGroup = Db.ReadConfigInt("NGUONG_GOM_NHOM", 4);
            int closeDays = Db.ReadConfigInt("SO_NGAY_DONG_NHOM", 3);
            using (var cn = Db.Open())
            using (var tx = cn.BeginTransaction())
            {
                try
                {
                    // A need whose travel date has passed cannot remain waiting for a future group.
                    Db.Execute(cn, tx, @"UPDATE dbo.NHU_CAU_DI_LAI
SET TrangThai=N'Không đủ nhóm'
WHERE TrangThai=N'Chưa gom nhóm' AND NgayDi<CAST(SYSDATETIME() AS DATE);");

                    // Reject overdue proposals and close their linked request groups.
                    Db.Execute(cn, tx, @"UPDATE dbo.DE_XUAT_CHUYEN
SET TrangThai=N'Từ chối'
WHERE TrangThai=N'Chờ duyệt' AND NgayTao<DATEADD(DAY,-@days,SYSDATETIME());", Db.P("@days", closeDays));
                    Db.Execute(cn, tx, @"UPDATE n SET TrangThai=N'Không đủ nhóm'
FROM dbo.NHU_CAU_DI_LAI n JOIN dbo.DE_XUAT_CHUYEN dx ON dx.MaDeXuat=n.MaDeXuat
WHERE dx.TrangThai=N'Từ chối' AND n.TrangThai=N'Đã gom nhóm';");

                    DataTable groups = Db.Query(cn, tx, @"SELECT NgayDi,HuongDi,KhungGioMongMuon,
COUNT(*) AS SoNhuCau,SUM(CASE WHEN CoAccessibility=1 THEN 1 ELSE 0 END) AS SoCanHoTro,
MIN(ThoiDiemTao) AS TaoLucSomNhat
FROM dbo.NHU_CAU_DI_LAI
WHERE TrangThai=N'Chưa gom nhóm' AND NgayDi>=CAST(SYSDATETIME() AS DATE)
GROUP BY NgayDi,HuongDi,KhungGioMongMuon;");

                    foreach (DataRow g in groups.Rows)
                    {
                        DateTime day = Convert.ToDateTime(g["NgayDi"]).Date;
                        string direction = Convert.ToString(g["HuongDi"]);
                        string slot = Convert.ToString(g["KhungGioMongMuon"]);
                        int count = Convert.ToInt32(g["SoNhuCau"]);
                        int accessibilityCount = Convert.ToInt32(g["SoCanHoTro"]);
                        DateTime oldest = Convert.ToDateTime(g["TaoLucSomNhat"]);

                        if (count < minGroup)
                        {
                            if (oldest < DateTime.Now.AddDays(-closeDays))
                            {
                                Db.Execute(cn, tx, @"UPDATE dbo.NHU_CAU_DI_LAI SET TrangThai=N'Không đủ nhóm'
WHERE TrangThai=N'Chưa gom nhóm' AND NgayDi=@day AND HuongDi=@dir AND KhungGioMongMuon=@slot;",
                                    Db.P("@day", day), Db.P("@dir", direction), Db.P("@slot", slot));
                            }
                            continue;
                        }

                        object existing = Db.Scalar(cn, tx, @"SELECT TOP(1) MaDeXuat FROM dbo.DE_XUAT_CHUYEN
WHERE NgayDi=@day AND HuongDi=@dir AND KhungGio=@slot AND TrangThai IN (N'Chờ duyệt',N'Đã duyệt')
ORDER BY CASE WHEN TrangThai=N'Chờ duyệt' THEN 0 ELSE 1 END, NgayTao;",
                            Db.P("@day", day), Db.P("@dir", direction), Db.P("@slot", slot));
                        string proposal;
                        if (existing != null && existing != DBNull.Value)
                        {
                            proposal = Convert.ToString(existing);
                        }
                        else
                        {
                            proposal = "DX" + Guid.NewGuid().ToString("N").Substring(0, 14).ToUpperInvariant();
                            bool noAccessibleCar = accessibilityCount > 0 && Convert.ToInt32(Db.Scalar(cn, tx, @"SELECT COUNT(*) FROM dbo.XE
WHERE CoAccessibility=1 AND TrangThai NOT IN (N'Bảo trì',N'Ngưng sử dụng');")) == 0;
                            Db.Execute(cn, tx, @"INSERT INTO dbo.DE_XUAT_CHUYEN
(MaDeXuat,NgayDi,HuongDi,KhungGio,SoCanHoTro,CanhBaoThieuXe,TrangThai,NgayTao)
VALUES(@id,@day,@dir,@slot,@access,@warning,N'Chờ duyệt',SYSDATETIME());",
                                Db.P("@id", proposal), Db.P("@day", day), Db.P("@dir", direction), Db.P("@slot", slot),
                                Db.P("@access", accessibilityCount), Db.P("@warning", noAccessibleCar));
                        }

                        bool hasTrip = Convert.ToInt32(Db.Scalar(cn, tx, "SELECT COUNT(*) FROM dbo.CHUYEN WHERE MaDeXuat=@id AND TrangThai<>N'Đã hủy';", Db.P("@id", proposal))) > 0;
                        string requestStatus = hasTrip ? "Đã mở chuyến" : "Đã gom nhóm";
                        Db.Execute(cn, tx, @"UPDATE dbo.NHU_CAU_DI_LAI SET MaDeXuat=@proposal,TrangThai=@status
WHERE TrangThai=N'Chưa gom nhóm' AND NgayDi=@day AND HuongDi=@dir AND KhungGioMongMuon=@slot;",
                            Db.P("@proposal", proposal), Db.P("@status", requestStatus), Db.P("@day", day), Db.P("@dir", direction), Db.P("@slot", slot));
                    }
                    tx.Commit();
                }
                catch
                {
                    if (tx.Connection != null) tx.Rollback();
                    throw;
                }
            }
        }
    }
}
