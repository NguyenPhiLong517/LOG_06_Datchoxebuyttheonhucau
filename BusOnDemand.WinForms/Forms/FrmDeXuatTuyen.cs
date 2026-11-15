using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using BusOnDemand.WinForms.Core;

namespace BusOnDemand.WinForms.Forms
{
    public class FrmDeXuatTuyen : Form
    {
        private readonly UserSession session;
        private DataGridView grid;
        private ComboBox cbXe;
        private ComboBox cbLaiXe;
        private DateTimePicker dpDeparture;
        private Label lblSelected;
        private ToolStripStatusLabel lblStatusCount;

        public FrmDeXuatTuyen(UserSession user)
        {
            session = user;
            Ui.StyleForm(this, "Đề xuất chuyến - Bus On Demand", 1240, 820);
            BuildLayout();
            Load += (s, e) => { LoadProposals(); LoadCars(); LoadDrivers(); };
        }

        private void BuildLayout()
        {
            // Combined Header
            string roleBadgeText = session.RoleLabel + " · " + session.HoTen + " (" + session.TenDangNhap + ")";
            var header = Ui.CreateCombinedHeader(
                "Đề xuất chuyến",
                "Xem nhóm nhu cầu đã gom, chọn xe/lái xe phù hợp và tạo chuyến.",
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
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 225F));

            // Top Card: Proposals Grid
            var topCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Ui.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(20, 14, 20, 12),
                Margin = new Padding(0, 0, 0, 12)
            };

