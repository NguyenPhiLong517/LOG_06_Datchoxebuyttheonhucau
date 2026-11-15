using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BusOnDemand.WinForms.Core;
using BusOnDemand.WinForms.Forms;

namespace BusOnDemand.Tests
{
    class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            DebugLayout.Dump();
            int passed = 0;
            int failed = 0;

            void RunTest(string name, Action test)
            {
                try
                {
                    test();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[PASS] " + name);
                    Console.ResetColor();
                    passed++;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[FAIL] " + name + ": " + ex.Message);
                    Console.ResetColor();
                    failed++;
                }
            }

            Console.WriteLine("=================================================");
            Console.WriteLine("   RUNNING BUS ON DEMAND UI DESIGN TDD TESTS     ");
            Console.WriteLine("=================================================");

            // Test 1: Design Tokens Verification
            RunTest("Design Tokens Match Figma Specs", () =>
            {
                AssertEqual(23, Ui.Navy.R, "Navy.R");
                AssertEqual(52, Ui.Navy.G, "Navy.G");
                AssertEqual(91, Ui.Navy.B, "Navy.B");

                AssertEqual(36, Ui.Navy2.R, "Navy2.R");
                AssertEqual(73, Ui.Navy2.G, "Navy2.G");
                AssertEqual(111, Ui.Navy2.B, "Navy2.B");

                AssertEqual(13, Ui.Teal.R, "Teal.R");
                AssertEqual(140, Ui.Teal.G, "Teal.G");
                AssertEqual(140, Ui.Teal.B, "Teal.B");

                AssertEqual(243, Ui.Background.R, "Background.R");
                AssertEqual(246, Ui.Background.G, "Background.G");
                AssertEqual(251, Ui.Background.B, "Background.B");

                AssertEqual(195, Ui.Red.R, "Red.R");
                AssertEqual(61, Ui.Red.G, "Red.G");
                AssertEqual(75, Ui.Red.B, "Red.B");

                AssertEqual(33, Ui.Green.R, "Green.R");
                AssertEqual(131, Ui.Green.G, "Green.G");
                AssertEqual(90, Ui.Green.B, "Green.B");
            });

            // Test 2: FrmDangNhap layout and rules
            RunTest("FrmDangNhap Split Layout and Rules", () =>
            {
                using (var form = new FrmDangNhap())
                {
                    var allTexts = GetAllControlTexts(form);
                    AssertTrue(allTexts.Any(t => t.Contains("BUS ON DEMAND")), "Contains app title");
                    AssertTrue(allTexts.Any(t => t.Contains("ĐĂNG NHẬP")), "Contains ĐĂNG NHẬP button");
                    AssertTrue(allTexts.Any(t => t.Contains("Một nền tảng. Ba vai trò vận hành.")), "Contains 3 roles subtitle");
                    AssertTrue(allTexts.Any(t => t.Contains("Hành khách khai báo nhu cầu")), "Contains passenger bullet");
                    AssertTrue(allTexts.Any(t => t.Contains("Điều phối viên")), "Contains coordinator bullet");
                    AssertTrue(allTexts.Any(t => t.Contains("Lái xe")), "Contains driver bullet");

                    AssertFalse(allTexts.Any(t => t.ToLower().Contains("đăng ký")), "Must not have register feature");
                    AssertFalse(allTexts.Any(t => t.ToLower().Contains("quên mật khẩu")), "Must not have forgot password feature");
                }
            });

