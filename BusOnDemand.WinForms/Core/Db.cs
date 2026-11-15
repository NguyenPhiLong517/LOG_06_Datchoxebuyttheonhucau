using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace BusOnDemand.WinForms.Core
{
    public static class Db
    {
        private static string ConnectionString
        {
            get
            {
                var item = ConfigurationManager.ConnectionStrings["BusOnDemand"];
                if (item == null || String.IsNullOrWhiteSpace(item.ConnectionString))
                    throw new InvalidOperationException("Chưa cấu hình connection string BusOnDemand trong App.config.");
                return item.ConnectionString;
            }
        }

        public static SqlConnection Open()
        {
            var cn = new SqlConnection(ConnectionString);
            cn.Open();
            return cn;
        }

        public static SqlParameter P(string name, object value)
        {
            if (value == null || value == DBNull.Value)
                return new SqlParameter(name, SqlDbType.NVarChar, -1) { Value = DBNull.Value };
            return new SqlParameter(name, value);
        }

        public static DataTable Query(string sql, params SqlParameter[] parameters)
        {
            using (var cn = Open()) return Query(cn, null, sql, parameters);
        }

        public static DataTable Query(SqlConnection cn, SqlTransaction tx, string sql, params SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, cn, tx))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                using (var da = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        public static int Execute(string sql, params SqlParameter[] parameters)
        {
            using (var cn = Open()) return Execute(cn, null, sql, parameters);
        }

        public static int Execute(SqlConnection cn, SqlTransaction tx, string sql, params SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, cn, tx))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        public static object Scalar(string sql, params SqlParameter[] parameters)
        {
            using (var cn = Open()) return Scalar(cn, null, sql, parameters);
        }

        public static object Scalar(SqlConnection cn, SqlTransaction tx, string sql, params SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, cn, tx))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteScalar();
            }
        }

        public static DataTable Procedure(string name, params SqlParameter[] parameters)
        {
            using (var cn = Open())
            using (var cmd = new SqlCommand(name, cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 60;
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                using (var da = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        public static int ReadConfigInt(string key, int defaultValue)
        {
            try
            {
                var value = Scalar("SELECT GiaTri FROM dbo.CAU_HINH_HE_THONG WHERE TenThamSo=@key", P("@key", key));
                int parsed;
                return value != null && value != DBNull.Value && Int32.TryParse(Convert.ToString(value), out parsed) && parsed > 0
                    ? parsed : defaultValue;
            }
            catch { return defaultValue; }
        }
    }
}
