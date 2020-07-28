using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace pocoGenerator
{
    class Program
    {
        private static IConfigurationRoot configuration;
        static void Main(string[] args)
        {
            try
            {
                configuration = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                                .AddJsonFile("appsettings.json", false)
                                .Build();

                var _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "pocoGenerator");
                Directory.CreateDirectory(_path);

                using (var connection = new SqlConnection(configuration.GetSection("connectionString").Value))
                {
                    connection.Open();

                    var _sqlCmd = @"SELECT name 
                                    FROM (
                                        SELECT name FROM sys.tables
                                        UNION ALL
                                        SELECT name FROM sys.views
                                   ) t
                                   ORDER BY t.name";
                    if (args.Any() && !string.IsNullOrEmpty(args[0])) _sqlCmd += " WHERE name = QUOTENAME('" + args[0] + "')";

                    var _da = new SqlDataAdapter(_sqlCmd, connection);
                    var _dt = new DataTable();
                    _da.Fill(_dt);

                    var _nameSpace = configuration.GetSection("namespace")?.Value;

                    OutpMessage("Creating classes...");
                    foreach (DataRow table in _dt.Rows)
                    {
                        var _t = table.Field<string>(0);
                        OutpMessage(_t);

                        using (var sw = new StreamWriter(Path.Combine(_path, _t + ".cs")))
                        {
                            var classText = new StringBuilder("using System;\r\n")
                                            .AppendLine("using System.Xml;")
                                            .AppendLine("using System.Linq;")
                                            .AppendLine("namespace " + _nameSpace)
                                            .AppendLine("{")
                                            .AppendLine("\tpublic class " + _t)
                                            .AppendLine("\t{");

                            _sqlCmd = $@"SELECT TAB.name, 
                                                TYP.name,
	                                            COL.name,
                                                COL.is_nullable
                                        FROM (
                                              SELECT object_id, name FROM sys.tables 
                                              UNION ALL
                                              SELECT object_id, name FROM sys.views
                                        ) TAB INNER JOIN sys.columns COL ON TAB.object_id = COL.object_id
                                              INNER JOIN sys.types TYP ON TYP.system_type_id = COL.system_type_id
                                        WHERE TAB.name = @tableName";
                            var _cmd = new SqlCommand(_sqlCmd, connection);
                            _cmd.Parameters.Add(new SqlParameter("tableName", _t));

                            using (var dr = _cmd.ExecuteReader())
                            {
                                if (dr.HasRows)
                                {
                                    while (dr.Read())
                                    {
                                        classText.AppendLine("\t\tpublic " 
                                            + GetNETType(dr.GetString(1)) 
                                            + (dr.GetBoolean(3) ? "?" : "" )
                                            + " " 
                                            + dr.GetString(2) 
                                            + " {get; set;}");
                                    }
                                }
                            }

                            classText.AppendLine("\t}")
                                      .AppendLine("}");

                            sw.WriteLine(classText.ToString());
                        }

                    }
                    OutpMessage("Operation completed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Prints on console a timestamped message
        /// </summary>
        /// <param name="message"></param>
        private static void OutpMessage(string message)
        {
            Console.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + message);
        }

        /// <summary>
        /// Convert SQL type in .NET type
        /// </summary>
        /// <param name="sqlType"></param>
        /// <returns></returns>
        private static string GetNETType(string sqlType)
        {
            if (string.IsNullOrEmpty(sqlType)) return "object";

            switch (sqlType.ToLower())
            {
                case "bigint":
                    return "long";

                case "binary":
                case "filestream":
                case "image":
                case "rowversion":
                case "timestamp":
                case "varbinary":
                    return "byte[]";

                case "bit":
                    return "bool";

                case "data":
                case "datetime":
                case "datetime2":
                case "smalldatetime":
                    return "DateTime";

                case "datetimeoffset":
                    return "DateTimeOffset";

                case "decimal":
                case "money":
                case "numeric":
                case "smallmoney":
                    return "decimal";

                case "float":
                    return "double";

                case "int":
                    return "int";

                case "real":
                    return "single";

                case "smallint":
                    return "short";

                case "varchar":
                case "nvarchar":
                case "char":
                case "nchar":
                case "text":
                case "ntext":
                    return "string";

                case "time":
                    return "TimeSpan";

                case "tinyint":
                    return "byte";

                case "uniqueidentifier":
                    return "Guid";

                case "xml":
                    return "Xml";

                default:
                    return "object";
            }
        }
    }
}
