using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using BusOnDemand.WinForms.Core;

namespace BusOnDemand.WinForms.Forms
{
    public class FrmVanHanh : Form
    {
        private readonly UserSession session;
        private ComboBox cbTrip;
        private Label lblTripInfo;
        private DataGridView grid;
        private ToolStripStatusLabel lblStatusCount;

        public FrmVanHanh(UserSession user)
        {
            session = user;
            Ui.StyleForm(this, "Vận hành chuyến xe - Bus On Demand", 1240, 820);
            BuildLayout();
            Load += (s, e) => LoadTrips();
        }

        private void BuildLayout()
        {
            // Combined Header
            string roleBadgeText = session.RoleLabel + " · " + session.HoTen + " (" + session.TenDangNhap + ")";
            var header = Ui.CreateCombinedHeader(
                "Vận hành chuyến xe",
                "Điểm danh một lần trước khởi hành; xác nhận khởi hành và hoàn thành chuyến.",
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
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 115F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Card 1: Chuyến được phân công
            var selectCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Ui.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(20, 14, 20, 12),
                Margin = new Padding(0, 0, 0, 12)
            };

            var selectTitle = new Label
            {
                Text = "Chuyến được phân công",
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = Ui.Navy,
                AutoSize = true,
                Location = new Point(20, 10)
            };

            var row = new FlowLayoutPanel
            {
                Location = new Point(20, 36),
                Size = new Size(1150, 60),
                BackColor = Ui.Surface,
                WrapContents = false
            };

            cbTrip = Ui.Combo();
            cbTrip.Width = 380;
            cbTrip.SelectedIndexChanged += (s, e) => LoadBookings();
            row.Controls.Add(Ui.FieldBox("CHỌN CHUYẾN", cbTrip, 400));

            var infoPanel = new Panel { Width = 680, Height = 64, BackColor = Ui.Surface };
            lblTripInfo = new Label
            {
                Text = "Chọn chuyến để xem danh sách đặt ghế.",
                AutoSize = true,
                Location = new Point(10, 22),
                ForeColor = Ui.Navy,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            infoPanel.Controls.Add(lblTripInfo);
            row.Controls.Add(infoPanel);

            selectCard.Controls.Add(selectTitle);
            selectCard.Controls.Add(row);

            // Card 2: Attendee Grid Card
            var attendeeCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Ui.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(20, 14, 20, 12),
                Margin = new Padding(0, 0, 0, 0)
            };

            var attendeeHeader = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Ui.Surface };
            var caption = new Label
            {
                Text = "Danh sách hành khách đặt ghế",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Ui.Navy,
                AutoSize = true,
                Location = new Point(0, 2)
            };
            var attendeeSub = new Label
            {
                Text = "Chọn kết quả điểm danh cho từng hành khách. Có thể sửa trước khi xác nhận khởi hành.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Ui.Muted,
                AutoSize = true,
                Location = new Point(2, 26)
            };
            attendeeHeader.Controls.Add(caption);
            attendeeHeader.Controls.Add(attendeeSub);

