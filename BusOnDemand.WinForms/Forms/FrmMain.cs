using System;
using System.Drawing;
using System.Windows.Forms;
using BusOnDemand.WinForms.Core;

namespace BusOnDemand.WinForms.Forms
{
    public class FrmMain : Form
    {
        private readonly UserSession session;
        public bool LoggedOut { get; private set; }

        public FrmMain(UserSession user)
        {
            session = user;
            Ui.StyleForm(this, "Bus On Demand - Trang chủ", 1280, 820);
            BuildLayout();
        }

        private void BuildLayout()
        {
            // Standard Top Bar with ĐĂNG XUẤT button
            string roleBadgeText = session.RoleLabel + " · " + session.HoTen + " (" + session.TenDangNhap + ")";
            var top = Ui.CreateTopBar(roleBadgeText, PerformLogout);

            var area = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(32, 20, 32, 24),
                BackColor = Ui.Background,
                AutoScroll = true
            };

            // Page Header
            var pageTitle = new Label
            {
                Text = "Trang chủ",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Ui.Navy,
                AutoSize = true,
                Location = new Point(0, 4)
            };

            var pageSubtitle = new Label
            {
                Text = "Chọn chức năng theo nghiệp vụ và vai trò đã đăng nhập.",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Ui.Muted,
                AutoSize = true,
                Location = new Point(2, 40)
            };

            var titlePanel = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Ui.Background };
            titlePanel.Controls.Add(pageTitle);
            titlePanel.Controls.Add(pageSubtitle);

