using System;
using System.Drawing;
using System.Windows.Forms;

namespace BusOnDemand.WinForms.Core
{
    public static class Ui
    {
        // Design Tokens matching Figma Design_Tokens.md & Word Report
        public static readonly Color Navy = Color.FromArgb(23, 52, 91);       // #17345B
        public static readonly Color Navy2 = Color.FromArgb(36, 73, 111);     // #24496F
        public static readonly Color Teal = Color.FromArgb(13, 140, 140);      // #0D8C8C
        public static readonly Color Background = Color.FromArgb(243, 246, 251); // #F3F6FB
        public static readonly Color Surface = Color.FromArgb(255, 255, 255);  // #FFFFFF
        public static readonly Color Text = Color.FromArgb(28, 45, 67);        // #1C2D43
        public static readonly Color Muted = Color.FromArgb(100, 117, 139);    // #64758B
        public static readonly Color Border = Color.FromArgb(217, 227, 239);   // #D9E3EF
        public static readonly Color Red = Color.FromArgb(195, 61, 75);        // #C33D4B (Danger)
        public static readonly Color Green = Color.FromArgb(33, 131, 90);      // #21835A (Success)
        public static readonly Color Warning = Color.FromArgb(168, 106, 18);   // #A86A12
        public static readonly Color TealBadgeBg = Color.FromArgb(231, 247, 245); // #E7F7F5
        public static readonly Color NoteBoxBg = Color.FromArgb(234, 241, 249);   // #EAF1F9
        public static readonly Color NoteBoxBorder = Color.FromArgb(208, 223, 238);
        public static readonly Color HeroBorder = Color.FromArgb(71, 103, 138); // #47678A
        public static readonly Color GridAltRow = Color.FromArgb(247, 249, 252); // #F7F9FC
        public static readonly Color GridSelection = Color.FromArgb(217, 239, 240); // #D9EFF0

        public static void StyleForm(Form form, string title, int width = 1240, int height = 800)
        {
            form.Text = title;
            form.Font = new Font("Segoe UI", 9F);
            form.BackColor = Background;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimumSize = new Size(1024, 680);
            form.Size = new Size(width, height);
            form.AutoScaleMode = AutoScaleMode.Font;
        }

        /// <summary>
        /// Tạo cụm Header thống nhất gồm TopBar thương hiệu (82px) và PageHeader tiêu đề/UC (76px).
        /// Cụm này được gắn vào một Panel cha duy nhất docked Top, ngăn ngừa hoàn toàn lỗi đảo thứ tự Z-order.
        /// </summary>
        public static Panel CreateCombinedHeader(string title, string subtitle, string ucTag, string role, Action onLogoutOrBack = null, string logoutOrBackText = "ĐÓNG")
        {
            var headerContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 158,
                BackColor = Background
            };

            // 1. Top Bar (82px)
            var topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 82,
                BackColor = Navy
            };

            var mark = new Label
            {
                Text = "B",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Teal,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(28, 17, 48, 48)
            };

            var appTitle = new Label
            {
                Text = "BUS ON DEMAND",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(90, 18)
            };

            var appSub = new Label
            {
                Text = "HỆ THỐNG ĐẶT CHỖ XE BUÝT THEO NHU CẦU",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(200, 215, 232),
                AutoSize = true,
                Location = new Point(91, 46)
            };

            topBar.Controls.Add(mark);
            topBar.Controls.Add(appTitle);
            topBar.Controls.Add(appSub);

