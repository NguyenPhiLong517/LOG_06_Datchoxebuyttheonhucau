using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using BusOnDemand.WinForms.Core;

namespace BusOnDemand.WinForms.Forms
{
    public class FrmDatGhe : Form
    {
        private readonly UserSession session;
        private DataGridView trips;
        private DataGridView bookings;
        private ToolStripStatusLabel lblStatusCount;

        public FrmDatGhe(UserSession user)
        {
            session = user;
            Ui.StyleForm(this, "Đặt ghế - Bus On Demand", 1240, 820);
            BuildLayout();
            Load += (s, e) => { LoadTrips(); LoadBookings(); };
        }

        private void BuildLayout()
        {
            // Combined Header
            string roleBadgeText = session.RoleLabel + " · " + session.HoTen + " (" + session.TenDangNhap + ")";
            var header = Ui.CreateCombinedHeader(
                "Đặt ghế",
                "Xem chuyến thuộc nhu cầu của bạn, kiểm tra số chỗ còn lại và đặt/hủy chỗ.",
                null,
                roleBadgeText,
                () => Close(),
                "ĐÓNG"
            );

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(28, 0, 28, 20),
                BackColor = Ui.Background
            };
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 53F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 47F));

            // Card 1: Chuyến có thể đặt
            var topCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Ui.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(20, 14, 20, 12),
                Margin = new Padding(0, 0, 0, 10)
            };

            var topHeaderPanel = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Ui.Surface };
            var title1 = new Label
            {
                Text = "Chuyến có thể đặt",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Ui.Navy,
                AutoSize = true,
                Location = new Point(0, 2)
            };
            var sub1 = new Label
            {
                Text = "Không chọn số ghế cụ thể; số chỗ còn lại được tính theo sức chứa và các chỗ đang giữ.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Ui.Muted,
                AutoSize = true,
                Location = new Point(2, 26)
            };
            topHeaderPanel.Controls.Add(title1);
            topHeaderPanel.Controls.Add(sub1);

            trips = Ui.Grid();
            Ui.StyleGrid(trips);

            var action1 = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                Padding = new Padding(0, 6, 0, 0),
                BackColor = Ui.Surface
            };
            var book = Ui.Button("ĐẶT GHẾ", Ui.Teal, 150);
            book.Click += BookSeat;
            var refresh1 = Ui.Button("LÀM MỚI", Ui.Navy2, 130);
            refresh1.Click += (s, e) => LoadTrips();
            action1.Controls.Add(book);
            action1.Controls.Add(refresh1);

            topCard.Controls.Add(trips);
            topCard.Controls.Add(action1);
            topCard.Controls.Add(topHeaderPanel);

            // Card 2: Đặt ghế của tôi
            var bottomCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Ui.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(20, 14, 20, 12),
                Margin = new Padding(0, 0, 0, 0)
            };

            var bottomHeaderPanel = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Ui.Surface };
            var title2 = new Label
            {
                Text = "Đặt ghế của tôi",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Ui.Navy,
                AutoSize = true,
                Location = new Point(0, 2)
            };
            var sub2 = new Label
            {
                Text = "Có thể hủy chỗ khi chuyến chưa khởi hành; hủy muộn được xử lý theo quy tắc 2 giờ.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Ui.Muted,
                AutoSize = true,
                Location = new Point(2, 26)
            };
            bottomHeaderPanel.Controls.Add(title2);
            bottomHeaderPanel.Controls.Add(sub2);

            bookings = Ui.Grid();
            Ui.StyleGrid(bookings);

            var action2 = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                Padding = new Padding(0, 6, 0, 0),
                BackColor = Ui.Surface
            };
            var cancel = Ui.Button("HỦY ĐẶT GHẾ", Ui.Red, 160);
            cancel.Click += CancelSeat;
            var refresh2 = Ui.Button("LÀM MỚI", Ui.Navy2, 130);
            refresh2.Click += (s, e) => LoadBookings();
            action2.Controls.Add(cancel);
            action2.Controls.Add(refresh2);

            bottomCard.Controls.Add(bookings);
            bottomCard.Controls.Add(action2);
            bottomCard.Controls.Add(bottomHeaderPanel);

            body.Controls.Add(topCard, 0, 0);
            body.Controls.Add(bottomCard, 0, 1);

            var statusStrip = Ui.CreateStatusStrip(out lblStatusCount);

            Controls.Add(header);
            Controls.Add(statusStrip);
            Controls.Add(body);
            body.BringToFront();
        }

        private void LoadTrips()
        {
            try
            {
                trips.DataSource = Db.Query(@"
SELECT c.MaChuyen, CONVERT(NVARCHAR(10), c.GioDuKien, 103) + ' ' + CONVERT(NVARCHAR(5), c.GioDuKien, 108) AS GioDuKien,
       dx.HuongDi, x.MaXe + N' · ' + CONVERT(NVARCHAR(10), x.SucChua) + N' chỗ' AS XeVaSucChua,
       x.SucChua - (SELECT COUNT(*) FROM dbo.DAT_CHO d WHERE d.MaChuyen=c.MaChuyen AND d.TrangThai=N'Đã giữ') AS ChoCon,
       CASE WHEN x.CoAccessibility=1 THEN N'Có hỗ trợ' ELSE N'Thông thường' END AS Accessibility,
       c.MaDeXuat
FROM dbo.CHUYEN c
JOIN dbo.DE_XUAT_CHUYEN dx ON dx.MaDeXuat=c.MaDeXuat
JOIN dbo.XE x ON x.MaXe=c.MaXe
WHERE c.TrangThai=N'Dự kiến' AND c.GioDuKien>SYSDATETIME()
  AND EXISTS (SELECT 1 FROM dbo.NHU_CAU_DI_LAI n WHERE n.MaHanhKhach=@user AND n.MaDeXuat=c.MaDeXuat AND n.TrangThai<>N'Đã hủy')
  AND NOT EXISTS (SELECT 1 FROM dbo.NHU_CAU_DI_LAI n WHERE n.MaHanhKhach=@user AND n.MaDeXuat=c.MaDeXuat AND n.CoAccessibility=1 AND n.TrangThai<>N'Đã hủy' AND x.CoAccessibility=0)
ORDER BY c.GioDuKien;", Db.P("@user", session.MaNguoiDung));

                if (trips.Columns.Contains("MaDeXuat")) trips.Columns["MaDeXuat"].Visible = false;
                Rename(trips, "MaChuyen", "Mã chuyến");
                Rename(trips, "GioDuKien", "Giờ dự kiến");
                Rename(trips, "HuongDi", "Hướng đi");
                Rename(trips, "XeVaSucChua", "Xe / sức chứa");
                Rename(trips, "ChoCon", "Chỗ còn");
                Rename(trips, "Accessibility", "Hỗ trợ tiếp cận");
                Ui.StyleGrid(trips);

                if (trips.Columns.Contains("MaChuyen")) trips.Columns["MaChuyen"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (trips.Columns.Contains("GioDuKien")) trips.Columns["GioDuKien"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (trips.Columns.Contains("HuongDi")) trips.Columns["HuongDi"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (trips.Columns.Contains("ChoCon")) trips.Columns["ChoCon"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (trips.Columns.Contains("Accessibility")) trips.Columns["Accessibility"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                UpdateStatusBar();
            }
            catch (Exception ex) { Ui.Error(ex); }
        }

        private void LoadBookings()
        {
            try
            {
                bookings.DataSource = Db.Query(@"
SELECT d.MaDatCho, d.MaChuyen, CONVERT(NVARCHAR(10), c.GioDuKien, 103) + ' ' + CONVERT(NVARCHAR(5), c.GioDuKien, 108) AS GioKhoiHanh,
       CASE WHEN d.TrangThai=N'Đã hủy' AND EXISTS(SELECT 1 FROM dbo.VI_PHAM_NO_SHOW v WHERE v.MaDatCho=d.MaDatCho AND v.Loai=N'Hủy muộn') THEN N'Đã hủy muộn'
            WHEN d.TrangThai=N'Đã hủy muộn' THEN N'Đã hủy muộn'
            WHEN d.TrangThai=N'Vắng mặt' OR d.TrangThai=N'No-show' THEN N'No-show'
            WHEN d.TrangThai=N'Đã sử dụng' OR d.TrangThai=N'Đã hoàn thành' THEN N'Đã hoàn thành'
            ELSE d.TrangThai END AS TrangThaiHienThi,
       CASE WHEN EXISTS(SELECT 1 FROM dbo.VI_PHAM_NO_SHOW v WHERE v.MaDatCho=d.MaDatCho AND v.Loai=N'Hủy muộn') THEN N'Hủy sát giờ' ELSE N'' END AS GhiChu
FROM dbo.DAT_CHO d JOIN dbo.CHUYEN c ON c.MaChuyen=d.MaChuyen
WHERE d.MaHanhKhach=@user ORDER BY d.ThoiDiemDat DESC;", Db.P("@user", session.MaNguoiDung));

                Rename(bookings, "MaDatCho", "Mã đặt chỗ");
                Rename(bookings, "MaChuyen", "Mã chuyến");
                Rename(bookings, "GioKhoiHanh", "Giờ khởi hành");
                Rename(bookings, "TrangThaiHienThi", "Trạng thái");
                Rename(bookings, "GhiChu", "Ghi chú");
                Ui.StyleGrid(bookings);

                if (bookings.Columns.Contains("MaDatCho")) bookings.Columns["MaDatCho"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (bookings.Columns.Contains("MaChuyen")) bookings.Columns["MaChuyen"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (bookings.Columns.Contains("GioKhoiHanh")) bookings.Columns["GioKhoiHanh"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (bookings.Columns.Contains("TrangThaiHienThi")) bookings.Columns["TrangThaiHienThi"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                UpdateStatusBar();
            }
            catch (Exception ex) { Ui.Error(ex); }
        }

        private void UpdateStatusBar()
        {
            if (lblStatusCount == null) return;
            int nTrips = trips != null && trips.DataSource is DataTable tDt ? tDt.Rows.Count : 0;
            int nBookings = bookings != null && bookings.DataSource is DataTable bDt ? bDt.Rows.Count : 0;
            lblStatusCount.Text = string.Format("Chuyến có thể đặt: {0}  |  Vé của tôi: {1}  |  Tài khoản: {2}", nTrips, nBookings, session.TenDangNhap);
        }

        private void BookSeat(object sender, EventArgs e)
        {
            string trip = Ui.Cell(trips, "MaChuyen");
            if (String.IsNullOrWhiteSpace(trip))
            {
                MessageBox.Show("Hãy chọn chuyến cần đặt ghế.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                DataTable result = Db.Procedure("dbo.sp_DatCho", Db.P("@MaChuyen", trip), Db.P("@MaHanhKhach", session.MaNguoiDung));
                string message = result.Rows.Count > 0 && result.Columns.Contains("KetQua") ? Convert.ToString(result.Rows[0]["KetQua"]) : "Đặt ghế thành công.";
                MessageBox.Show(message, "Đặt ghế", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadTrips();
                LoadBookings();
            }
            catch (Exception ex)
            {
                Ui.Error(ex, "Không thể đặt ghế");
            }
        }

        private void CancelSeat(object sender, EventArgs e)
        {
            string booking = Ui.Cell(bookings, "MaDatCho");
            if (String.IsNullOrWhiteSpace(booking))
            {
                MessageBox.Show("Hãy chọn đặt chỗ cần hủy.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                DataTable dt = Db.Query(@"SELECT d.TrangThai, c.GioDuKien FROM dbo.DAT_CHO d JOIN dbo.CHUYEN c ON c.MaChuyen=d.MaChuyen WHERE d.MaDatCho=@id AND d.MaHanhKhach=@user;",
                    Db.P("@id", booking), Db.P("@user", session.MaNguoiDung));

                if (dt.Rows.Count == 0 || Convert.ToString(dt.Rows[0]["TrangThai"]) != "Đã giữ")
                {
                    MessageBox.Show("Chỉ đặt chỗ đang ở trạng thái “Đã giữ” mới được hủy.", "Không thể hủy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DateTime scheduled = Convert.ToDateTime(dt.Rows[0]["GioDuKien"]);
                int limitHours = Db.ReadConfigInt("HAN_HUY_GIO", 2);
                bool late = DateTime.Now > scheduled.AddHours(-limitHours);

                string prompt = late
                    ? "Đã còn dưới " + limitHours + " giờ trước khởi hành. Hủy sát giờ sẽ được ghi nhận một lần vi phạm no-show theo quy tắc hệ thống. Bạn có xác nhận hủy không?"
                    : "Bạn có chắc muốn hủy đặt chỗ này?";

                if (MessageBox.Show(prompt, "Xác nhận hủy đặt chỗ", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                DataTable result = Db.Procedure("dbo.sp_HuyDatCho", Db.P("@MaDatCho", booking));
                string msg = result.Rows.Count > 0 && result.Columns.Contains("KetQua") ? Convert.ToString(result.Rows[0]["KetQua"]) : "Đã gửi yêu cầu hủy.";
                if (result.Rows.Count > 0 && result.Columns.Contains("KetQuaViPham"))
                    msg += Environment.NewLine + Convert.ToString(result.Rows[0]["KetQuaViPham"]);

                MessageBox.Show(msg, "Kết quả hủy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadTrips();
                LoadBookings();
            }
            catch (Exception ex)
            {
                Ui.Error(ex, "Không thể hủy đặt ghế");
            }
        }

        private static void Rename(DataGridView grid, string source, string target)
        {
            if (grid.Columns.Contains(source)) grid.Columns[source].HeaderText = target;
        }
    }
}
