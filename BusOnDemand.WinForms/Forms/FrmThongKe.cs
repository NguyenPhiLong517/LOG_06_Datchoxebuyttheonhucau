using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using BusOnDemand.WinForms.Core;

namespace BusOnDemand.WinForms.Forms
{
    public class FrmThongKe : Form
    {
        private readonly UserSession session;
        private DateTimePicker dpFrom, dpTo;
        private Label[] statValues;
        private DataGridView grid;
        private ToolStripStatusLabel lblStatusCount;

        public FrmThongKe(UserSession user)
        {
            session = user;
            Ui.StyleForm(this, "Báo cáo thống kê - Bus On Demand", 1240, 840);
            BuildLayout();
            Load += (s, e) => LoadReport();
        }

        private void BuildLayout()
        {
            // Combined Header
            string roleBadgeText = session.RoleLabel + " · " + session.HoTen + " (" + session.TenDangNhap + ")";
            var header = Ui.CreateCombinedHeader(
                "Báo cáo thống kê",
                "Tổng hợp nhu cầu, chuyến, tỉ lệ lấp đầy, no-show và nhu cầu hỗ trợ theo thời gian.",
                null,
                roleBadgeText,
                () => Close(),
                "ĐÓNG"
            );

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(28, 0, 28, 20),
                BackColor = Ui.Background
            };
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 130F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Filter Card
            var filterCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Ui.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(20, 12, 20, 12),
                Margin = new Padding(0, 0, 0, 12)
            };

            var filterTitle = new Label
            {
                Text = "Khoảng thời gian thống kê",
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = Ui.Navy,
                AutoSize = true,
                Location = new Point(20, 10)
            };

            var filterFields = new FlowLayoutPanel
            {
                Location = new Point(20, 36),
                Size = new Size(1150, 56),
                BackColor = Ui.Surface,
                WrapContents = false
            };

