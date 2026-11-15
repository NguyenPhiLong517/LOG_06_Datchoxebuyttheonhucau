using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using BusOnDemand.WinForms.Core;

namespace BusOnDemand.WinForms.Forms
{
    public class FrmNhuCau : Form
    {
        private readonly UserSession session;
        private DataGridView grid;
        private DateTimePicker dpDate;
        private DateTimePicker dpTime;
        private ComboBox cbDirection;
        private CheckBox chkAccessibility;
        private ToolStripStatusLabel lblStatusCount;

        public FrmNhuCau(UserSession user)
        {
            session = user;
            Ui.StyleForm(this, "Nhu cầu di chuyển - Bus On Demand", 1240, 800);
            BuildLayout();
            Load += (s, e) => LoadNeeds();
        }

        private void BuildLayout()
        {
            // Combined Header (TopBar + PageHeader)
            string roleBadgeText = session.RoleLabel + " · " + session.HoTen + " (" + session.TenDangNhap + ")";
            var header = Ui.CreateCombinedHeader(
                "Nhu cầu di chuyển",
                "Khai báo, tra cứu và quản lý nhu cầu đi lại theo khung giờ hệ thống.",
                null,
                roleBadgeText,
                () => Close(),
                "ĐÓNG"
            );

            // TableLayoutPanel body guarantees inputCard is never covered by gridCard
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(28, 0, 28, 20),
                BackColor = Ui.Background
            };
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 185F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Card 1: Khai báo nhu cầu (Input Card)
            var inputCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Ui.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(22, 14, 22, 12),
                Margin = new Padding(0, 0, 0, 14)
            };

            var inputTitle = new Label
            {
                Text = "Khai báo nhu cầu",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Ui.Navy,
                AutoSize = true,
                Location = new Point(22, 12)
            };

            var inputSubtitle = new Label
            {
                Text = "Các nhu cầu được gom nhóm theo ngày đi, hướng đi và khung giờ đã cấu hình.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Ui.Muted,
                AutoSize = true,
                Location = new Point(24, 36)
            };

            var fields = new FlowLayoutPanel
            {
                Location = new Point(22, 62),
                Size = new Size(1140, 70),
                BackColor = Ui.Surface,
                WrapContents = false
            };