            // Test 3: FrmMain Role-Based UI Filtering and Logout button
            RunTest("FrmMain Role-Based Permissions & Logout Feature", () =>
            {
                var sessionPassenger = new UserSession { MaNguoiDung = "nd01", TenDangNhap = "nd01", HoTen = "Nguyễn Văn A", VaiTro = "HANH_KHACH" };
                using (var formPassenger = new FrmMain(sessionPassenger))
                {
                    var texts = GetAllControlTexts(formPassenger);
                    AssertTrue(texts.Any(t => t.Contains("NHU CẦU DI CHUYỂN")), "Passenger has travel demand tile");
                    AssertTrue(texts.Any(t => t.Contains("ĐẶT GHẾ")), "Passenger has seat booking tile");
                    AssertTrue(texts.Any(t => t.Contains("ĐĂNG XUẤT")), "Has ĐĂNG XUẤT button");
                    AssertFalse(texts.Any(t => t.Contains("ĐỀ XUẤT CHUYẾN")), "Passenger must NOT see trip proposals");
                    AssertFalse(texts.Any(t => t.Contains("VẬN HÀNH")), "Passenger must NOT see operation");
                }

                var sessionCoordinator = new UserSession { MaNguoiDung = "nd03", TenDangNhap = "nd03", HoTen = "Trần Thị B", VaiTro = "DIEU_PHOI" };
                using (var formCoordinator = new FrmMain(sessionCoordinator))
                {
                    var texts = GetAllControlTexts(formCoordinator);
                    AssertTrue(texts.Any(t => t.Contains("ĐỀ XUẤT CHUYẾN")), "Coordinator has proposals tile");
                    AssertTrue(texts.Any(t => t.Contains("LỊCH TRÌNH CHUYẾN")), "Coordinator has schedule tile");
                    AssertTrue(texts.Any(t => t.Contains("DANH MỤC & CẤU HÌNH")), "Coordinator has vehicles & config tile");
                    AssertTrue(texts.Any(t => t.Contains("BÁO CÁO THỐNG KÊ")), "Coordinator has statistics tile");
                    AssertTrue(texts.Any(t => t.Contains("ĐĂNG XUẤT")), "Coordinator has ĐĂNG XUẤT button");
                    AssertFalse(texts.Any(t => t.Contains("NHU CẦU DI CHUYỂN")), "Coordinator must NOT have passenger need submission tile");
                }

                var sessionDriver = new UserSession { MaNguoiDung = "nd02", TenDangNhap = "nd02", HoTen = "Lê Văn C", VaiTro = "LAI_XE" };
                using (var formDriver = new FrmMain(sessionDriver))
                {
                    var texts = GetAllControlTexts(formDriver);
                    AssertTrue(texts.Any(t => t.Contains("LỊCH TRÌNH")), "Driver has schedule tile");
                    AssertTrue(texts.Any(t => t.Contains("VẬN HÀNH CHUYẾN")), "Driver has trip operation tile");
                    AssertTrue(texts.Any(t => t.Contains("ĐĂNG XUẤT")), "Driver has ĐĂNG XUẤT button");
                    AssertFalse(texts.Any(t => t.Contains("ĐỀ XUẤT CHUYẾN")), "Driver must NOT see trip proposals");
                    AssertFalse(texts.Any(t => t.Contains("DANH MỤC")), "Driver must NOT have category config tile");
                }
            });

            // Test 4: FrmNhuCau Controls and Layout
            RunTest("FrmNhuCau Controls, Visibility and Layout", () =>
            {
                var session = new UserSession { MaNguoiDung = "nd01", TenDangNhap = "nd01", HoTen = "Nguyễn Văn A", VaiTro = "HANH_KHACH" };
                using (var form = new FrmNhuCau(session))
                {
                    var texts = GetAllControlTexts(form);
                    AssertTrue(texts.Any(t => t.Contains("Khai báo nhu cầu")), "Has declaration title");
                    AssertTrue(texts.Any(t => t.Contains("NGÀY ĐI")), "Has field label NGÀY ĐI");
                    AssertTrue(texts.Any(t => t.Contains("HƯỚNG ĐI")), "Has field label HƯỚNG ĐI");
                    AssertTrue(texts.Any(t => t.Contains("KHUNG GIỜ MONG MUỐN")), "Has field label KHUNG GIỜ MONG MUỐN");
                    AssertTrue(texts.Any(t => t.Contains("Cần hỗ trợ tiếp cận")), "Has accessibility checkbox");
                    AssertTrue(texts.Any(t => t.Contains("GỬI NHU CẦU")), "Has submit button");
                    AssertTrue(texts.Any(t => t.Contains("Nhu cầu của tôi")), "Has my needs grid section");
                    AssertTrue(texts.Any(t => t.Contains("HỦY NHU CẦU")), "Has cancel button");
                    AssertTrue(texts.Any(t => t.Contains("LÀM MỚI DANH SÁCH")), "Has refresh list button");

                    // Check layout hierarchy: body must be TableLayoutPanel so inputCard is not covered
                    var body = form.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
                    AssertTrue(body != null, "Body must be TableLayoutPanel to ensure inputCard is never covered");
                    AssertEqual(2, body.RowCount, "Body has 2 rows (input and grid)");
                }
            });