            // Welcome Card
            var welcome = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = Ui.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(24, 20, 24, 20)
            };

            var wTitle = new Label
            {
                Text = "Xin chào, " + session.HoTen,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Ui.Navy,
                AutoSize = true,
                Location = new Point(24, 22)
            };

            string welcomeDesc = session.IsPassenger
                ? "Từ đây bạn có thể gửi nhu cầu di chuyển, theo dõi tiến độ gom nhóm và đặt chỗ trên các chuyến phù hợp."
                : (session.IsCoordinator
                    ? "Từ đây có thể xem đề xuất chuyến, điều chỉnh lịch trình và quản lý cấu hình hệ thống."
                    : "Từ đây có thể xem lịch trình các chuyến được giao và thực hiện vận hành, điểm danh hành khách.");

            var wDesc = new Label
            {
                Text = welcomeDesc,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Ui.Muted,
                AutoSize = true,
                Location = new Point(26, 58)
            };

            string roleTagText = session.IsPassenger ? "VAI TRÒ HÀNH KHÁCH" : (session.IsCoordinator ? "VAI TRÒ ĐIỀU PHỐI" : "VAI TRÒ LÁI XE");
            var roleTag = new Label
            {
                Text = roleTagText,
                AutoSize = true,
                BackColor = Ui.TealBadgeBg,
                ForeColor = Ui.Teal,
                Padding = new Padding(12, 6, 12, 6),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(welcome.Width - 180, 24)
            };

            welcome.Controls.Add(wTitle);
            welcome.Controls.Add(wDesc);
            welcome.Controls.Add(roleTag);
            welcome.Resize += (s, e) => roleTag.Left = Math.Max(300, welcome.ClientSize.Width - roleTag.Width - 28);

            // Tiles Layout
            var tiles = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 20, 0, 10),
                BackColor = Ui.Background,
                AutoScroll = true,
                WrapContents = true
            };

            if (session.IsPassenger)
            {
                AddTile(tiles, "01", "NHU CẦU DI CHUYỂN", "Khai báo, tra cứu và hủy nhu cầu đã gửi", "UC01–UC03", Ui.Teal, () => Open(new FrmNhuCau(session)));
                AddTile(tiles, "02", "ĐẶT GHẾ", "Xem chuyến, kiểm tra số chỗ còn lại và đặt/hủy chỗ", "UC06–UC07", Ui.Navy2, () => Open(new FrmDatGhe(session)));
            }
            else if (session.IsCoordinator)
            {
                AddTile(tiles, "01", "ĐỀ XUẤT CHUYẾN", "Xem nhóm nhu cầu và tạo chuyến", "UC05", Ui.Teal, () => Open(new FrmDeXuatTuyen(session)));
                AddTile(tiles, "02", "LỊCH TRÌNH CHUYẾN", "Dời giờ, đổi xe, hủy hoặc mở thêm", "UC08–UC11 · UC19", Ui.Navy2, () => Open(new FrmLichTrinh(session)));
                AddTile(tiles, "03", "DANH MỤC & CẤU HÌNH", "Quản lý xe, tham số nghiệp vụ", "UC16 · UC17", Color.FromArgb(85, 115, 142), () => Open(new FrmDanhMuc(session)));
                AddTile(tiles, "04", "BÁO CÁO THỐNG KÊ", "Nhu cầu, chuyến, mức lấp đầy, vi phạm", "UC18", Ui.Green, () => Open(new FrmThongKe(session)));
            }
            else if (session.IsDriver)
            {
                AddTile(tiles, "01", "LỊCH TRÌNH", "Xem các chuyến được phân công", "UC19", Ui.Navy2, () => Open(new FrmLichTrinh(session)));
                AddTile(tiles, "02", "VẬN HÀNH CHUYẾN", "Điểm danh, xác nhận khởi hành và hoàn thành", "UC12–UC15", Ui.Teal, () => Open(new FrmVanHanh(session)));
            }

            // Footer note
            var footer = new Label
            {
                Text = "Các mục hiển thị thay đổi theo vai trò: Hành khách · Điều phối viên · Lái xe.",
                Dock = DockStyle.Bottom,
                Height = 30,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Ui.Muted,
                TextAlign = ContentAlignment.MiddleLeft
            };

            area.Controls.Add(tiles);
            area.Controls.Add(footer);
            area.Controls.Add(welcome);
            area.Controls.Add(titlePanel);

            ToolStripStatusLabel lblMainStatus;
            var statusStrip = Ui.CreateStatusStrip(out lblMainStatus, "Hệ thống: Vận hành bình thường | CSDL: BusOnDemandDB (SQL Server)");
            lblMainStatus.Text = "Đăng nhập lúc: " + DateTime.Now.ToString("HH:mm dd/MM/yyyy") + " | Tài khoản: " + session.TenDangNhap;

            Controls.Add(top);
            Controls.Add(statusStrip);
            Controls.Add(area);
            area.BringToFront();
        }

        private void AddTile(FlowLayoutPanel panel, string number, string title, string desc, string ucTag, Color color, Action action)
        {
            var tile = new Panel
            {
                Width = 580,
                Height = 150,
                BackColor = Ui.Surface,
                Margin = new Padding(0, 0, 24, 20),
                Padding = new Padding(22),
                BorderStyle = BorderStyle.FixedSingle
            };

            var badge = new Label
            {
                Text = number,
                BackColor = color,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(22, 22, 54, 54)
            };

            var head = new Label
            {
                Text = title,
                ForeColor = Ui.Navy,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(90, 20)
            };

            var sub = new Label
            {
                Text = desc,
                ForeColor = Ui.Muted,
                Font = new Font("Segoe UI", 9F),
                AutoSize = true,
                Location = new Point(92, 48)
            };

            var btn = Ui.Button("MỞ CHỨC NĂNG", color, 180, 36);
            btn.Location = new Point(92, 84);
            btn.Click += (s, e) => action();

            tile.Controls.Add(badge);
            tile.Controls.Add(head);
            tile.Controls.Add(sub);
            tile.Controls.Add(btn);

            panel.Controls.Add(tile);
        }

        private void PerformLogout()
        {
            if (MessageBox.Show("Bạn có muốn đăng xuất khỏi hệ thống?", "Xác nhận đăng xuất", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                LoggedOut = true;
                Close();
            }
        }

        private void Open(Form form)
        {
            using (form) form.ShowDialog(this);
        }
    }
}