            // Field 1: Ngày đi
            dpDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                MinDate = DateTime.Today,
                Value = DateTime.Today.AddDays(1),
                Font = new Font("Segoe UI", 9.5F)
            };
            fields.Controls.Add(Ui.FieldBox("NGÀY ĐI", dpDate, 175));

            // Field 2: Hướng đi
            cbDirection = Ui.Combo();
            cbDirection.Items.AddRange(new object[] { "Chiều đi", "Chiều về" });
            cbDirection.SelectedIndex = 0;
            fields.Controls.Add(Ui.FieldBox("HƯỚNG ĐI", cbDirection, 185));

            // Field 3: Khung giờ mong muốn
            dpTime = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "HH:mm",
                ShowUpDown = true,
                Value = DateTime.Today.AddHours(7).AddMinutes(30),
                Font = new Font("Segoe UI", 9.5F)
            };
            fields.Controls.Add(Ui.FieldBox("KHUNG GIỜ MONG MUỐN", dpTime, 205));

            // Field 4: Accessibility Checkbox
            var chkBoxPanel = new Panel { Width = 180, Height = 64, BackColor = Ui.Surface };
            chkAccessibility = new CheckBox
            {
                Text = "Cần hỗ trợ tiếp cận",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Ui.Text,
                Location = new Point(0, 24)
            };
            chkBoxPanel.Controls.Add(chkAccessibility);
            fields.Controls.Add(chkBoxPanel);

            // Field 5: Submit Button
            var submitPanel = new Panel { Width = 180, Height = 64, BackColor = Ui.Surface };
            var submit = Ui.Button("GỬI NHU CẦU", Ui.Teal, 160, 36);
            submit.Location = new Point(0, 18);
            submit.Click += CreateNeed;
            submitPanel.Controls.Add(submit);
            fields.Controls.Add(submitPanel);

            var hint = new Label
            {
                Text = "Chỉ hủy nhu cầu chưa gom nhóm. Khung giờ được kiểm tra theo cấu hình hệ thống.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Ui.Muted,
                AutoSize = true,
                Location = new Point(24, 142)
            };

            inputCard.Controls.Add(inputTitle);
            inputCard.Controls.Add(inputSubtitle);
            inputCard.Controls.Add(fields);
            inputCard.Controls.Add(hint);

            // Card 2: Nhu cầu của tôi (Grid Card)
            var gridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Ui.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(22, 14, 22, 14)
            };

            var gridHeaderPanel = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Ui.Surface };
            var gridTitle = new Label
            {
                Text = "Nhu cầu của tôi",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Ui.Navy,
                AutoSize = true,
                Location = new Point(0, 2)
            };
            var gridSubtitle = new Label
            {
                Text = "Chọn một nhu cầu để hủy nếu đang ở trạng thái cho phép.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Ui.Muted,
                AutoSize = true,
                Location = new Point(2, 26)
            };
            gridHeaderPanel.Controls.Add(gridTitle);
            gridHeaderPanel.Controls.Add(gridSubtitle);

            grid = Ui.Grid();
            Ui.StyleGrid(grid);

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Ui.Surface,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 8, 0, 0)
            };

            var cancel = Ui.Button("HỦY NHU CẦU", Ui.Red, 165);
            cancel.Click += CancelNeed;

            var refresh = Ui.Button("LÀM MỚI DANH SÁCH", Ui.Navy2, 200);
            refresh.Click += (s, e) => LoadNeeds();

            actions.Controls.Add(cancel);
            actions.Controls.Add(refresh);

            gridCard.Controls.Add(grid);
            gridCard.Controls.Add(actions);
            gridCard.Controls.Add(gridHeaderPanel);

            body.Controls.Add(inputCard, 0, 0);
            body.Controls.Add(gridCard, 0, 1);

            var statusStrip = Ui.CreateStatusStrip(out lblStatusCount);

            Controls.Add(header);
            Controls.Add(statusStrip);
            Controls.Add(body);
            body.BringToFront();
        }

        private void LoadNeeds()
        {
            try
            {
                grid.DataSource = Db.Query(@"
SELECT n.MaNhuCau, CONVERT(NVARCHAR(10), n.NgayDi, 103) AS NgayDi, n.HuongDi, n.KhungGioMongMuon,
       CASE WHEN n.CoAccessibility=1 THEN N'Có' ELSE N'Không' END AS HoTroTiepCan,
       CASE
          WHEN n.TrangThai=N'Đã hủy' THEN N'Đã hủy'
          WHEN n.TrangThai=N'Không đủ nhóm' THEN N'Không đủ nhóm'
          WHEN EXISTS (SELECT 1 FROM dbo.DE_XUAT_CHUYEN dx WHERE dx.MaDeXuat=n.MaDeXuat AND dx.TrangThai IN (N'Không đủ nhu cầu',N'Từ chối')) THEN N'Không đủ nhóm'
          WHEN EXISTS (SELECT 1 FROM dbo.CHUYEN c WHERE c.MaDeXuat=n.MaDeXuat AND c.TrangThai<>N'Đã hủy') THEN N'Đã mở chuyến'
          ELSE n.TrangThai END AS TrangThaiHienThi,
       CONVERT(NVARCHAR(10), n.ThoiDiemTao, 103) + ' ' + CONVERT(NVARCHAR(5), n.ThoiDiemTao, 108) AS ThoiDiemTao, n.MaDeXuat
FROM dbo.NHU_CAU_DI_LAI n
WHERE n.MaHanhKhach=@user
ORDER BY n.ThoiDiemTao DESC;", Db.P("@user", session.MaNguoiDung));

                if (grid.Columns.Contains("MaDeXuat")) grid.Columns["MaDeXuat"].Visible = false;
                if (grid.Columns.Contains("ThoiDiemTao")) grid.Columns["ThoiDiemTao"].HeaderText = "Thời điểm khai báo";
                if (grid.Columns.Contains("MaNhuCau")) grid.Columns["MaNhuCau"].HeaderText = "Mã nhu cầu";
                if (grid.Columns.Contains("NgayDi")) grid.Columns["NgayDi"].HeaderText = "Ngày đi";
                if (grid.Columns.Contains("HuongDi")) grid.Columns["HuongDi"].HeaderText = "Hướng đi";
                if (grid.Columns.Contains("KhungGioMongMuon")) grid.Columns["KhungGioMongMuon"].HeaderText = "Khung giờ";
                if (grid.Columns.Contains("HoTroTiepCan")) grid.Columns["HoTroTiepCan"].HeaderText = "Hỗ trợ tiếp cận";
                if (grid.Columns.Contains("TrangThaiHienThi")) grid.Columns["TrangThaiHienThi"].HeaderText = "Trạng thái";

                Ui.StyleGrid(grid);

                foreach (DataGridViewColumn col in grid.Columns)
                {
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                var dt = grid.DataSource as DataTable;
                if (lblStatusCount != null && dt != null)
                {
                    lblStatusCount.Text = string.Format("Tổng cộng: {0} nhu cầu  |  Tài khoản: {1}", dt.Rows.Count, session.TenDangNhap);
                }
            }
            catch (Exception ex) { Ui.Error(ex); }
        }

        private void CreateNeed(object sender, EventArgs e)
        {
            if (dpDate.Value.Date < DateTime.Today)
            {
                MessageBox.Show("Ngày đi không được nằm trong quá khứ.", "Kiểm tra dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int width = Db.ReadConfigInt("DO_RONG_KHUNG_GIO_PHUT", 30);
            if (width <= 0 || dpTime.Value.Minute % width != 0)
            {
                MessageBox.Show("Khung giờ chưa phù hợp độ rộng đang cấu hình (" + width + " phút). Hãy chọn mốc giờ hợp lệ (ví dụ phút chia hết cho " + width + ").", "Kiểm tra dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string time = dpTime.Value.ToString("HH:mm", CultureInfo.InvariantCulture);

            try
            {
                object duplicate = Db.Scalar(@"SELECT COUNT(*) FROM dbo.NHU_CAU_DI_LAI
WHERE MaHanhKhach=@user AND NgayDi=@date AND HuongDi=@dir AND KhungGioMongMuon=@time AND TrangThai=N'Chưa gom nhóm';",
                    Db.P("@user", session.MaNguoiDung), Db.P("@date", dpDate.Value.Date), Db.P("@dir", Convert.ToString(cbDirection.SelectedItem)), Db.P("@time", time));

                if (Convert.ToInt32(duplicate) > 0)
                {
                    MessageBox.Show("Bạn đã có nhu cầu cùng ngày, hướng và khung giờ đang chờ xử lý.", "Không tạo nhu cầu trùng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string id = "NC" + Guid.NewGuid().ToString("N").Substring(0, 16).ToUpperInvariant();
                Db.Execute(@"INSERT INTO dbo.NHU_CAU_DI_LAI
(MaNhuCau, NgayDi, HuongDi, KhungGioMongMuon, CoAccessibility, TrangThai, ThoiDiemTao, MaHanhKhach, MaDeXuat)
VALUES (@id, @date, @dir, @time, @acc, N'Chưa gom nhóm', SYSDATETIME(), @user, NULL);",
                    Db.P("@id", id), Db.P("@date", dpDate.Value.Date), Db.P("@dir", Convert.ToString(cbDirection.SelectedItem)), Db.P("@time", time),
                    Db.P("@acc", chkAccessibility.Checked), Db.P("@user", session.MaNguoiDung));

                MessageBox.Show("Đã khai báo nhu cầu. Mã nhu cầu: " + id, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadNeeds();
            }
            catch (Exception ex)
            {
                Ui.Error(ex, "Không thể khai báo nhu cầu");
            }
        }

        private void CancelNeed(object sender, EventArgs e)
        {
            string id = Ui.Cell(grid, "MaNhuCau");
            if (String.IsNullOrWhiteSpace(id))
            {
                MessageBox.Show("Hãy chọn nhu cầu cần hủy.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show("Bạn có chắc muốn hủy nhu cầu " + id + "?", "Xác nhận hủy", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                int n = Db.Execute(@"UPDATE dbo.NHU_CAU_DI_LAI SET TrangThai=N'Đã hủy'
WHERE MaNhuCau=@id AND MaHanhKhach=@user AND TrangThai=N'Chưa gom nhóm';",
                    Db.P("@id", id), Db.P("@user", session.MaNguoiDung));

                if (n == 0)
                    MessageBox.Show("Chỉ được hủy nhu cầu đang ở trạng thái “Chưa gom nhóm”.", "Không thể hủy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    MessageBox.Show("Đã hủy nhu cầu.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadNeeds();
            }
            catch (Exception ex)
            {
                Ui.Error(ex);
            }
        }
    }
}
