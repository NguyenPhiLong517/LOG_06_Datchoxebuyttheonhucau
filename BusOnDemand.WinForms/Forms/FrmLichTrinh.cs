using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using BusOnDemand.WinForms.Core;

namespace BusOnDemand.WinForms.Forms
{
    public class FrmLichTrinh : Form
    {
        private readonly UserSession session;
        private DataGridView grid;
        private DateTimePicker dpFilterDate;
        private CheckBox chkUseDateFilter;
        private ComboBox cbFilterDirection;
        private ComboBox cbFilterStatus;
        private Label lblSelectedTripInfo;

        // Coordinator action controls
        private DateTimePicker dpNewTime;
        private ComboBox cbXe;
        private ComboBox cbLaiXe;
        private ToolStripStatusLabel lblStatusCount;

        public FrmLichTrinh(UserSession user)
        {
            session = user;
            Ui.StyleForm(this, "Quản lý lịch trình chuyến xe - Bus On Demand", 1320, 840);
            BuildLayout();
            Load += (s, e) =>
            {
                LoadCars();
                LoadDrivers();
                LoadTrips();
            };
        }

        private void BuildLayout()
        {
            // 1. Combined Header (82px TopBar + 76px PageHeader = 158px)
            string roleBadgeText = session.RoleLabel + " · " + session.HoTen + " (" + session.TenDangNhap + ")";
            string sub = session.IsCoordinator
                ? "Theo dõi danh sách các chuyến xe và điều chỉnh phân công xe, lái xe, giờ chạy."
                : "Danh sách các chuyến xe được phân công cho tài khoản lái xe.";

            var header = Ui.CreateCombinedHeader(
                "Lịch trình chuyến xe",
                sub,
                null,
                roleBadgeText,
                () => Close(),
                "ĐÓNG"
            );

            // 2. Main Body TableLayoutPanel
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = session.IsCoordinator ? 2 : 1,
                Padding = new Padding(28, 0, 28, 20),
                BackColor = Ui.Background
            };
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            if (session.IsCoordinator)
                body.RowStyles.Add(new RowStyle(SizeType.Absolute, 220F));

            // ==========================================
            // CARD 1: DANH SÁCH CHUYẾN (VÀ BỘ LỌC)
            // ==========================================
            var gridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Ui.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(20, 14, 20, 14),
                Margin = new Padding(0, 0, 0, session.IsCoordinator ? 12 : 0)
            };