            grid = Ui.Grid();
            grid.AutoGenerateColumns = false;
            Ui.StyleGrid(grid);

            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "MaDatCho", Name = "MaDatCho", HeaderText = "Mã đặt chỗ", ReadOnly = true, FillWeight = 18 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "MaHanhKhach", Name = "MaHanhKhach", HeaderText = "Mã hành khách", ReadOnly = true, FillWeight = 20 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "HoTen", Name = "HoTen", HeaderText = "Họ tên", ReadOnly = true, FillWeight = 34 });

            var statusCol = new DataGridViewComboBoxColumn
            {
                DataPropertyName = "KetQuaDiemDanh",
                Name = "KetQuaDiemDanh",
                HeaderText = "Kết quả điểm danh",
                FillWeight = 28,
                FlatStyle = FlatStyle.Flat
            };
            statusCol.Items.AddRange("Chưa điểm danh", "Có mặt", "Vắng mặt");
            grid.Columns.Add(statusCol);

            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DaDiemDanh", Name = "DaDiemDanh", Visible = false });

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Ui.Surface,
                Padding = new Padding(0, 8, 0, 0)
            };

            var attendance = Ui.Button("LƯU ĐIỂM DANH", Ui.Teal, 170);
            attendance.Click += SaveAttendance;

            var depart = Ui.Button("XÁC NHẬN KHỞI HÀNH", Ui.Navy2, 210);
            depart.Click += ConfirmDeparture;

            var finish = Ui.Button("XÁC NHẬN HOÀN THÀNH", Ui.Green, 220);
            finish.Click += ConfirmFinish;

            var bottomNote = new Label
            {
                Text = "* Ghi chú: Hành khách vắng mặt sẽ được hệ thống tự động ghi nhận trạng thái No-show khi xác nhận khởi hành.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Ui.Muted,
                AutoSize = true,
                Margin = new Padding(16, 12, 0, 0)
            };

            actions.Controls.Add(attendance);
            actions.Controls.Add(depart);
            actions.Controls.Add(finish);
            actions.Controls.Add(bottomNote);

            attendeeCard.Controls.Add(grid);
            attendeeCard.Controls.Add(actions);
            attendeeCard.Controls.Add(attendeeHeader);

            body.Controls.Add(selectCard, 0, 0);
            body.Controls.Add(attendeeCard, 0, 1);

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
                DataTable dt = Db.Query(@"SELECT c.MaChuyen, c.MaChuyen + N' — ' + t.HuongDi + N' — ' + CONVERT(NVARCHAR(10),c.GioDuKien,103) + N' ' + CONVERT(NVARCHAR(5),c.GioDuKien,108) + N' — ' + c.TrangThai AS TenHienThi
FROM dbo.CHUYEN c JOIN dbo.TUYEN t ON t.MaTuyen=c.MaTuyen
WHERE c.MaLaiXe=@user AND c.TrangThai IN (N'Dự kiến',N'Đã khởi hành',N'Đã hoàn thành') ORDER BY c.GioDuKien DESC;", Db.P("@user", session.MaNguoiDung));

                cbTrip.DataSource = dt;
                cbTrip.DisplayMember = "TenHienThi";
                cbTrip.ValueMember = "MaChuyen";
                if (dt.Rows.Count == 0)
                {
                    lblTripInfo.Text = "Chưa có chuyến được giao.";
                    grid.DataSource = null;
                    if (lblStatusCount != null) lblStatusCount.Text = "Chưa có chuyến được phân công";
                }
                else
                {
                    if (lblStatusCount != null) lblStatusCount.Text = string.Format("Tổng số chuyến được giao: {0}  |  Tài xế: {1}", dt.Rows.Count, session.HoTen);
                }
            }
            catch (Exception ex) { Ui.Error(ex); }
        }

        private string SelectedTripId()
        {
            return cbTrip.SelectedValue == null ? null : Convert.ToString(cbTrip.SelectedValue);
        }

        private void LoadBookings()
        {
            string trip = SelectedTripId();
            if (String.IsNullOrWhiteSpace(trip) || grid == null) return;
            try
            {
                DataTable dt = Db.Query(@"SELECT d.MaDatCho, d.MaHanhKhach, nd.HoTen,
 CASE WHEN dd.MaDiemDanh IS NULL THEN N'Chưa điểm danh' WHEN dd.CoMat=1 THEN N'Có mặt' ELSE N'Vắng mặt' END AS KetQuaDiemDanh,
 CASE WHEN dd.MaDiemDanh IS NULL THEN 0 ELSE 1 END AS DaDiemDanh
FROM dbo.DAT_CHO d JOIN dbo.NGUOI_DUNG nd ON nd.MaNguoiDung=d.MaHanhKhach
LEFT JOIN dbo.DIEM_DANH dd ON dd.MaDatCho=d.MaDatCho
WHERE d.MaChuyen=@trip AND d.TrangThai NOT IN (N'Đã hủy',N'Đã hủy do hệ thống',N'Đã hủy muộn') ORDER BY nd.HoTen;", Db.P("@trip", trip));

                grid.DataSource = dt;

                if (grid.Columns.Contains("MaDatCho")) grid.Columns["MaDatCho"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (grid.Columns.Contains("MaHanhKhach")) grid.Columns["MaHanhKhach"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (grid.Columns.Contains("KetQuaDiemDanh")) grid.Columns["KetQuaDiemDanh"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                var info = Db.Query("SELECT c.GioDuKien, c.TrangThai, x.MaXe, x.SucChua FROM dbo.CHUYEN c JOIN dbo.XE x ON x.MaXe=c.MaXe WHERE c.MaChuyen=@id;", Db.P("@id", trip));
                if (info.Rows.Count > 0)
                {
                    lblTripInfo.Text = "Xe: " + Convert.ToString(info.Rows[0]["MaXe"]) + "   ·   Sức chứa: " + Convert.ToString(info.Rows[0]["SucChua"]) + "   ·   Trạng thái: " + Convert.ToString(info.Rows[0]["TrangThai"]);
                }

                bool isUpcoming = info.Rows.Count > 0 && Convert.ToString(info.Rows[0]["TrangThai"]) == "Dự kiến";
                grid.ReadOnly = !isUpcoming;
                if (grid.Columns.Contains("KetQuaDiemDanh"))
                    grid.Columns["KetQuaDiemDanh"].ReadOnly = !isUpcoming;

                if (lblStatusCount != null)
                {
                    lblStatusCount.Text = string.Format("Chuyến: {0}  |  Danh sách khách: {1} người  |  Tài xế: {2}", trip, dt.Rows.Count, session.HoTen);
                }
            }
            catch (Exception ex) { Ui.Error(ex); }
        }

        private void SaveAttendance(object sender, EventArgs e)
        {
            string trip = SelectedTripId();
            if (String.IsNullOrWhiteSpace(trip))
            {
                MessageBox.Show("Hãy chọn chuyến được giao.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                grid.EndEdit();
                if (Convert.ToInt32(Db.Scalar("SELECT CASE WHEN TrangThai=N'Dự kiến' THEN 1 ELSE 0 END FROM dbo.CHUYEN WHERE MaChuyen=@id AND MaLaiXe=@user;", Db.P("@id", trip), Db.P("@user", session.MaNguoiDung)) ?? 0) != 1)
                {
                    MessageBox.Show("Chỉ được điểm danh trước khi chuyến khởi hành.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                foreach (DataGridViewRow r in grid.Rows)
                {
                    if (r.IsNewRow) continue;
                    string outcome = Convert.ToString(r.Cells["KetQuaDiemDanh"].Value);
                    if (outcome != "Có mặt" && outcome != "Vắng mặt")
                    {
                        MessageBox.Show("Hãy chọn Có mặt hoặc Vắng mặt cho từng hành khách trước khi lưu.", "Chưa hoàn tất điểm danh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    Db.Procedure("dbo.sp_GhiNhanDiemDanh", Db.P("@MaDatCho", Convert.ToString(r.Cells["MaDatCho"].Value)), Db.P("@CoMat", outcome == "Có mặt"), Db.P("@GhiChu", DBNull.Value));
                }

                MessageBox.Show("Đã lưu kết quả điểm danh. Có thể điều chỉnh lại kết quả trước khi xác nhận khởi hành.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadBookings();
                LoadTrips();
            }
            catch (Exception ex)
            {
                Ui.Error(ex, "Không thể lưu điểm danh");
                LoadBookings();
            }
        }

        private void ConfirmDeparture(object sender, EventArgs e)
        {
            string trip = SelectedTripId();
            if (String.IsNullOrWhiteSpace(trip))
            {
                MessageBox.Show("Hãy chọn chuyến.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show("Xác nhận chuyến đã khởi hành? Hệ thống sẽ ghi nhận vi phạm no-show cho hành khách vắng mặt.", "Xác nhận khởi hành", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                Db.Procedure("dbo.sp_KhoiHanhChuyen", Db.P("@MaChuyen", trip), Db.P("@ThoiDiemKhoiHanh", DateTime.Now));
                MessageBox.Show("Đã xác nhận khởi hành.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadTrips();
                LoadBookings();
            }
            catch (Exception ex) { Ui.Error(ex, "Không thể xác nhận khởi hành"); }
        }

        private void ConfirmFinish(object sender, EventArgs e)
        {
            string trip = SelectedTripId();
            if (String.IsNullOrWhiteSpace(trip))
            {
                MessageBox.Show("Hãy chọn chuyến.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show("Xác nhận chuyến đã hoàn thành?", "Xác nhận hoàn thành", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                Db.Procedure("dbo.sp_HoanThanhChuyen", Db.P("@MaChuyen", trip), Db.P("@ThoiDiemHoanThanh", DateTime.Now));
                MessageBox.Show("Đã xác nhận hoàn thành chuyến.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadTrips();
                LoadBookings();
            }
            catch (Exception ex) { Ui.Error(ex, "Không thể xác nhận hoàn thành"); }
        }
    }
}