            // Test 5: FrmDatGhe Controls
            RunTest("FrmDatGhe Controls and Constraints", () =>
            {
                var session = new UserSession { MaNguoiDung = "nd01", TenDangNhap = "nd01", HoTen = "Nguyễn Văn A", VaiTro = "HANH_KHACH" };
                using (var form = new FrmDatGhe(session))
                {
                    var texts = GetAllControlTexts(form);
                    AssertTrue(texts.Any(t => t.Contains("Chuyến có thể đặt")), "Has available trips section");
                    AssertTrue(texts.Any(t => t.Contains("ĐẶT GHẾ")), "Has book button");
                    AssertTrue(texts.Any(t => t.Contains("Đặt ghế của tôi")), "Has my bookings section");
                    AssertTrue(texts.Any(t => t.Contains("HỦY ĐẶT GHẾ")), "Has cancel booking button");
                    AssertFalse(texts.Any(t => t.ToLower().Contains("chọn vị trí ghế")), "Must not have physical seat position picker");
                }
            });

            // Test 6: FrmDeXuatTuyen Controls
            RunTest("FrmDeXuatTuyen Controls and Constraints", () =>
            {
                var session = new UserSession { MaNguoiDung = "nd03", TenDangNhap = "nd03", HoTen = "Trần Thị B", VaiTro = "DIEU_PHOI" };
                using (var form = new FrmDeXuatTuyen(session))
                {
                    var texts = GetAllControlTexts(form);
                    AssertTrue(texts.Any(t => t.Contains("Đề xuất chuyến chờ duyệt")), "Has pending proposals grid");
                    AssertTrue(texts.Any(t => t.Contains("GIỜ KHỞI HÀNH")), "Has GIỜ KHỞI HÀNH field");
                    AssertTrue(texts.Any(t => t.Contains("XE KHẢ DỤNG")), "Has XE KHẢ DỤNG field");
                    AssertTrue(texts.Any(t => t.Contains("LÁI XE KHẢ DỤNG")), "Has LÁI XE KHẢ DỤNG field");
                    AssertTrue(texts.Any(t => t.Contains("TẠO CHUYẾN")), "Has create trip button");
                    AssertTrue(texts.Any(t => t.Contains("TỪ CHỐI ĐỀ XUẤT")), "Has reject proposal button");
                    AssertTrue(texts.Any(t => t.Contains("LÀM MỚI")), "Has refresh button");
                }
            });