            // Right side buttons & role pill
            var rightFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Transparent,
                Location = new Point(topBar.Width - 420, 20),
                WrapContents = false
            };

            if (onLogoutOrBack != null)
            {
                var actionBtn = new Button
                {
                    Text = logoutOrBackText,
                    AutoSize = true,
                    Height = 38,
                    BackColor = logoutOrBackText.Contains("ĐĂNG XUẤT") ? Red : Navy2,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(6, 2, 6, 2),
                    Padding = new Padding(12, 0, 12, 0)
                };
                actionBtn.FlatAppearance.BorderSize = 0;
                actionBtn.Click += (sender, args) => onLogoutOrBack();
                rightFlow.Controls.Add(actionBtn);
            }

            if (!String.IsNullOrWhiteSpace(role))
            {
                var roleTag = new Label
                {
                    Text = role,
                    AutoSize = true,
                    BackColor = Navy2,
                    ForeColor = Color.White,
                    Padding = new Padding(14, 10, 14, 10),
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Margin = new Padding(6, 2, 6, 2)
                };
                rightFlow.Controls.Add(roleTag);
            }

            topBar.Controls.Add(rightFlow);
            topBar.Resize += (src, ev) => rightFlow.Left = Math.Max(280, topBar.ClientSize.Width - rightFlow.Width - 28);

            // 2. Page Subheader (76px) docked Bottom of headerContainer
            var subHeader = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 76,
                BackColor = Background,
                Padding = new Padding(28, 12, 28, 6)
            };

            var h = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Navy,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var s = new Label
            {
                Text = subtitle,
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Muted,
                TextAlign = ContentAlignment.MiddleLeft
            };

            subHeader.Controls.Add(s);
            subHeader.Controls.Add(h);

            // Giao diện phần mềm chuẩn doanh nghiệp: Không in mã Use Case lên mặt màn hình
            // Giữ tham số ucTag trong signature để tương thích nhưng không render để đảm bảo độ chuyên nghiệp

            // In headerContainer: topBar is Top (82px), subHeader is Bottom (76px). Total height = 158px.
            headerContainer.Controls.Add(topBar);
            headerContainer.Controls.Add(subHeader);

            return headerContainer;
        }

        public static StatusStrip CreateStatusStrip(out ToolStripStatusLabel lblCount, string dbText = "Trạng thái: Đã kết nối CSDL BusOnDemandDB (SQL Server)")
        {
            var status = new StatusStrip
            {
                BackColor = Color.FromArgb(238, 242, 248),
                Font = new Font("Segoe UI", 9F),
                SizingGrip = true
            };
            var lblDb = new ToolStripStatusLabel
            {
                Text = dbText,
                ForeColor = Navy2,
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblCount = new ToolStripStatusLabel
            {
                Text = "Sẵn sàng",
                ForeColor = Muted,
                TextAlign = ContentAlignment.MiddleRight
            };
            status.Items.Add(lblDb);
            status.Items.Add(lblCount);
            return status;
        }

        public static Panel CreateTopBar(string role = null, Action onLogout = null)
        {
            var p = new Panel { Dock = DockStyle.Top, Height = 82, BackColor = Navy };

            var mark = new Label
            {
                Text = "B",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Teal,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(28, 17, 48, 48)
            };

            var title = new Label
            {
                Text = "BUS ON DEMAND",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(90, 18)
            };

            var sub = new Label
            {
                Text = "HỆ THỐNG ĐẶT CHỖ XE BUÝT THEO NHU CẦU",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(200, 215, 232),
                AutoSize = true,
                Location = new Point(91, 46)
            };

            p.Controls.Add(mark);
            p.Controls.Add(title);
            p.Controls.Add(sub);

            var rightFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Transparent,
                Location = new Point(p.Width - 420, 20),
                WrapContents = false
            };

            if (onLogout != null)
            {
                var btnLogout = new Button
                {
                    Text = "ĐĂNG XUẤT",
                    AutoSize = true,
                    Height = 38,
                    BackColor = Red,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(6, 2, 6, 2),
                    Padding = new Padding(12, 0, 12, 0)
                };
                btnLogout.FlatAppearance.BorderSize = 0;
                btnLogout.Click += (s, e) => onLogout();
                rightFlow.Controls.Add(btnLogout);
            }

            if (!String.IsNullOrWhiteSpace(role))
            {
                var roleTag = new Label
                {
                    Text = role,
                    AutoSize = true,
                    BackColor = Navy2,
                    ForeColor = Color.White,
                    Padding = new Padding(14, 10, 14, 10),
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Margin = new Padding(6, 2, 6, 2)
                };
                rightFlow.Controls.Add(roleTag);
            }

            p.Controls.Add(rightFlow);
            p.Resize += (s, e) => rightFlow.Left = Math.Max(280, p.ClientSize.Width - rightFlow.Width - 28);

            return p;
        }

        public static Panel CreatePageHeader(string title, string subtitle, string ucTag = null)
        {
            var p = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = Background,
                Padding = new Padding(28, 12, 28, 6)
            };

            var h = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Navy,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var s = new Label
            {
                Text = subtitle,
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Muted,
                TextAlign = ContentAlignment.MiddleLeft
            };

            p.Controls.Add(s);
            p.Controls.Add(h);

            if (!String.IsNullOrWhiteSpace(ucTag))
            {
                var tag = new Label
                {
                    Text = ucTag,
                    AutoSize = true,
                    BackColor = TealBadgeBg,
                    ForeColor = Teal,
                    Padding = new Padding(12, 5, 12, 5),
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    Location = new Point(p.Width - 160, 16)
                };
                p.Controls.Add(tag);
                p.Resize += (s0, e0) => tag.Left = Math.Max(200, p.ClientSize.Width - tag.Width - 28);
                tag.BringToFront();
            }

            return p;
        }

        public static Panel Header(string title, string subtitle, string role = null)
        {
            return CreatePageHeader(title, subtitle, role);
        }

        public static Panel Card(string title = null, string subtitle = null, int headerHeight = 44)
        {
            var p = new Panel
            {
                BackColor = Surface,
                Padding = new Padding(20),
                Margin = new Padding(8),
                BorderStyle = BorderStyle.FixedSingle
            };

            if (!String.IsNullOrWhiteSpace(subtitle) && !String.IsNullOrWhiteSpace(title))
            {
                var headerBox = new Panel { Dock = DockStyle.Top, Height = headerHeight, BackColor = Surface };
                var h = new Label
                {
                    Text = title,
                    Dock = DockStyle.Top,
                    Height = 26,
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    ForeColor = Navy,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                var s = new Label
                {
                    Text = subtitle,
                    Dock = DockStyle.Top,
                    Height = 20,
                    Font = new Font("Segoe UI", 8.5F),
                    ForeColor = Muted,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                headerBox.Controls.Add(s);
                headerBox.Controls.Add(h);
                p.Controls.Add(headerBox);
            }
            else if (!String.IsNullOrWhiteSpace(title))
            {
                var h = new Label
                {
                    Text = title,
                    Dock = DockStyle.Top,
                    Height = 32,
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    ForeColor = Navy,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                p.Controls.Add(h);
            }

            return p;
        }

        public static Label Label(string text, bool isFieldLabel = false, bool bold = false)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = isFieldLabel ? Muted : (bold ? Navy : Text),
                Font = new Font("Segoe UI", isFieldLabel ? 8.5F : 9F, (bold || isFieldLabel) ? FontStyle.Bold : FontStyle.Regular),
                Margin = new Padding(3, 4, 12, 4)
            };
        }

        public static Panel FieldBox(string labelText, Control control, int width = 200, int height = 64)
        {
            var p = new Panel { Width = width, Height = height, Margin = new Padding(3, 0, 10, 0), BackColor = Surface };
            var lbl = new Label
            {
                Text = labelText.ToUpperInvariant(),
                AutoSize = true,
                Location = new Point(0, 0),
                ForeColor = Muted,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            control.Location = new Point(0, 20);
            control.Width = width - 4;
            p.Controls.Add(lbl);
            p.Controls.Add(control);
            return p;
        }

        public static TextBox TextBox(string text = "")
        {
            return new TextBox
            {
                Text = text,
                Width = 200,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(3, 2, 12, 5)
            };
        }

        public static ComboBox Combo()
        {
            return new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(3, 2, 12, 5)
            };
        }

        public static Button Button(string text, Color? backColor = null, int width = 150, int height = 38)
        {
            var b = new Button
            {
                Text = text,
                Width = width,
                Height = height,
                BackColor = backColor ?? Teal,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 6, 8, 6)
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        public static DataGridView Grid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoGenerateColumns = true,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 38,
                RowTemplate = { Height = 34 },
                Font = new Font("Segoe UI", 9F),
                GridColor = Border
            };
        }

        public static void StyleGrid(DataGridView grid)
        {
            grid.ColumnHeadersDefaultCellStyle.BackColor = Navy2;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);

            grid.DefaultCellStyle.SelectionBackColor = GridSelection;
            grid.DefaultCellStyle.SelectionForeColor = Text;
            grid.DefaultCellStyle.ForeColor = Text;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);

            grid.AlternatingRowsDefaultCellStyle.BackColor = GridAltRow;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = GridSelection;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Text;
            grid.AlternatingRowsDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        }

        public static string Cell(DataGridView grid, string col)
        {
            if (grid.CurrentRow == null || !grid.Columns.Contains(col)) return null;
            var value = grid.CurrentRow.Cells[col].Value;
            return value == null || value == DBNull.Value ? null : Convert.ToString(value);
        }

        public static void Error(Exception ex, string title = "Không thể thực hiện")
        {
            MessageBox.Show(ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