            var topHeaderPanel = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Ui.Surface };
            var title1 = new Label
            {
                Text = "Đề xuất chuyến chờ duyệt",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Ui.Navy,
                AutoSize = true,
                Location = new Point(0, 2)
            };
            var sub1 = new Label
            {
                Text = "Một đề xuất gắn với ngày đi, hướng đi và khung giờ mong muốn.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Ui.Muted,
                AutoSize = true,
                Location = new Point(2, 26)
            };
            topHeaderPanel.Controls.Add(title1);
            topHeaderPanel.Controls.Add(sub1);

            grid = Ui.Grid();
            Ui.StyleGrid(grid);
            grid.SelectionChanged += (s, e) => UpdateSelectedProposal();

            var refreshPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                Padding = new Padding(0, 6, 0, 0),
                BackColor = Ui.Surface
            };
            var refresh = Ui.Button("LÀM MỚI", Ui.Navy2, 130);
            refresh.Click += (s, e) => LoadProposals();
            refreshPanel.Controls.Add(refresh);

            topCard.Controls.Add(grid);
            topCard.Controls.Add(refreshPanel);
            topCard.Controls.Add(topHeaderPanel);

            // Bottom Card: Actions
            var bottomCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Ui.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(20, 14, 20, 12),
                Margin = new Padding(0, 0, 0, 0)
            };

            var bottomHeaderPanel = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Ui.Surface };
            var title2 = new Label
            {
                Text = "Thao tác với đề xuất đã chọn",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Ui.Navy,
                AutoSize = true,
                Location = new Point(0, 2)
            };
            lblSelected = new Label
            {
                Text = "Chọn một đề xuất để xem thông tin và tạo chuyến.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Ui.Navy,
                AutoSize = true,
                Location = new Point(2, 28)
            };
            bottomHeaderPanel.Controls.Add(title2);
            bottomHeaderPanel.Controls.Add(lblSelected);

            var fieldsRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Ui.Surface,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 0)
            };

            // Field: Giờ khởi hành
            dpDeparture = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy HH:mm",
                Font = new Font("Segoe UI", 9.5F),
                Value = DateTime.Today.AddDays(1).AddHours(7).AddMinutes(30)
            };
            fieldsRow.Controls.Add(Ui.FieldBox("GIỜ KHỞI HÀNH", dpDeparture, 220));

            // Field: Xe khả dụng
            cbXe = Ui.Combo();
            fieldsRow.Controls.Add(Ui.FieldBox("XE KHẢ DỤNG", cbXe, 320));

            // Field: Lái xe khả dụng
            cbLaiXe = Ui.Combo();
            fieldsRow.Controls.Add(Ui.FieldBox("LÁI XE KHẢ DỤNG", cbLaiXe, 280));

            var buttonRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Ui.Surface,
                Padding = new Padding(0, 6, 0, 0)
            };
            var makeTrip = Ui.Button("TẠO CHUYẾN", Ui.Teal, 160);
            makeTrip.Click += CreateTrip;
            var reject = Ui.Button("TỪ CHỐI ĐỀ XUẤT", Ui.Red, 180);
            reject.Click += RejectProposal;
            buttonRow.Controls.Add(makeTrip);
            buttonRow.Controls.Add(reject);

            bottomCard.Controls.Add(fieldsRow);
            bottomCard.Controls.Add(buttonRow);
            bottomCard.Controls.Add(bottomHeaderPanel);

            body.Controls.Add(topCard, 0, 0);
            body.Controls.Add(bottomCard, 0, 1);

            var statusStrip = Ui.CreateStatusStrip(out lblStatusCount);

            Controls.Add(header);
            Controls.Add(statusStrip);
            Controls.Add(body);
            body.BringToFront();
        }

        private void LoadProposals()
        {
            try
            {
                grid.DataSource = Db.Query(@"
SELECT dx.MaDeXuat, CONVERT(NVARCHAR(10), dx.NgayDi, 103) AS NgayDi, dx.HuongDi, dx.KhungGio,
       COUNT(n.MaNhuCau) AS SoNhuCau,
       SUM(CASE WHEN n.CoAccessibility=1 THEN 1 ELSE 0 END) AS SoCanHoTro,
       CASE WHEN dx.CanhBaoThieuXe=1 THEN N'Thiếu xe accessibility' ELSE dx.TrangThai END AS TrangThaiHienThi,
       dx.TrangThai
FROM dbo.DE_XUAT_CHUYEN dx
LEFT JOIN dbo.NHU_CAU_DI_LAI n ON n.MaDeXuat=dx.MaDeXuat AND n.TrangThai<>N'Đã hủy'
WHERE dx.TrangThai=N'Chờ duyệt'
GROUP BY dx.MaDeXuat,dx.NgayDi,dx.HuongDi,dx.KhungGio,dx.CanhBaoThieuXe,dx.TrangThai
ORDER BY dx.NgayDi,dx.KhungGio;");

                if (grid.Columns.Contains("TrangThai")) grid.Columns["TrangThai"].Visible = false;
                Rename("MaDeXuat", "Mã đề xuất");
                Rename("NgayDi", "Ngày đi");
                Rename("HuongDi", "Hướng đi");
                Rename("KhungGio", "Khung giờ");
                Rename("SoNhuCau", "Số nhu cầu");
                Rename("SoCanHoTro", "Cần hỗ trợ");
                Rename("TrangThaiHienThi", "Trạng thái");
                Ui.StyleGrid(grid);

                if (grid.Columns.Contains("MaDeXuat")) grid.Columns["MaDeXuat"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (grid.Columns.Contains("NgayDi")) grid.Columns["NgayDi"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (grid.Columns.Contains("HuongDi")) grid.Columns["HuongDi"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (grid.Columns.Contains("KhungGio")) grid.Columns["KhungGio"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (grid.Columns.Contains("SoNhuCau")) grid.Columns["SoNhuCau"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (grid.Columns.Contains("SoCanHoTro")) grid.Columns["SoCanHoTro"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (grid.Columns.Contains("TrangThaiHienThi")) grid.Columns["TrangThaiHienThi"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                if (lblStatusCount != null)
                {
                    int c = grid.DataSource is DataTable dt ? dt.Rows.Count : 0;
                    lblStatusCount.Text = string.Format("Đề xuất chờ duyệt: {0}  |  Điều phối viên: {1}", c, session.TenDangNhap);
                }

                if (grid.Rows.Count > 0) grid.Rows[0].Selected = true;
                UpdateSelectedProposal();
            }
            catch (Exception ex) { Ui.Error(ex); }
        }

        private void LoadCars()
        {
            try
            {
                DataTable dt = Db.Query(@"SELECT MaXe, MaXe + N' — ' + BienSo + N' — ' + CONVERT(NVARCHAR(10),SucChua) + N' chỗ' + CASE WHEN CoAccessibility=1 THEN N' (hỗ trợ tiếp cận)' ELSE N'' END AS TenHienThi
FROM dbo.XE WHERE TrangThai NOT IN (N'Bảo trì',N'Ngưng sử dụng') ORDER BY MaXe;");
                cbXe.DataSource = dt;
                cbXe.DisplayMember = "TenHienThi";
                cbXe.ValueMember = "MaXe";
            }
            catch (Exception ex) { Ui.Error(ex); }
        }

        private void LoadDrivers()
        {
            try
            {
                DataTable dt = Db.Query(@"SELECT MaNguoiDung, MaNguoiDung + N' — ' + HoTen AS TenHienThi FROM dbo.NGUOI_DUNG WHERE VaiTro=N'LAI_XE' ORDER BY HoTen;");
                cbLaiXe.DataSource = dt;
                cbLaiXe.DisplayMember = "TenHienThi";
                cbLaiXe.ValueMember = "MaNguoiDung";
            }
            catch (Exception ex) { Ui.Error(ex); }
        }

        private void UpdateSelectedProposal()
        {
            string id = Ui.Cell(grid, "MaDeXuat");
            string date = Ui.Cell(grid, "NgayDi");
            string direction = Ui.Cell(grid, "HuongDi");
            string slot = Ui.Cell(grid, "KhungGio");

            if (String.IsNullOrWhiteSpace(id))
            {
                lblSelected.Text = "Chọn một đề xuất để xem thông tin và tạo chuyến.";
                return;
            }

            lblSelected.Text = "Đang chọn: " + id + "  ·  Ngày: " + date + "  ·  Hướng: " + direction + "  ·  Khung giờ: " + slot +
                               "  ·  Nhu cầu: " + Ui.Cell(grid, "SoNhuCau") + "  ·  Cần hỗ trợ: " + Ui.Cell(grid, "SoCanHoTro");

            DateTime d;
            TimeSpan tm;
            if (DateTime.TryParseExact(date, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out d) && TimeSpan.TryParse(slot, out tm))
            {
                var proposed = d.Date.Add(tm);
                if (proposed > DateTime.Now) dpDeparture.Value = proposed;
                else dpDeparture.Value = DateTime.Today.AddDays(1).Add(tm);
            }
        }

        private void CreateTrip(object sender, EventArgs e)
        {
            string proposal = Ui.Cell(grid, "MaDeXuat");
            if (String.IsNullOrWhiteSpace(proposal) || cbXe.SelectedValue == null || cbLaiXe.SelectedValue == null)
            {
                MessageBox.Show("Hãy chọn đề xuất, xe và lái xe.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                DataTable data = Db.Query("SELECT NgayDi, HuongDi, KhungGio FROM dbo.DE_XUAT_CHUYEN WHERE MaDeXuat=@id AND TrangThai=N'Chờ duyệt';", Db.P("@id", proposal));
                if (data.Rows.Count == 0)
                {
                    MessageBox.Show("Đề xuất không còn ở trạng thái chờ duyệt. Hãy làm mới danh sách.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadProposals();
                    return;
                }

                DateTime day = Convert.ToDateTime(data.Rows[0]["NgayDi"]).Date;
                string direction = Convert.ToString(data.Rows[0]["HuongDi"]);
                string slot = Convert.ToString(data.Rows[0]["KhungGio"]);
                TimeSpan slotTime;

                if (dpDeparture.Value.Date != day || !TimeSpan.TryParse(slot, out slotTime) || Math.Abs((dpDeparture.Value.TimeOfDay - slotTime).TotalMinutes) > Db.ReadConfigInt("DO_RONG_KHUNG_GIO_PHUT", 30))
                {
                    MessageBox.Show("Giờ khởi hành phải đúng ngày đi và nằm trong khung giờ của đề xuất.", "Giờ không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string route = Convert.ToString(Db.Scalar("SELECT TOP(1) MaTuyen FROM dbo.TUYEN WHERE HuongDi=@dir ORDER BY MaTuyen;", Db.P("@dir", direction)));
                if (String.IsNullOrWhiteSpace(route))
                {
                    MessageBox.Show("Chưa có tuyến tương ứng với hướng đi trong danh mục tuyến.", "Thiếu tuyến", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string car = Convert.ToString(cbXe.SelectedValue);
                string driver = Convert.ToString(cbLaiXe.SelectedValue);

                // BR07: Accessibility check
                int needsAcc = Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM dbo.NHU_CAU_DI_LAI WHERE MaDeXuat=@id AND CoAccessibility=1 AND TrangThai<>N'Đã hủy';", Db.P("@id", proposal)));
                if (needsAcc > 0)
                {
                    bool carAcc = Convert.ToBoolean(Db.Scalar("SELECT CoAccessibility FROM dbo.XE WHERE MaXe=@car;", Db.P("@car", car)));
                    if (!carAcc)
                    {
                        MessageBox.Show("Đề xuất có hành khách cần hỗ trợ tiếp cận (accessibility). Xe được chọn phải hỗ trợ tiếp cận.", "Quy tắc BR07", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                // Conflict check
                object conflict = Db.Scalar(@"SELECT COUNT(*) FROM dbo.CHUYEN WHERE TrangThai<>N'Đã hủy' AND GioDuKien=@time AND (MaXe=@car OR MaLaiXe=@driver);",
                    Db.P("@time", dpDeparture.Value), Db.P("@car", car), Db.P("@driver", driver));
                if (Convert.ToInt32(conflict) > 0)
                {
                    MessageBox.Show("Xe hoặc lái xe đã được phân công một chuyến ở đúng giờ này.", "Trùng lịch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string id = "CX" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant();
                using (var cn = Db.Open())
                using (var tx = cn.BeginTransaction())
                {
                    try
                    {
                        Db.Execute(cn, tx, @"INSERT INTO dbo.CHUYEN(MaChuyen,GioDuKien,TrangThai,MaTuyen,MaXe,MaLaiXe,MaDeXuat)
VALUES(@id,@time,N'Dự kiến',@route,@car,@driver,@proposal);",
                            Db.P("@id", id), Db.P("@time", dpDeparture.Value), Db.P("@route", route), Db.P("@car", car), Db.P("@driver", driver), Db.P("@proposal", proposal));

                        Db.Execute(cn, tx, "UPDATE dbo.DE_XUAT_CHUYEN SET TrangThai=N'Đã duyệt' WHERE MaDeXuat=@id AND TrangThai=N'Chờ duyệt';", Db.P("@id", proposal));
                        Db.Execute(cn, tx, "UPDATE dbo.NHU_CAU_DI_LAI SET TrangThai=N'Đã mở chuyến' WHERE MaDeXuat=@id AND TrangThai=N'Đã gom nhóm';", Db.P("@id", proposal));
                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }

                MessageBox.Show("Đã tạo chuyến " + id + ". Các nhu cầu liên kết với đề xuất sẽ hiển thị trạng thái “Đã mở chuyến” trên màn hình hành khách.", "Tạo chuyến thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadProposals();
            }
            catch (Exception ex)
            {
                Ui.Error(ex, "Không thể tạo chuyến");
            }
        }

        private void RejectProposal(object sender, EventArgs e)
        {
            string id = Ui.Cell(grid, "MaDeXuat");
            if (String.IsNullOrWhiteSpace(id))
            {
                MessageBox.Show("Hãy chọn đề xuất cần từ chối.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show("Từ chối đề xuất " + id + "? Các nhu cầu thuộc nhóm sẽ được chuyển sang trạng thái “Không đủ nhóm”.", "Xác nhận từ chối", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                int changed;
                using (var cn = Db.Open())
                using (var tx = cn.BeginTransaction())
                {
                    try
                    {
                        changed = Db.Execute(cn, tx, "UPDATE dbo.DE_XUAT_CHUYEN SET TrangThai=N'Từ chối' WHERE MaDeXuat=@id AND TrangThai=N'Chờ duyệt';", Db.P("@id", id));
                        if (changed > 0)
                            Db.Execute(cn, tx, "UPDATE dbo.NHU_CAU_DI_LAI SET TrangThai=N'Không đủ nhóm' WHERE MaDeXuat=@id AND TrangThai=N'Đã gom nhóm';", Db.P("@id", id));
                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }

                MessageBox.Show(changed > 0 ? "Đã từ chối đề xuất." : "Đề xuất không còn ở trạng thái chờ duyệt.", "Kết quả", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadProposals();
            }
            catch (Exception ex)
            {
                Ui.Error(ex);
            }
        }

        private void Rename(string source, string target)
        {
            if (grid.Columns.Contains(source)) grid.Columns[source].HeaderText = target;
        }
    }
}