            dpFrom = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                Font = new Font("Segoe UI", 9.5F),
                Value = DateTime.Today.AddDays(-30)
            };
            filterFields.Controls.Add(Ui.FieldBox("TỪ NGÀY", dpFrom, 190));

            dpTo = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                Font = new Font("Segoe UI", 9.5F),
                Value = DateTime.Today
            };
            filterFields.Controls.Add(Ui.FieldBox("ĐẾN NGÀY", dpTo, 190));

            var runPanel = new Panel { Width = 180, Height = 64, BackColor = Ui.Surface };
            var run = Ui.Button("TỔNG HỢP", Ui.Teal, 160, 36);
            run.Location = new Point(0, 18);
            run.Click += (s, e) => LoadReport();
            runPanel.Controls.Add(run);
            filterFields.Controls.Add(runPanel);

            filterCard.Controls.Add(filterTitle);
            filterCard.Controls.Add(filterFields);

            // 5 KPI Cards Row
            var statsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                BackColor = Ui.Background,
                Padding = new Padding(0),
                Margin = new Padding(0, 0, 0, 12)
            };
            for (int i = 0; i < 5; i++) statsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

            string[] names = { "NHU CẦU", "CHUYẾN XE", "TỈ LỆ LẤP ĐẦY", "VI PHẠM", "CẦN HỖ TRỢ" };
            string[] descs = { "Tổng nhu cầu đi lại", "Chuyến không bị hủy", "Trung bình chỗ đã giữ", "No-show / hủy muộn", "Nhu cầu accessibility" };
            statValues = new Label[5];

            for (int i = 0; i < 5; i++)
            {
                var card = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Ui.Surface,
                    BorderStyle = BorderStyle.FixedSingle,
                    Margin = new Padding(i == 0 ? 0 : 7, 0, i == 4 ? 0 : 7, 0),
                    Padding = new Padding(16, 12, 16, 10)
                };

                var headerLab = new Label
                {
                    Text = names[i],
                    Dock = DockStyle.Top,
                    Height = 22,
                    ForeColor = Ui.Muted,
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold)
                };

                statValues[i] = new Label
                {
                    Text = "—",
                    Dock = DockStyle.Fill,
                    ForeColor = i == 3 ? Ui.Red : (i == 4 ? Ui.Teal : Ui.Navy),
                    Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft
                };

                var descLab = new Label
                {
                    Text = descs[i],
                    Dock = DockStyle.Bottom,
                    Height = 20,
                    ForeColor = Ui.Muted,
                    Font = new Font("Segoe UI", 8F)
                };

                card.Controls.Add(statValues[i]);
                card.Controls.Add(headerLab);
                card.Controls.Add(descLab);

                statsPanel.Controls.Add(card, i, 0);
            }

            // Detail Grid Card
            var detailCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Ui.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(20, 14, 20, 12),
                Margin = new Padding(0, 0, 0, 0)
            };

            var detailHeader = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Ui.Surface };
            var caption = new Label
            {
                Text = "Chi tiết theo ngày / hướng / khung giờ",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Ui.Navy,
                AutoSize = true,
                Location = new Point(0, 2)
            };
            var detailSubtitle = new Label
            {
                Text = "Bảng tổng hợp phục vụ theo dõi vận hành, không bổ sung chức năng xuất file.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Ui.Muted,
                AutoSize = true,
                Location = new Point(2, 26)
            };
            detailHeader.Controls.Add(caption);
            detailHeader.Controls.Add(detailSubtitle);

            grid = Ui.Grid();
            Ui.StyleGrid(grid);

            detailCard.Controls.Add(grid);
            detailCard.Controls.Add(detailHeader);

            body.Controls.Add(filterCard, 0, 0);
            body.Controls.Add(statsPanel, 0, 1);
            body.Controls.Add(detailCard, 0, 2);

            var statusStrip = Ui.CreateStatusStrip(out lblStatusCount);

            Controls.Add(header);
            Controls.Add(statusStrip);
            Controls.Add(body);
            body.BringToFront();
        }

        private void LoadReport()
        {
            if (dpTo.Value.Date < dpFrom.Value.Date)
            {
                MessageBox.Show("Ngày kết thúc không được trước ngày bắt đầu.", "Kiểm tra dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                DateTime from = dpFrom.Value.Date, to = dpTo.Value.Date;

                statValues[0].Text = Convert.ToString(Db.Scalar("SELECT COUNT(*) FROM dbo.NHU_CAU_DI_LAI WHERE NgayDi BETWEEN @from AND @to;", Db.P("@from", from), Db.P("@to", to)));
                statValues[1].Text = Convert.ToString(Db.Scalar("SELECT COUNT(*) FROM dbo.CHUYEN WHERE CAST(GioDuKien AS DATE) BETWEEN @from AND @to AND TrangThai<>N'Đã hủy';", Db.P("@from", from), Db.P("@to", to)));

                object fill = Db.Scalar(@"SELECT AVG(100.0 * q.SoChoDaGiu / NULLIF(x.SucChua,0))
FROM dbo.CHUYEN c JOIN dbo.XE x ON x.MaXe=c.MaXe
CROSS APPLY (SELECT COUNT(*) AS SoChoDaGiu FROM dbo.DAT_CHO d WHERE d.MaChuyen=c.MaChuyen AND d.TrangThai IN (N'Đã giữ',N'Đã sử dụng',N'Đã hoàn thành',N'Vắng mặt',N'No-show')) q
WHERE CAST(c.GioDuKien AS DATE) BETWEEN @from AND @to AND c.TrangThai<>N'Đã hủy';", Db.P("@from", from), Db.P("@to", to));
                statValues[2].Text = fill == DBNull.Value || fill == null ? "0.0%" : Convert.ToDecimal(fill).ToString("0.0") + "%";

                statValues[3].Text = Convert.ToString(Db.Scalar("SELECT COUNT(*) FROM dbo.VI_PHAM_NO_SHOW WHERE CAST(NgayGhiNhan AS DATE) BETWEEN @from AND @to;", Db.P("@from", from), Db.P("@to", to)));

                statValues[4].Text = Convert.ToString(Db.Scalar(@"SELECT COUNT(DISTINCT n.MaNhuCau) FROM dbo.NHU_CAU_DI_LAI n
JOIN dbo.CHUYEN c ON c.MaDeXuat=n.MaDeXuat JOIN dbo.XE x ON x.MaXe=c.MaXe
WHERE n.NgayDi BETWEEN @from AND @to AND n.CoAccessibility=1 AND x.CoAccessibility=1 AND c.TrangThai IN (N'Dự kiến',N'Đã khởi hành',N'Đã hoàn thành');", Db.P("@from", from), Db.P("@to", to)));

                grid.DataSource = Db.Query(@"SELECT CONVERT(NVARCHAR(10), dx.NgayDi, 103) AS NgayDi, dx.HuongDi, dx.KhungGio,
 (SELECT COUNT(*) FROM dbo.NHU_CAU_DI_LAI n WHERE n.MaDeXuat=dx.MaDeXuat AND n.TrangThai<>N'Đã hủy') AS SoNhuCau,
 (SELECT COUNT(*) FROM dbo.CHUYEN c WHERE c.MaDeXuat=dx.MaDeXuat AND c.TrangThai<>N'Đã hủy') AS SoChuyen,
 (SELECT COUNT(*) FROM dbo.VI_PHAM_NO_SHOW v JOIN dbo.DAT_CHO d ON d.MaDatCho=v.MaDatCho JOIN dbo.CHUYEN c ON c.MaChuyen=d.MaChuyen WHERE c.MaDeXuat=dx.MaDeXuat AND CAST(v.NgayGhiNhan AS DATE) BETWEEN @from AND @to) AS SoViPham
FROM dbo.DE_XUAT_CHUYEN dx WHERE dx.NgayDi BETWEEN @from AND @to ORDER BY dx.NgayDi, dx.HuongDi, dx.KhungGio;", Db.P("@from", from), Db.P("@to", to));

                Rename("NgayDi", "Ngày đi");
                Rename("HuongDi", "Hướng đi");
                Rename("KhungGio", "Khung giờ");
                Rename("SoNhuCau", "Số nhu cầu");
                Rename("SoChuyen", "Số chuyến");
                Rename("SoViPham", "Số vi phạm");
                Ui.StyleGrid(grid);

                foreach (DataGridViewColumn col in grid.Columns)
                {
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                if (lblStatusCount != null)
                {
                    lblStatusCount.Text = string.Format("Kỳ báo cáo: {0} đến {1}  |  Điều phối viên: {2}", dpFrom.Value.ToString("dd/MM/yyyy"), dpTo.Value.ToString("dd/MM/yyyy"), session.TenDangNhap);
                }
            }
            catch (Exception ex)
            {
                Ui.Error(ex, "Không thể tổng hợp báo cáo");
            }
        }

        private void Rename(string a, string b)
        {
            if (grid.Columns.Contains(a)) grid.Columns[a].HeaderText = b;
        }
    }
}
