using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using BusOnDemand.WinForms.Core;

namespace BusOnDemand.WinForms.Forms
{
    public class FrmDanhMuc : Form
    {
        private readonly UserSession session;
        private DataGridView vehicles;
        private DataGridView settings;
        private TextBox txtMaXe;
        private TextBox txtBienSo;
        private NumericUpDown numCapacity;
        private CheckBox chkAccess;
        private ComboBox cbStatus;
        private ToolStripStatusLabel lblStatusCount;

        public FrmDanhMuc(UserSession user)
        {
            session = user;
            Ui.StyleForm(this, "Danh mục xe và cấu hình hệ thống - Bus On Demand", 1240, 820);
            BuildLayout();
            Load += (s, e) => { LoadVehicles(); LoadSettings(); };
        }

        private void BuildLayout()
        {
            // Combined Header
            string roleBadgeText = session.RoleLabel + " · " + session.HoTen + " (" + session.TenDangNhap + ")";
            var header = Ui.CreateCombinedHeader(
                "Danh mục xe và cấu hình hệ thống",
                "Quản lý phương tiện, sức chứa và điều kiện hỗ trợ tiếp cận / tham số nghiệp vụ.",
                null,
                roleBadgeText,
                () => Close(),
                "ĐÓNG"
            );

            var body = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 0, 28, 20),
                BackColor = Ui.Background
            };

            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Point(20, 8)
            };

            var vehicleTab = new TabPage("DANH MỤC XE") { BackColor = Ui.Background, Padding = new Padding(0, 10, 0, 0) };
            var configTab = new TabPage("CẤU HÌNH NGƯỠNG") { BackColor = Ui.Background, Padding = new Padding(0, 10, 0, 0) };
            tabs.TabPages.Add(vehicleTab);
            tabs.TabPages.Add(configTab);

            // ================== TAB 1: DANH MỤC XE ==================
            var vehicleLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Ui.Background
            };
            vehicleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 175F));
            vehicleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var vehicleInputCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Ui.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(20, 14, 20, 12),
                Margin = new Padding(0, 0, 0, 12)
            };

            var row1 = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 68,
                WrapContents = false,
                BackColor = Ui.Surface
            };

            txtMaXe = Ui.TextBox();
            txtBienSo = Ui.TextBox();
            numCapacity = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 1000,
                Value = 16,
                Font = new Font("Segoe UI", 9.5F)
            };
            cbStatus = Ui.Combo();
            cbStatus.Items.AddRange(new object[] { "Sẵn sàng", "Bảo trì", "Ngưng sử dụng", "Đang hoạt động" });
            cbStatus.SelectedIndex = 0;

            row1.Controls.Add(Ui.FieldBox("MÃ XE", txtMaXe, 170));
            row1.Controls.Add(Ui.FieldBox("BIỂN SỐ", txtBienSo, 210));
            row1.Controls.Add(Ui.FieldBox("SỨC CHỨA", numCapacity, 150));

            var chkBoxPanel = new Panel { Width = 180, Height = 64, BackColor = Ui.Surface };
            chkAccess = new CheckBox
            {
                Text = "Hỗ trợ tiếp cận",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Ui.Text,
                Location = new Point(0, 24)
            };
            chkBoxPanel.Controls.Add(chkAccess);
            row1.Controls.Add(chkBoxPanel);

            row1.Controls.Add(Ui.FieldBox("TRẠNG THÁI", cbStatus, 210));

            var row2 = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                WrapContents = false,
                BackColor = Ui.Surface,
                Padding = new Padding(0, 6, 0, 0)
            };

            var add = Ui.Button("THÊM XE", Ui.Teal, 130);
            add.Click += AddVehicle;
            var update = Ui.Button("LƯU SỬA", Ui.Navy2, 130);
            update.Click += UpdateVehicle;
            var delete = Ui.Button("XÓA XE", Ui.Red, 130);
            delete.Click += DeleteVehicle;
            var reload = Ui.Button("LÀM MỚI", Ui.Navy2, 130);
            reload.Click += (s, e) => LoadVehicles();

            row2.Controls.Add(add);
            row2.Controls.Add(update);
            row2.Controls.Add(delete);
            row2.Controls.Add(reload);

            vehicleInputCard.Controls.Add(row1);
            vehicleInputCard.Controls.Add(row2);

            var vehicleGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Ui.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(20, 14, 20, 10),
                Margin = new Padding(0, 0, 0, 0)
            };

            vehicles = Ui.Grid();
            Ui.StyleGrid(vehicles);
            vehicles.SelectionChanged += (s, e) => FillVehicleFields();

            var vehicleHint = new Label
            {
                Text = "Quy tắc: sức chứa phải lớn hơn 0; không xóa xe đang được gán cho chuyến còn hoạt động.",
                Dock = DockStyle.Bottom,
                Height = 28,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Ui.Muted,
                TextAlign = ContentAlignment.MiddleLeft
            };

            vehicleGridCard.Controls.Add(vehicles);
            vehicleGridCard.Controls.Add(vehicleHint);

            vehicleLayout.Controls.Add(vehicleInputCard, 0, 0);
            vehicleLayout.Controls.Add(vehicleGridCard, 0, 1);
            vehicleTab.Controls.Add(vehicleLayout);

            // ================== TAB 2: CẤU HÌNH NGƯỠNG ==================
            var configCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Ui.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(20, 14, 20, 14)
            };

            var configHeaderPanel = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Ui.Surface };
            var configHint = new Label
            {
                Text = "Chọn ô “Giá trị” để chỉnh tham số. Tên và mô tả tham số không thay đổi.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Ui.Muted,
                AutoSize = true,
                Location = new Point(0, 4)
            };
            var saveConfig = Ui.Button("LƯU CẤU HÌNH", Ui.Teal, 180, 36);
            saveConfig.Location = new Point(0, 24);
            saveConfig.Click += SaveSettings;
            configHeaderPanel.Controls.Add(configHint);
            configHeaderPanel.Controls.Add(saveConfig);

            settings = Ui.Grid();
            settings.ReadOnly = false;
            Ui.StyleGrid(settings);

            var configFooter = new Label
            {
                Text = "Giá trị số nguyên dương; 15 phút và 3 ngày là giá trị mặc định triển khai.",
                Dock = DockStyle.Bottom,
                Height = 28,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Ui.Muted,
                TextAlign = ContentAlignment.MiddleLeft
            };

            configCard.Controls.Add(settings);
            configCard.Controls.Add(configFooter);
            configCard.Controls.Add(configHeaderPanel);

            configTab.Controls.Add(configCard);

            body.Controls.Add(tabs);

            var statusStrip = Ui.CreateStatusStrip(out lblStatusCount);

            Controls.Add(header);
            Controls.Add(statusStrip);
            Controls.Add(body);
            body.BringToFront();
        }

        private void LoadVehicles()
        {
            try
            {
                vehicles.DataSource = Db.Query(@"SELECT MaXe, BienSo, SucChua,
       CASE WHEN CoAccessibility=1 THEN N'Có' ELSE N'Không' END AS HoTroTiepCan,
       TrangThai
FROM dbo.XE ORDER BY MaXe;");

                Rename(vehicles, "MaXe", "Mã xe");
                Rename(vehicles, "BienSo", "Biển số");
                Rename(vehicles, "SucChua", "Sức chứa");
                Rename(vehicles, "HoTroTiepCan", "Hỗ trợ tiếp cận");
                Rename(vehicles, "TrangThai", "Trạng thái");
                Ui.StyleGrid(vehicles);

                if (vehicles.Columns.Contains("MaXe")) vehicles.Columns["MaXe"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (vehicles.Columns.Contains("BienSo")) vehicles.Columns["BienSo"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (vehicles.Columns.Contains("SucChua")) vehicles.Columns["SucChua"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (vehicles.Columns.Contains("HoTroTiepCan")) vehicles.Columns["HoTroTiepCan"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (vehicles.Columns.Contains("TrangThai")) vehicles.Columns["TrangThai"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                if (lblStatusCount != null)
                {
                    int c = vehicles.DataSource is DataTable dt ? dt.Rows.Count : 0;
                    lblStatusCount.Text = string.Format("Tổng số phương tiện: {0} xe  |  Điều phối viên: {1}", c, session.TenDangNhap);
                }
            }
            catch (Exception ex) { Ui.Error(ex); }
        }

        private void FillVehicleFields()
        {
            if (vehicles == null || vehicles.CurrentRow == null) return;
            try
            {
                var r = vehicles.CurrentRow;
                if (r.Cells["MaXe"].Value == null) return;
                txtMaXe.Text = Convert.ToString(r.Cells["MaXe"].Value);
                txtBienSo.Text = Convert.ToString(r.Cells["BienSo"].Value);
                numCapacity.Value = Math.Max(1, Math.Min(1000, Convert.ToDecimal(r.Cells["SucChua"].Value)));
                string acc = Convert.ToString(r.Cells["HoTroTiepCan"].Value);
                chkAccess.Checked = acc == "Có";
                cbStatus.SelectedItem = Convert.ToString(r.Cells["TrangThai"].Value);
            }
            catch { }
        }

        private void ValidateVehicle()
        {
            if (String.IsNullOrWhiteSpace(txtMaXe.Text) || String.IsNullOrWhiteSpace(txtBienSo.Text))
                throw new InvalidOperationException("Mã xe và biển số không được để trống.");
            if (numCapacity.Value <= 0)
                throw new InvalidOperationException("Sức chứa phải lớn hơn 0.");
            if (cbStatus.SelectedItem == null)
                throw new InvalidOperationException("Hãy chọn trạng thái xe.");
        }

        private void AddVehicle(object sender, EventArgs e)
        {
            try
            {
                ValidateVehicle();
                Db.Execute(@"INSERT INTO dbo.XE(MaXe,BienSo,SucChua,CoAccessibility,TrangThai) VALUES(@id,@plate,@cap,@access,@status);",
                    Db.P("@id", txtMaXe.Text.Trim()), Db.P("@plate", txtBienSo.Text.Trim()), Db.P("@cap", Convert.ToInt32(numCapacity.Value)),
                    Db.P("@access", chkAccess.Checked), Db.P("@status", Convert.ToString(cbStatus.SelectedItem)));
                MessageBox.Show("Đã thêm xe mới.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadVehicles();
            }
            catch (Exception ex) { Ui.Error(ex, "Không thể thêm xe"); }
        }

        private void UpdateVehicle(object sender, EventArgs e)
        {
            try
            {
                ValidateVehicle();
                int n = Db.Execute(@"UPDATE dbo.XE SET BienSo=@plate,SucChua=@cap,CoAccessibility=@access,TrangThai=@status WHERE MaXe=@id;",
                    Db.P("@id", txtMaXe.Text.Trim()), Db.P("@plate", txtBienSo.Text.Trim()), Db.P("@cap", Convert.ToInt32(numCapacity.Value)),
                    Db.P("@access", chkAccess.Checked), Db.P("@status", Convert.ToString(cbStatus.SelectedItem)));
                MessageBox.Show(n > 0 ? "Đã cập nhật thông tin xe." : "Không tìm thấy mã xe để sửa.", "Kết quả", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadVehicles();
            }
            catch (Exception ex) { Ui.Error(ex, "Không thể sửa xe"); }
        }

        private void DeleteVehicle(object sender, EventArgs e)
        {
            string id = txtMaXe.Text.Trim();
            if (String.IsNullOrWhiteSpace(id))
            {
                MessageBox.Show("Hãy chọn xe cần xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                int active = Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM dbo.CHUYEN WHERE MaXe=@id AND TrangThai NOT IN (N'Đã hoàn thành',N'Đã hủy');", Db.P("@id", id)));
                if (active > 0)
                {
                    MessageBox.Show("Không thể xóa xe đang được gán cho chuyến chưa hoàn thành.", "Không thể xóa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (MessageBox.Show("Xóa xe " + id + "? Nếu xe đã được tham chiếu trong lịch sử chuyến, cơ sở dữ liệu sẽ không cho xóa để bảo toàn liên kết.", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                int n = Db.Execute("DELETE FROM dbo.XE WHERE MaXe=@id;", Db.P("@id", id));
                MessageBox.Show(n > 0 ? "Đã xóa xe." : "Không tìm thấy xe.", "Kết quả", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadVehicles();
            }
            catch (Exception ex) { Ui.Error(ex, "Không thể xóa xe"); }
        }

        private void LoadSettings()
        {
            try
            {
                settings.DataSource = Db.Query("SELECT TenThamSo, GiaTri, MoTa FROM dbo.CAU_HINH_HE_THONG ORDER BY TenThamSo;");
                Rename(settings, "TenThamSo", "Tên tham số");
                Rename(settings, "GiaTri", "Giá trị");
                Rename(settings, "MoTa", "Mô tả");
                if (settings.Columns.Contains("TenThamSo")) settings.Columns["TenThamSo"].ReadOnly = true;
                if (settings.Columns.Contains("MoTa")) settings.Columns["MoTa"].ReadOnly = true;
                Ui.StyleGrid(settings);
            }
            catch (Exception ex) { Ui.Error(ex); }
        }

        private void SaveSettings(object sender, EventArgs e)
        {
            try
            {
                settings.EndEdit();
                var dt = settings.DataSource as DataTable;
                if (dt == null) return;

                foreach (DataRow row in dt.Rows)
                {
                    int value;
                    if (!int.TryParse(Convert.ToString(row["GiaTri"]), out value) || value <= 0)
                    {
                        MessageBox.Show("Giá trị tham số '" + row["TenThamSo"] + "' phải là số nguyên dương.", "Giá trị không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                foreach (DataRow row in dt.Rows)
                {
                    Db.Execute("UPDATE dbo.CAU_HINH_HE_THONG SET GiaTri=@val WHERE TenThamSo=@name;",
                        Db.P("@val", Convert.ToString(row["GiaTri"])),
                        Db.P("@name", Convert.ToString(row["TenThamSo"])));
                }

                MessageBox.Show("Đã lưu cấu hình ngưỡng nghiệp vụ.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadSettings();
            }
            catch (Exception ex) { Ui.Error(ex, "Không thể lưu cấu hình"); }
        }

        private static void Rename(DataGridView grid, string source, string target)
        {
            if (grid.Columns.Contains(source)) grid.Columns[source].HeaderText = target;
        }
    }
}
