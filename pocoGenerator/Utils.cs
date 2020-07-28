using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace pocoGenerator
{
    public static class Utils
    {
        /// <summary>
        /// Prints on console a timestamped message
        /// </summary>
        /// <param name="message"></param>
        public static void OutpMessage(string message)
        {
            Console.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + message);
        }

        /// <summary>
        /// Convert SQL type in .NET type
        /// </summary>
        /// <param name="sqlType"></param>
        /// <returns></returns>
        public static string GetNETType(string sqlType)
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

        /// <summary>
        /// Process tables and views
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="_path"></param>
        /// <param name="arguments"></param>
        public static void SqlObjectsProcess(SqlConnection connection, string _nameSpace, string _dataModelName, string _path, string[] arguments, bool _areTables = true)
        {
            string _sqlCmd;
            if (_areTables)
            {
                _sqlCmd = @"SELECT name 
                            FROM (
                                SELECT name FROM sys.tables
                                UNION ALL
                                SELECT name FROM sys.views
                            ) t";
            }
            else
            {
                _sqlCmd = @"SELECT name 
                            FROM (
                                SELECT name FROM sys.procedures
                                UNION ALL
                                SELECT name FROM sys.objects WHERE type IN ('FN', 'IF', 'TF')
                            ) t";
            }
            if (arguments.Any() && !string.IsNullOrEmpty(arguments[0])) _sqlCmd += " WHERE name = QUOTENAME('" + arguments[0] + "')";
            _sqlCmd += " ORDER BY t.name";

            var _da = new SqlDataAdapter(_sqlCmd, connection);
            var _dt = new DataTable();
            _da.Fill(_dt);

            var classText = new StringBuilder();
            if (_areTables)
            {
                classText = new StringBuilder("using System;\r\n")
                            .AppendLine("using System.Xml;")
                            .AppendLine("using System.Linq;")
                            .AppendLine("using System.Data.Entity;")
                            .AppendLine("using System.ComponentModel.DataAnnotations;")
                            .AppendLine("namespace " + _nameSpace)
                            .AppendLine("{")
                            .Append("\tpublic partial class ")
                            .Append(_dataModelName)
                            .AppendLine(": DbContext")
                            .AppendLine("\t{");
            }

            foreach (DataRow table in _dt.Rows)
            {
                var _t = table.Field<string>(0);
                OutpMessage(_t);

                CreatePocoEntity(connection, _t, _path, _nameSpace, _areTables);

                if (_areTables)
                {
                    classText.Append("\t\tpublic virtual DbSet<")
                         .Append(_t)
                         .Append("> ")
                         .Append(_t)
                         .AppendLine(" { get; set; }");
                }
            }

            if (_areTables)
            {
                classText.AppendLine("\t}")
                         .AppendLine("}");

                File.WriteAllText(Path.Combine(_path, "pocoDataModel.cs"), classText.ToString());
            }
        }

        /// <summary>
        /// Create a .cs file from a SQL entity
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="_t"></param>
        /// <param name="_path"></param>
        /// <param name="_nameSpace"></param>
        private static void CreatePocoEntity(SqlConnection connection, string _t, string _path, string _nameSpace, bool _isTable = true)
        {
            var classText = new StringBuilder("using System;\r\n")
                            .AppendLine("using System.Xml;")
                            .AppendLine("using System.Linq;")
                            .AppendLine("using System.ComponentModel.DataAnnotations;")
                            .AppendLine("namespace " + _nameSpace)
                            .AppendLine("{")
                            .AppendLine("\tpublic partial class " + _t)
                            .AppendLine("\t{");

            string _sqlCmd;

            if (_isTable)
            {
                _sqlCmd = $@"SELECT TAB.name, 
                                    TYP.name,
	                                COL.name,
                                    COL.is_nullable,
                                    CAST(CASE WHEN IDC.column_id IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS is_primary,
                                    ISNULL(IDC.index_column_id, 1) AS index_column_id,
                                    COL.is_identity,
                                    COL.is_computed
                            FROM (
                                    SELECT object_id, name FROM sys.tables 
                                    UNION ALL
                                    SELECT object_id, name FROM sys.views
                            ) TAB INNER JOIN sys.columns COL ON TAB.object_id = COL.object_id
                                  INNER JOIN sys.types TYP ON TYP.system_type_id = COL.system_type_id
	                              LEFT JOIN sys.indexes IDX ON IDX.object_id = TAB.object_id AND IDX.is_primary_key = 1
                                  LEFT JOIN sys.index_columns IDC ON IDC.object_id = COL.object_id AND IDC.column_id = COL.column_id AND IDC.index_id = IDX.index_id
                            WHERE TAB.name = @objName";
            }
            else
            {
                classText.Append("\t\tpublic void " + _t + "(");

                _sqlCmd = $@"SELECT OBJ.name, 
                                    TYP.name,
	                                PAR.name,
                                    PAR.is_nullable,
                                    CAST(0 AS BIT) AS is_primary,
                                    0 AS index_column_id,
                                    CAST(0 AS BIT) AS is_identity,
                                    CAST(0 AS BIT) AS is_computed
                            FROM (
	                            SELECT object_id, name FROM sys.procedures
	                            UNION ALL
	                            SELECT object_id, name FROM sys.objects WHERE type IN ('FN', 'IF', 'TF')
                            )
                            OBJ INNER JOIN sys.parameters PAR ON OBJ.object_id = PAR.object_id
                                INNER JOIN sys.types TYP ON TYP.system_type_id = PAR.system_type_id
                            WHERE OBJ.name = @objName
                            ORDER BY OBJ.name, PAR.parameter_id";
            }

            var _cmd = new SqlCommand(_sqlCmd, connection);
            _cmd.Parameters.Add(new SqlParameter("objName", _t));

            var _da = new SqlDataAdapter(_cmd);
            var _dt = new DataTable();
            _da.Fill(_dt);

            var _numRows = 1;
            foreach (DataRow r in _dt.Rows)
            {
                var _type = GetNETType(r.Field<string>(1));
                var _name = r.Field<string>(2);

                if (_isTable)
                {
                    if (r.Field<bool>(4))
                    {
                        classText.AppendLine("\t\t[Key]")
                                 .AppendLine("\t\t[Column(Order=" + (r.Field<int>(5) - 1).ToString() + ")]");
                    }

                    if (r.Field<bool>(6))
                    {
                        classText.AppendLine("\t\t[DatabaseGenerated(DatabaseGeneratedOption.Identity)]");
                    }

                    if (r.Field<bool>(7))
                    {
                        classText.AppendLine("\t\t[DatabaseGenerated(DatabaseGeneratedOption.Computed)]");
                    }

                    classText.Append("\t\tpublic ")
                             .Append(_type)
                             .Append(r.Field<bool>(3) ? "?" : "")
                             .Append(" ")
                             .Append(_name)
                             .AppendLine(" { get; set; }");
                }
                else
                {
                    classText.Append(_type)
                             .Append(" ")
                             .Append(_name.Replace("@", ""))
                             .Append(_numRows++ < _dt.Rows.Count ? ", " : "");
                }
            }

            if (!_isTable)
            {
                classText.AppendLine(")")
                            .AppendLine("\t\t{")
                            .AppendLine("\t\t\t// TO DO: Implement function calling")
                            .AppendLine("\t\t}");
            }

            classText.AppendLine("\t}")
                     .AppendLine("}");

            File.WriteAllText(Path.Combine(_path, _t + ".cs"), classText.ToString());
        }
    }
}
