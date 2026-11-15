namespace BusOnDemand.WinForms.Core
{
    public sealed class UserSession
    {
        public string MaNguoiDung { get; set; }
        public string HoTen { get; set; }
        public string TenDangNhap { get; set; }
        public string VaiTro { get; set; }

        public bool IsPassenger { get { return VaiTro == "HANH_KHACH"; } }
        public bool IsCoordinator { get { return VaiTro == "DIEU_PHOI"; } }
        public bool IsDriver { get { return VaiTro == "LAI_XE"; } }
        public string RoleLabel
        {
            get
            {
                if (IsPassenger) return "HÀNH KHÁCH";
                if (IsCoordinator) return "ĐIỀU PHỐI VIÊN";
                if (IsDriver) return "LÁI XE";
                return VaiTro ?? "";
            }
        }
    }
}