            // Test 7: FrmLichTrinh Coordinator vs Driver
            RunTest("FrmLichTrinh Permissions (Coordinator vs Driver)", () =>
            {
                var sessionCoord = new UserSession { MaNguoiDung = "nd03", TenDangNhap = "nd03", HoTen = "Trần Thị B", VaiTro = "DIEU_PHOI" };
                using (var formCoord = new FrmLichTrinh(sessionCoord))
                {
                    var texts = GetAllControlTexts(formCoord);
                    AssertTrue(texts.Any(t => t.Contains("LỌC")), "Filter bar has LỌC button");
                    AssertTrue(texts.Any(t => t.Contains("NGÀY ĐI")), "Filter bar has NGÀY ĐI");
                    AssertTrue(texts.Any(t => t.Contains("HƯỚNG ĐI")), "Filter bar has HƯỚNG ĐI");
                    AssertTrue(texts.Any(t => t.Contains("TRẠNG THÁI")), "Filter bar has TRẠNG THÁI");
                    AssertTrue(texts.Any(t => t.Contains("LÀM MỚI") || t.Contains("TẤT CẢ")), "Filter bar has refresh / reset button");
                    AssertTrue(texts.Any(t => t.Contains("Danh sách chuyến")), "Has trip list header");
                    AssertTrue(texts.Any(t => t.Contains("Thao tác với chuyến đã chọn")), "Has action card for coordinator");
                    AssertTrue(texts.Any(t => t.Contains("DỜI GIỜ")), "Coordinator has DỜI GIỜ");
                    AssertTrue(texts.Any(t => t.Contains("ĐỔI XE")), "Coordinator has ĐỔI XE");
                    AssertTrue(texts.Any(t => t.Contains("ĐỔI LÁI XE")), "Coordinator has ĐỔI LÁI XE");
                    AssertTrue(texts.Any(t => t.Contains("HỦY CHUYẾN")), "Coordinator has HỦY CHUYẾN");
                }

                var sessionDriver = new UserSession { MaNguoiDung = "nd02", TenDangNhap = "nd02", HoTen = "Lê Văn C", VaiTro = "LAI_XE" };
                using (var formDriver = new FrmLichTrinh(sessionDriver))
                {
                    var texts = GetAllControlTexts(formDriver);
                    AssertTrue(texts.Any(t => t.Contains("Danh sách chuyến")), "Driver can see trip list");
                    AssertFalse(texts.Any(t => t.Contains("Thao tác với chuyến đã chọn")), "Driver must NOT see action card");
                    AssertFalse(texts.Any(t => t.Contains("DỜI GIỜ")), "Driver must NOT see DỜI GIỜ");
                    AssertFalse(texts.Any(t => t.Contains("ĐỔI XE")), "Driver must NOT see ĐỔI XE");
                    AssertFalse(texts.Any(t => t.Contains("HỦY CHUYẾN")), "Driver must NOT see HỦY CHUYẾN");
                }
            });

            // Test 8: FrmDanhMuc 2 Tabs (Vehicles & Threshold Configuration)
            RunTest("FrmDanhMuc 2 Tabs and Controls", () =>
            {
                var session = new UserSession { MaNguoiDung = "nd03", TenDangNhap = "nd03", HoTen = "Trần Thị B", VaiTro = "DIEU_PHOI" };
                using (var form = new FrmDanhMuc(session))
                {
                    var texts = GetAllControlTexts(form);
                    AssertTrue(texts.Any(t => t.Contains("DANH MỤC XE")), "Has vehicle tab");
                    AssertTrue(texts.Any(t => t.Contains("CẤU HÌNH NGƯỠNG")), "Has config tab");
                    AssertTrue(texts.Any(t => t.Contains("MÃ XE")), "Has MÃ XE field");
                    AssertTrue(texts.Any(t => t.Contains("BIỂN SỐ")), "Has BIỂN SỐ field");
                    AssertTrue(texts.Any(t => t.Contains("SỨC CHỨA")), "Has SỨC CHỨA field");
                    AssertTrue(texts.Any(t => t.Contains("THÊM XE")), "Has add vehicle button");
                    AssertTrue(texts.Any(t => t.Contains("LƯU SỬA")), "Has edit vehicle button");
                    AssertTrue(texts.Any(t => t.Contains("XÓA XE")), "Has delete vehicle button");
                    AssertTrue(texts.Any(t => t.Contains("LƯU CẤU HÌNH")), "Has save config button");
                }
            });