            // Header của Card 1
            var gridHeaderPanel = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Ui.Surface };
            var h = new Label
            {
                Text = "Danh sách chuyến",
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = Ui.Navy,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            var s = new Label
            {
                Text = "Lọc theo ngày, hướng đi hoặc trạng thái để chọn chuyến cần xem.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Ui.Muted,
                AutoSize = true,
                Location = new Point(1, 24)
            };
            gridHeaderPanel.Controls.Add(h);
            gridHeaderPanel.Controls.Add(s);

            // Filter Bar (Đúng chuẩn Figma: NGÀY ĐI, HƯỚNG ĐI, TRẠNG THÁI, NÚT LỌC, NÚT TẤT CẢ)
            var filterBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Ui.Surface,
                Padding = new Padding(0, 4, 0, 0),
                WrapContents = false
            };

            // Bộ lọc 1: Ngày đi
            dpFilterDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                Font = new Font("Segoe UI", 9.5F),
                Value = DateTime.Today,
                Width = 140
            };
            chkUseDateFilter = new CheckBox
            {
                Text = "Lọc ngày",
                Checked = false,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Ui.Muted,
                AutoSize = true,
                Location = new Point(144, 23)
            };
            var pnlDateFilter = new Panel { Width = 230, Height = 58, Margin = new Padding(0, 0, 12, 0) };
            var lblNgayDi = new Label
            {
                Text = "NGÀY ĐI",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Ui.Muted,
                Location = new Point(0, 0),
                AutoSize = true
            };
            dpFilterDate.Location = new Point(0, 20);
            pnlDateFilter.Controls.Add(lblNgayDi);
            pnlDateFilter.Controls.Add(dpFilterDate);
            pnlDateFilter.Controls.Add(chkUseDateFilter);
            filterBar.Controls.Add(pnlDateFilter);

            // Bộ lọc 2: Hướng đi
            cbFilterDirection = Ui.Combo();
            cbFilterDirection.Width = 180;
            cbFilterDirection.Items.AddRange(new object[] { "Tất cả hướng", "Chiều đi", "Chiều về" });
            cbFilterDirection.SelectedIndex = 0;
            filterBar.Controls.Add(Ui.FieldBox("HƯỚNG ĐI", cbFilterDirection, 190));

            // Bộ lọc 3: Trạng thái
            cbFilterStatus = Ui.Combo();
            cbFilterStatus.Width = 190;
            cbFilterStatus.Items.AddRange(new object[] { "Tất cả trạng thái", "Dự kiến", "Đã khởi hành", "Đã hoàn thành", "Đã hủy" });
            cbFilterStatus.SelectedIndex = 0;
            filterBar.Controls.Add(Ui.FieldBox("TRẠNG THÁI", cbFilterStatus, 200));

            // Nút LỌC
            var btnFilter = Ui.Button("LỌC", Ui.Navy2, 110, 36);
            btnFilter.Margin = new Padding(6, 18, 6, 0);
            btnFilter.Click += (sender, args) => LoadTrips();
            filterBar.Controls.Add(btnFilter);

            // Nút TẤT CẢ / LÀM MỚI
            var btnReset = Ui.Button("TẤT CẢ", Color.FromArgb(235, 240, 247), 110, 36);
            btnReset.ForeColor = Ui.Navy;
            btnReset.Margin = new Padding(4, 18, 6, 0);
            btnReset.Click += (sender, args) =>
            {
                chkUseDateFilter.Checked = false;
                cbFilterDirection.SelectedIndex = 0;
                cbFilterStatus.SelectedIndex = 0;
                LoadTrips();
            };
            filterBar.Controls.Add(btnReset);

            // DataGridView hiển thị chuyến
            grid = Ui.Grid();
            Ui.StyleGrid(grid);
            grid.CellFormatting += Grid_CellFormatting;
            grid.SelectionChanged += (s0, e0) => UpdateSelectionDetails();

            // Lắp ráp Card 1 (Add theo thứ tự: FilterBar -> GridHeader -> Grid)
            gridCard.Controls.Add(grid);
            gridCard.Controls.Add(filterBar);
            gridCard.Controls.Add(gridHeaderPanel);

            body.Controls.Add(gridCard, 0, 0);

            // ==========================================
            // CARD 2: THAO TÁC VỚI CHUYẾN ĐÃ CHỌN (ĐIỀU PHỐI VIÊN)
            // ==========================================
            if (session.IsCoordinator)
            {
                var actionCard = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Ui.Surface,
                    BorderStyle = BorderStyle.FixedSingle,
                    Padding = new Padding(20, 12, 20, 12),
                    Margin = new Padding(0, 0, 0, 0)
                };

                // Header Card 2
                var actionHeaderPanel = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = Ui.Surface };
                var title = new Label
                {
                    Text = "Thao tác với chuyến đã chọn",
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    ForeColor = Ui.Navy,
                    AutoSize = true,
                    Location = new Point(0, 0)
                };
                var subtitle = new Label
                {
                    Text = "Chỉ cho phép dời giờ / đổi xe / đổi lái xe / hủy khi chuyến đang ở trạng thái “Dự kiến”.",
                    Font = new Font("Segoe UI", 8.5F),
                    ForeColor = Ui.Muted,
                    AutoSize = true,
                    Location = new Point(1, 22)
                };
                actionHeaderPanel.Controls.Add(title);
                actionHeaderPanel.Controls.Add(subtitle);

                // Dải thông tin chuyến đang chọn (Selected Trip Summary Badge)
                lblSelectedTripInfo = new Label
                {
                    Dock = DockStyle.Top,
                    Height = 32,
                    BackColor = Ui.NoteBoxBg,
                    ForeColor = Ui.Navy,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(12, 0, 12, 0),
                    Text = "[Chưa chọn chuyến] Vui lòng chọn một chuyến xe ở bảng trên để thực hiện điều chỉnh."
                };

                // Dòng 1: Cụm Dời giờ, Đổi xe, Đổi lái xe
                var row1 = new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    Height = 60,
                    BackColor = Ui.Surface,
                    Padding = new Padding(0, 6, 0, 0),
                    WrapContents = false
                };

                // Cụm 1: Giờ mới + Dời giờ
                dpNewTime = new DateTimePicker
                {
                    Format = DateTimePickerFormat.Custom,
                    CustomFormat = "dd/MM/yyyy HH:mm",
                    Font = new Font("Segoe UI", 9.5F),
                    Value = DateTime.Today.AddDays(1).AddHours(7),
                    Width = 165
                };
                var btnMove = Ui.Button("DỜI GIỜ", Ui.Navy2, 100, 34);
                btnMove.Click += MoveTime;
                var pnlMove = new Panel { Width = 280, Height = 56, Margin = new Padding(0, 0, 14, 0) };
                pnlMove.Controls.Add(new Label { Text = "GIỜ KHỞI HÀNH MỚI", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Ui.Muted, Location = new Point(0, 0), AutoSize = true });
                dpNewTime.Location = new Point(0, 20);
                btnMove.Location = new Point(172, 18);
                pnlMove.Controls.Add(dpNewTime);
                pnlMove.Controls.Add(btnMove);
                row1.Controls.Add(pnlMove);

                // Cụm 2: Xe mới + Đổi xe
                cbXe = Ui.Combo();
                cbXe.Width = 230;
                var btnChangeCar = Ui.Button("ĐỔI XE", Ui.Navy2, 95, 34);
                btnChangeCar.Click += ChangeCar;
                var pnlCar = new Panel { Width = 340, Height = 56, Margin = new Padding(0, 0, 14, 0) };
                pnlCar.Controls.Add(new Label { Text = "XE MỚI", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Ui.Muted, Location = new Point(0, 0), AutoSize = true });
                cbXe.Location = new Point(0, 20);
                btnChangeCar.Location = new Point(236, 18);
                pnlCar.Controls.Add(cbXe);
                pnlCar.Controls.Add(btnChangeCar);
                row1.Controls.Add(pnlCar);

                // Cụm 3: Lái xe mới + Đổi lái xe
                cbLaiXe = Ui.Combo();
                cbLaiXe.Width = 210;
                var btnChangeDriver = Ui.Button("ĐỔI LÁI XE", Ui.Navy2, 110, 34);
                btnChangeDriver.Click += ChangeDriver;
                var pnlDriver = new Panel { Width = 335, Height = 56, Margin = new Padding(0, 0, 14, 0) };
                pnlDriver.Controls.Add(new Label { Text = "LÁI XE MỚI", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Ui.Muted, Location = new Point(0, 0), AutoSize = true });
                cbLaiXe.Location = new Point(0, 20);
                btnChangeDriver.Location = new Point(216, 18);
                pnlDriver.Controls.Add(cbLaiXe);
                pnlDriver.Controls.Add(btnChangeDriver);
                row1.Controls.Add(pnlDriver);

                // Dòng 2: Nút Hủy chuyến
                var row2 = new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    Height = 44,
                    BackColor = Ui.Surface,
                    Padding = new Padding(0, 4, 0, 0),
                    WrapContents = false
                };

                var btnCancel = Ui.Button("HỦY CHUYẾN", Ui.Red, 140, 34);
                btnCancel.Click += CancelTrip;
                row2.Controls.Add(btnCancel);

                actionCard.Controls.Add(row2);
                actionCard.Controls.Add(row1);
                actionCard.Controls.Add(lblSelectedTripInfo);
                actionCard.Controls.Add(actionHeaderPanel);

                body.Controls.Add(actionCard, 0, 1);
            }

            var statusStrip = new StatusStrip
            {
                BackColor = Color.FromArgb(238, 242, 248),
                Font = new Font("Segoe UI", 9F)
            };
            var lblStatusDb = new ToolStripStatusLabel
            {
                Text = "Trạng thái: Đã kết nối CSDL BusOnDemandDB (SQL Server)",
                ForeColor = Ui.Navy2,
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblStatusCount = new ToolStripStatusLabel
            {
                Text = "Đang tải dữ liệu...",
                ForeColor = Ui.Muted,
                TextAlign = ContentAlignment.MiddleRight
            };
            statusStrip.Items.Add(lblStatusDb);
            statusStrip.Items.Add(lblStatusCount);

            Controls.Add(header);
            Controls.Add(statusStrip);
            Controls.Add(body);
            body.BringToFront();
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string colName = grid.Columns[e.ColumnIndex].Name;

            if (colName == "TrangThai" && e.Value != null)
            {
                string status = e.Value.ToString();
                e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

                switch (status)
                {
                    case "Dự kiến":
                        e.CellStyle.BackColor = Color.FromArgb(234, 241, 249); // #EAF1F9
                        e.CellStyle.ForeColor = Ui.Navy2;
                        break;
                    case "Đã khởi hành":
                        e.CellStyle.BackColor = Color.FromArgb(233, 247, 239); // #E9F7EF
                        e.CellStyle.ForeColor = Ui.Green;
                        break;
                    case "Đã hoàn thành":
                        e.CellStyle.BackColor = Color.FromArgb(238, 242, 247); // #EEF2F7
                        e.CellStyle.ForeColor = Ui.Muted;
                        break;
                    case "Đã hủy":
                    case "Đã hủy do hệ thống":
                        e.CellStyle.BackColor = Color.FromArgb(252, 232, 230); // #FCE8E6
                        e.CellStyle.ForeColor = Ui.Red;
                        break;
                }
            }
        }

        private void UpdateSelectionDetails()
        {
            DataRow row = SelectedTrip();
            if (row == null)
            {
                if (lblSelectedTripInfo != null)
                    lblSelectedTripInfo.Text = "● Vui lòng chọn một chuyến xe ở bảng trên để thực hiện điều chỉnh.";
                return;
            }

            string maChuyen = Convert.ToString(row["MaChuyen"]);
            string gioDuKien = Convert.ToString(row["GioDuKien"]);
            string huongDi = Convert.ToString(row["HuongDi"]);
            string bienSo = Convert.ToString(row["BienSo"]);
            string tenLaiXe = Convert.ToString(row["TenLaiXe"]);
            string trangThai = Convert.ToString(row["TrangThai"]);

            if (lblSelectedTripInfo != null)
            {
                if (trangThai == "Dự kiến")
                {
                    lblSelectedTripInfo.ForeColor = Ui.Navy;
                    lblSelectedTripInfo.Text = string.Format("[Đang chọn] Mã chuyến: {0} | Khởi hành: {1} | {2} | Xe: {3} | Lái xe: {4} | Trạng thái: {5}",
                        maChuyen, gioDuKien, huongDi, bienSo, tenLaiXe, trangThai);
                }
                else
                {
                    lblSelectedTripInfo.ForeColor = Ui.Red;
                    lblSelectedTripInfo.Text = string.Format("[Lưu ý] Mã chuyến: {0} ({1}) - Chuyến không ở trạng thái “Dự kiến”, chỉ xem thông tin.",
                        maChuyen, trangThai);
                }
            }

            // Đồng bộ giờ hiện tại sang DateTimePicker
            if (dpNewTime != null && row["GioDuKien"] != DBNull.Value)
            {
                DateTime t;
                if (DateTime.TryParseExact(gioDuKien, "dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out t)
                    || DateTime.TryParse(gioDuKien, out t))
                {
                    if (t >= dpNewTime.MinDate && t <= dpNewTime.MaxDate)
                        dpNewTime.Value = t;
                }
            }

            // Đồng bộ chọn xe và lái xe nếu có trong combobox
            if (cbXe != null && row["MaXe"] != DBNull.Value)
                cbXe.SelectedValue = Convert.ToString(row["MaXe"]);

            if (cbLaiXe != null && row["MaLaiXe"] != DBNull.Value)
                cbLaiXe.SelectedValue = Convert.ToString(row["MaLaiXe"]);
        }

        private void LoadTrips()
        {
            try
            {
                string sql = @"SELECT c.MaChuyen, CONVERT(NVARCHAR(10), c.GioDuKien, 103) + ' ' + CONVERT(NVARCHAR(5), c.GioDuKien, 108) AS GioDuKien, t.HuongDi, x.BienSo, nd.HoTen AS TenLaiXe,
 c.TrangThai, c.MaXe, c.MaLaiXe, c.MaTuyen, c.MaDeXuat
FROM dbo.CHUYEN c
JOIN dbo.TUYEN t ON t.MaTuyen=c.MaTuyen
JOIN dbo.XE x ON x.MaXe=c.MaXe
JOIN dbo.NGUOI_DUNG nd ON nd.MaNguoiDung=c.MaLaiXe
WHERE 1=1 ";

                var pars = new System.Collections.Generic.List<System.Data.SqlClient.SqlParameter>();

                if (session.IsDriver)
                {
                    sql += " AND c.MaLaiXe = @driverUser ";
                    pars.Add(Db.P("@driverUser", session.MaNguoiDung));
                }

                // Lọc theo ngày
                if (chkUseDateFilter != null && chkUseDateFilter.Checked && dpFilterDate != null)
                {
                    sql += " AND CAST(c.GioDuKien AS DATE) = @filterDate ";
                    pars.Add(Db.P("@filterDate", dpFilterDate.Value.Date));
                }

                // Lọc theo hướng
                if (cbFilterDirection != null && cbFilterDirection.SelectedIndex > 0)
                {
                    sql += " AND t.HuongDi = @filterDirection ";
                    pars.Add(Db.P("@filterDirection", cbFilterDirection.SelectedItem.ToString()));
                }

                // Lọc theo trạng thái
                if (cbFilterStatus != null && cbFilterStatus.SelectedIndex > 0)
                {
                    sql += " AND c.TrangThai = @filterStatus ";
                    pars.Add(Db.P("@filterStatus", cbFilterStatus.SelectedItem.ToString()));
                }

                sql += " ORDER BY c.GioDuKien DESC;";

                grid.DataSource = Db.Query(sql, pars.ToArray());
                Hide("MaXe");
                Hide("MaLaiXe");
                Hide("MaTuyen");
                Hide("MaDeXuat");
                Rename("MaChuyen", "Mã chuyến");
                Rename("GioDuKien", "Giờ dự kiến");
                Rename("HuongDi", "Hướng đi");
                Rename("BienSo", "Biển số xe");
                Rename("TenLaiXe", "Lái xe");
                Rename("TrangThai", "Trạng thái");

                Ui.StyleGrid(grid);

                if (grid.Columns.Contains("MaChuyen"))
                {
                    grid.Columns["MaChuyen"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    grid.Columns["MaChuyen"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    grid.Columns["MaChuyen"].Width = 110;
                }
                if (grid.Columns.Contains("GioDuKien"))
                {
                    grid.Columns["GioDuKien"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    grid.Columns["GioDuKien"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    grid.Columns["GioDuKien"].Width = 150;
                }
                if (grid.Columns.Contains("HuongDi"))
                {
                    grid.Columns["HuongDi"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    grid.Columns["HuongDi"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    grid.Columns["HuongDi"].Width = 110;
                }
                if (grid.Columns.Contains("BienSo"))
                {
                    grid.Columns["BienSo"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    grid.Columns["BienSo"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    grid.Columns["BienSo"].Width = 120;
                }
                if (grid.Columns.Contains("TrangThai"))
                {
                    grid.Columns["TrangThai"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    grid.Columns["TrangThai"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    grid.Columns["TrangThai"].Width = 130;
                }

                var dt = grid.DataSource as DataTable;
                if (lblStatusCount != null && dt != null)
                {
                    lblStatusCount.Text = string.Format("Tổng cộng: {0} chuyến xe  |  Tài khoản: {1}", dt.Rows.Count, session.TenDangNhap);
                }

                UpdateSelectionDetails();
            }
            catch (Exception ex)
            {
                Ui.Error(ex);
            }
        }

        private void LoadCars()
        {
            if (cbXe == null) return;
            try
            {
                var dt = Db.Query(@"SELECT MaXe, MaXe + N' — ' + BienSo + N' — ' + CONVERT(NVARCHAR(10),SucChua) + N' chỗ' + CASE WHEN CoAccessibility=1 THEN N' (tiếp cận)' ELSE N'' END AS TenHienThi
FROM dbo.XE WHERE TrangThai NOT IN (N'Bảo trì',N'Ngưng sử dụng') ORDER BY MaXe;");
                cbXe.DataSource = dt;
                cbXe.DisplayMember = "TenHienThi";
                cbXe.ValueMember = "MaXe";
            }
            catch (Exception ex) { Ui.Error(ex); }
        }

        private void LoadDrivers()
        {
            if (cbLaiXe == null) return;
            try
            {
                var dt = Db.Query("SELECT MaNguoiDung, MaNguoiDung + N' — ' + HoTen AS TenHienThi FROM dbo.NGUOI_DUNG WHERE VaiTro=N'LAI_XE' ORDER BY HoTen;");
                cbLaiXe.DataSource = dt;
                cbLaiXe.DisplayMember = "TenHienThi";
                cbLaiXe.ValueMember = "MaNguoiDung";
            }
            catch (Exception ex) { Ui.Error(ex); }
        }

        private DataRow SelectedTrip()
        {
            if (grid.CurrentRow == null || grid.CurrentRow.DataBoundItem == null) return null;
            var view = grid.CurrentRow.DataBoundItem as DataRowView;
            return view == null ? null : view.Row;
        }

        private DateTime ParseTripTime(object val)
        {
            if (val == null || val == DBNull.Value) return DateTime.MinValue;
            DateTime t;
            string s = val.ToString().Trim();
            if (DateTime.TryParseExact(s, "dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out t))
                return t;
            if (DateTime.TryParse(s, out t))
                return t;
            return DateTime.MinValue;
        }

        private bool RequireUpcomingTrip(out DataRow row)
        {
            row = SelectedTrip();
            if (row == null)
            {
                MessageBox.Show("Hãy chọn một chuyến trên danh sách để thao tác.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            DateTime tripTime = ParseTripTime(row["GioDuKien"]);
            if (Convert.ToString(row["TrangThai"]) != "Dự kiến" || tripTime <= DateTime.Now)
            {
                MessageBox.Show("Chỉ được điều chỉnh chuyến ở trạng thái “Dự kiến” và chưa đến giờ khởi hành.", "Không thể thao tác", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void MoveTime(object sender, EventArgs e)
        {
            DataRow row;
            if (!RequireUpcomingTrip(out row)) return;
            DateTime newTime = dpNewTime.Value;
            DateTime oldTime = ParseTripTime(row["GioDuKien"]);

            if (newTime <= DateTime.Now)
            {
                MessageBox.Show("Giờ mới phải ở tương lai so với thời điểm hiện tại.", "Kiểm tra dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (newTime.Date != oldTime.Date)
            {
                MessageBox.Show("Dời giờ khởi hành không được thay đổi ngày đi của chuyến.", "Kiểm tra dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                object conflict = Db.Scalar(@"SELECT COUNT(*) FROM dbo.CHUYEN WHERE MaChuyen<>@id AND TrangThai<>N'Đã hủy' AND GioDuKien=@time AND (MaXe=@car OR MaLaiXe=@driver);",
                    Db.P("@id", row["MaChuyen"]), Db.P("@time", newTime), Db.P("@car", row["MaXe"]), Db.P("@driver", row["MaLaiXe"]));
                if (Convert.ToInt32(conflict) > 0)
                {
                    MessageBox.Show("Xe hoặc lái xe đã có lịch chuyến khác vào cùng giờ này.", "Trùng lịch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int n = Db.Execute(@"UPDATE dbo.CHUYEN SET GioDuKien=@time WHERE MaChuyen=@id AND TrangThai=N'Dự kiến' AND GioDuKien>SYSDATETIME();",
                    Db.P("@time", newTime), Db.P("@id", row["MaChuyen"]));
                MessageBox.Show(n > 0 ? "Đã dời giờ khởi hành thành công." : "Chuyến không còn đủ điều kiện điều chỉnh.", "Kết quả", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadTrips();
            }
            catch (Exception ex) { Ui.Error(ex); }
        }

        private void ChangeCar(object sender, EventArgs e)
        {
            DataRow row;
            if (!RequireUpcomingTrip(out row)) return;
            if (cbXe == null || cbXe.SelectedValue == null)
            {
                MessageBox.Show("Hãy chọn xe mới.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                Db.Procedure("dbo.sp_PhanCongXe", Db.P("@MaChuyen", Convert.ToString(row["MaChuyen"])), Db.P("@MaXeMoi", Convert.ToString(cbXe.SelectedValue)));
                MessageBox.Show("Đã cập nhật xe phục vụ thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadTrips();
            }
            catch (Exception ex) { Ui.Error(ex); }
        }

        private void ChangeDriver(object sender, EventArgs e)
        {
            DataRow row;
            if (!RequireUpcomingTrip(out row)) return;
            if (cbLaiXe == null || cbLaiXe.SelectedValue == null)
            {
                MessageBox.Show("Hãy chọn lái xe mới.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                string trip = Convert.ToString(row["MaChuyen"]);
                string driver = Convert.ToString(cbLaiXe.SelectedValue);
                DateTime time = ParseTripTime(row["GioDuKien"]);

                object conflict = Db.Scalar("SELECT COUNT(*) FROM dbo.CHUYEN WHERE MaChuyen<>@id AND MaLaiXe=@driver AND GioDuKien=@time AND TrangThai<>N'Đã hủy';",
                    Db.P("@id", trip), Db.P("@driver", driver), Db.P("@time", time));
                if (Convert.ToInt32(conflict) > 0)
                {
                    MessageBox.Show("Lái xe đã có chuyến khác vào cùng thời điểm.", "Trùng lịch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int n = Db.Execute("UPDATE dbo.CHUYEN SET MaLaiXe=@driver WHERE MaChuyen=@id AND TrangThai=N'Dự kiến' AND GioDuKien>SYSDATETIME();",
                    Db.P("@driver", driver), Db.P("@id", trip));
                MessageBox.Show(n > 0 ? "Đã đổi lái xe cho chuyến thành công." : "Chuyến không còn đủ điều kiện điều chỉnh.", "Kết quả", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadTrips();
            }
            catch (Exception ex) { Ui.Error(ex); }
        }

        private void CancelTrip(object sender, EventArgs e)
        {
            DataRow row;
            if (!RequireUpcomingTrip(out row)) return;
            string id = Convert.ToString(row["MaChuyen"]);

            if (MessageBox.Show("Hủy chuyến " + id + "? Toàn bộ đặt chỗ hiện có sẽ được chuyển sang trạng thái “Đã hủy do hệ thống”.", "Xác nhận hủy chuyến", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                using (var cn = Db.Open())
                using (var tx = cn.BeginTransaction())
                {
                    try
                    {
                        Db.Execute(cn, tx, "UPDATE dbo.CHUYEN SET TrangThai=N'Đã hủy' WHERE MaChuyen=@id AND TrangThai=N'Dự kiến' AND GioDuKien>SYSDATETIME();", Db.P("@id", id));
                        Db.Execute(cn, tx, "UPDATE dbo.DAT_CHO SET TrangThai=N'Đã hủy do hệ thống' WHERE MaChuyen=@id AND TrangThai=N'Đã giữ';", Db.P("@id", id));
                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
                MessageBox.Show("Đã hủy chuyến " + id + ".", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadTrips();
            }
            catch (Exception ex) { Ui.Error(ex); }
        }

        private void Hide(string col) { if (grid.Columns.Contains(col)) grid.Columns[col].Visible = false; }
        private void Rename(string a, string b) { if (grid.Columns.Contains(a)) grid.Columns[a].HeaderText = b; }
    }
}
