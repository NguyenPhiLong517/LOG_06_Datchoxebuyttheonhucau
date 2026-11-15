using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using BusOnDemand.WinForms.Core;

namespace BusOnDemand.WinForms.Forms
{
    public class FrmDangNhap : Form
    {
        private TextBox txtUser;
        private TextBox txtPassword;
        private Button btnLogin;
        public UserSession AuthenticatedSession { get; private set; }

        public FrmDangNhap()
        {
            Ui.StyleForm(this, "Đăng nhập - Bus On Demand", 1120, 680);
            MinimumSize = new Size(1000, 640);
            BuildLayout();
        }

        private void BuildLayout()
        {
            // Split Layout: Left Hero Panel + Right Form Panel
            var leftHero = new Panel
            {
                Dock = DockStyle.Left,
                Width = 500,
                BackColor = Ui.Navy,
                Padding = new Padding(48, 54, 48, 40)
            };

            // Brand Mark 'B'
            var mark = new Label
            {
                Text = "B",
                Font = new Font("Segoe UI", 26F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Ui.Teal,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(64, 64),
                Location = new Point(48, 54)
            };

            var title = new Label
            {
                Text = "BUS ON DEMAND",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(48, 140)
            };

            var sub = new Label
            {
                Text = "HỆ THỐNG ĐẶT CHỖ XE BUÝT\nTHEO NHU CẦU QUY MÔ TRƯỜNG",
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(213, 226, 240),
                AutoSize = true,
                Location = new Point(50, 182)
            };

            var divider = new Panel
            {
                Height = 1,
                Width = 400,
                BackColor = Ui.HeroBorder,
                Location = new Point(48, 260)
            };

            var platformTitle = new Label
            {
                Text = "Một nền tảng. Ba vai trò vận hành.",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(48, 290)
            };

            var bullet1 = new Label
            {
                Text = "• Hành khách khai báo nhu cầu và đặt chỗ.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(197, 215, 233),
                AutoSize = true,
                Location = new Point(48, 332)
            };

            var bullet2 = new Label
            {
                Text = "• Điều phối viên tổ chức chuyến xe.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(197, 215, 233),
                AutoSize = true,
                Location = new Point(48, 368)
            };

            var bullet3 = new Label
            {
                Text = "• Lái xe điểm danh và cập nhật trạng thái chuyến.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(197, 215, 233),
                AutoSize = true,
                Location = new Point(48, 404)
            };

            leftHero.Controls.Add(mark);
            leftHero.Controls.Add(title);
            leftHero.Controls.Add(sub);
            leftHero.Controls.Add(divider);
            leftHero.Controls.Add(platformTitle);
            leftHero.Controls.Add(bullet1);
            leftHero.Controls.Add(bullet2);
            leftHero.Controls.Add(bullet3);

            // Right Panel (Canvas with Centered Card)
            var rightCanvas = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Ui.Background
            };

            var card = new Panel
            {
                Size = new Size(480, 520),
                BackColor = Ui.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(36, 32, 36, 28)
            };

            // Center card in rightCanvas
            rightCanvas.Resize += (s, e) =>
            {
                card.Location = new Point(
                    Math.Max(20, (rightCanvas.ClientSize.Width - card.Width) / 2),
                    Math.Max(20, (rightCanvas.ClientSize.Height - card.Height) / 2)
                );
            };

            var heading = new Label
            {
                Text = "Đăng nhập hệ thống",
                ForeColor = Ui.Navy,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(36, 30)
            };

            var intro = new Label
            {
                Text = "Sử dụng tài khoản được cấp cho vai trò của bạn.",
                ForeColor = Ui.Muted,
                Font = new Font("Segoe UI", 9.5F),
                AutoSize = true,
                Location = new Point(38, 68)
            };

            var userLabel = new Label
            {
                Text = "TÊN ĐĂNG NHẬP",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Ui.Muted,
                AutoSize = true,
                Location = new Point(38, 114)
            };

            txtUser = new TextBox
            {
                Font = new Font("Segoe UI", 10.5F),
                Location = new Point(38, 136),
                Width = 404
            };

            var passLabel = new Label
            {
                Text = "MẬT KHẨU",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Ui.Muted,
                AutoSize = true,
                Location = new Point(38, 186)
            };

            txtPassword = new TextBox
            {
                Font = new Font("Segoe UI", 10.5F),
                PasswordChar = '●',
                Location = new Point(38, 208),
                Width = 404
            };

            btnLogin = Ui.Button("ĐĂNG NHẬP", Ui.Teal, 404, 46);
            btnLogin.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnLogin.Location = new Point(38, 266);
            btnLogin.Click += LoginClicked;

            // Note Box
            var notePanel = new Panel
            {
                Location = new Point(38, 336),
                Size = new Size(404, 88),
                BackColor = Ui.NoteBoxBg,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(14)
            };

            var noteTitle = new Label
            {
                Text = "Lưu ý",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Ui.Navy,
                AutoSize = true,
                Location = new Point(12, 10)
            };

            var noteText = new Label
            {
                Text = "Tài khoản được cấp sẵn; hệ thống mở chức năng tương ứng với vai trò sau khi xác thực.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Ui.Muted,
                Size = new Size(378, 48),
                Location = new Point(12, 30)
            };

            notePanel.Controls.Add(noteTitle);
            notePanel.Controls.Add(noteText);

            var desktopTag = new Label
            {
                Text = "Phiên bản v1.0.0 · Đồ án Thiết Kế Phần Mềm Hướng Đối Tượng",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Ui.Muted,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(38, 460),
                Size = new Size(404, 20)
            };

            card.Controls.Add(heading);
            card.Controls.Add(intro);
            card.Controls.Add(userLabel);
            card.Controls.Add(txtUser);
            card.Controls.Add(passLabel);
            card.Controls.Add(txtPassword);
            card.Controls.Add(btnLogin);
            card.Controls.Add(notePanel);
            card.Controls.Add(desktopTag);

            rightCanvas.Controls.Add(card);

            Controls.Add(rightCanvas);
            Controls.Add(leftHero);

            txtUser.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtPassword.Focus(); };
            txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoginClicked(s, EventArgs.Empty); };
        }

        private void LoginClicked(object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(txtUser.Text) || String.IsNullOrEmpty(txtPassword.Text))
            {
                MessageBox.Show("Vui lòng nhập tên đăng nhập và mật khẩu.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                DataTable dt = Db.Query(@"SELECT TOP (1) MaNguoiDung, HoTen, TenDangNhap, MatKhauHash, VaiTro
                                          FROM dbo.NGUOI_DUNG WHERE TenDangNhap=@user",
                    Db.P("@user", txtUser.Text.Trim()));

                if (dt.Rows.Count == 0 || !PasswordHasher.Verify(txtPassword.Text, Convert.ToString(dt.Rows[0]["MatKhauHash"])))
                {
                    MessageBox.Show("Tên đăng nhập hoặc mật khẩu không đúng. Nếu đây là database mẫu, hãy chạy 02_MatKhauTaiKhoanMau.sql trước.", "Đăng nhập thất bại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string role = Convert.ToString(dt.Rows[0]["VaiTro"]);
                if (role != "HANH_KHACH" && role != "DIEU_PHOI" && role != "LAI_XE")
                {
                    MessageBox.Show("Vai trò tài khoản chưa được hỗ trợ.", "Không thể đăng nhập", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                AuthenticatedSession = new UserSession
                {
                    MaNguoiDung = Convert.ToString(dt.Rows[0]["MaNguoiDung"]),
                    HoTen = Convert.ToString(dt.Rows[0]["HoTen"]),
                    TenDangNhap = Convert.ToString(dt.Rows[0]["TenDangNhap"]),
                    VaiTro = role
                };

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                Ui.Error(ex, "Lỗi kết nối cơ sở dữ liệu");
            }
        }
    }
}