            // Test 9: FrmThongKe 5 KPI Cards and Breakdown Grid
            RunTest("FrmThongKe 5 KPI Cards and Breakdown Grid", () =>
            {
                var session = new UserSession { MaNguoiDung = "nd03", TenDangNhap = "nd03", HoTen = "Trần Thị B", VaiTro = "DIEU_PHOI" };
                using (var form = new FrmThongKe(session))
                {
                    var texts = GetAllControlTexts(form);
                    AssertTrue(texts.Any(t => t.Contains("TỔNG HỢP")), "Has run summary button");
                    AssertTrue(texts.Any(t => t.Contains("NHU CẦU")), "Has KPI card NHU CẦU");
                    AssertTrue(texts.Any(t => t.Contains("CHUYẾN XE")), "Has KPI card CHUYẾN XE");
                    AssertTrue(texts.Any(t => t.Contains("TỈ LỆ LẤP ĐẦY")), "Has KPI card TỈ LỆ LẤP ĐẦY");
                    AssertTrue(texts.Any(t => t.Contains("VI PHẠM")), "Has KPI card VI PHẠM");
                    AssertTrue(texts.Any(t => t.Contains("CẦN HỖ TRỢ")), "Has KPI card CẦN HỖ TRỢ");
                    AssertTrue(texts.Any(t => t.Contains("Chi tiết theo ngày")), "Has detail section");
                    AssertFalse(texts.Any(t => t.ToLower().Contains("xuất excel") || t.ToLower().Contains("export")), "Must not have unrequested export feature");
                }
            });

            // Test 10: FrmVanHanh Operations & Attendance Controls
            RunTest("FrmVanHanh Attendance & Operations", () =>
            {
                var session = new UserSession { MaNguoiDung = "nd02", TenDangNhap = "nd02", HoTen = "Lê Văn C", VaiTro = "LAI_XE" };
                using (var form = new FrmVanHanh(session))
                {
                    var texts = GetAllControlTexts(form);
                    AssertTrue(texts.Any(t => t.Contains("CHỌN CHUYẾN")), "Has CHỌN CHUYẾN field");
                    AssertTrue(texts.Any(t => t.Contains("LƯU ĐIỂM DANH")), "Has save attendance button");
                    AssertTrue(texts.Any(t => t.Contains("XÁC NHẬN KHỞI HÀNH")), "Has confirm departure button");
                    AssertTrue(texts.Any(t => t.Contains("XÁC NHẬN HOÀN THÀNH")), "Has confirm finish button");
                    AssertFalse(texts.Any(t => t.ToLower().Contains("gps") || t.ToLower().Contains("bản đồ trực tiếp")), "Must not have real-time GPS tracking");
                }
            });

            Console.WriteLine("=================================================");
            Console.WriteLine($"RESULTS: {passed} PASSED, {failed} FAILED");
            Console.WriteLine("=================================================");

            return failed == 0 ? 0 : 1;
        }

        private static System.Collections.Generic.List<string> GetAllControlTexts(Control parent)
        {
            var list = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(parent.Text)) list.Add(parent.Text);

            if (parent is TabControl tabControl)
            {
                foreach (TabPage tab in tabControl.TabPages)
                {
                    if (!string.IsNullOrEmpty(tab.Text)) list.Add(tab.Text);
                    list.AddRange(GetAllControlTexts(tab));
                }
            }

            foreach (Control child in parent.Controls)
            {
                list.AddRange(GetAllControlTexts(child));
            }
            return list;
        }

        private static void AssertEqual<T>(T expected, T actual, string name)
        {
            if (!Equals(expected, actual))
                throw new Exception($"{name} expected '{expected}' but was '{actual}'");
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition) throw new Exception("Assertion failed: " + message);
        }

        private static void AssertFalse(bool condition, string message)
        {
            if (condition) throw new Exception("Assertion failed: " + message);
        }
    }
}
