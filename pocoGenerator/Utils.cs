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
        public static void SqlObjectsProcess(SqlConnection connection, string _nameSpace, string _path, string[] arguments, bool _areTables = true)
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

            foreach (DataRow table in _dt.Rows)
            {
                var _t = table.Field<string>(0);
                OutpMessage(_t);

                CreatePocoEntity(connection, _t, _path, _nameSpace, _areTables);
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
            using (var sw = new StreamWriter(Path.Combine(_path, _t + ".cs")))
            {
                var classText = new StringBuilder("using System;\r\n")
                                .AppendLine("using System.Xml;")
                                .AppendLine("using System.Linq;")
                                .AppendLine("namespace " + _nameSpace)
                                .AppendLine("{")
                                .AppendLine("\tpublic class " + _t)
                                .AppendLine("\t{");

                string _sqlCmd;

                if (_isTable)
                {
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
                }
                else
                {
                    classText.Append("\t\tpublic void " + _t + "(");

                    _sqlCmd = $@"SELECT OBJ.name, 
                                       TYP.name,
	                                   PAR.name
                                FROM (
	                                SELECT object_id, name FROM sys.procedures
	                                UNION ALL
	                                SELECT object_id, name FROM sys.objects WHERE type IN ('FN', 'IF', 'TF')
                                )
                                OBJ INNER JOIN sys.parameters PAR ON OBJ.object_id = PAR.object_id
                                    INNER JOIN sys.types TYP ON TYP.system_type_id = PAR.system_type_id
                                WHERE OBJ.name = @tableName
                                ORDER BY OBJ.name, PAR.parameter_id";
                }

                var _cmd = new SqlCommand(_sqlCmd, connection);
                _cmd.Parameters.Add(new SqlParameter("tableName", _t));

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
                        classText.Append("\t\tpublic ")
                                    .Append(_type)
                                    .Append(r.Field<bool>(3) ? "?" : "")
                                    .Append(" ")
                                    .Append(_name)
                                    .AppendLine(" {get; set;}");
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

                sw.WriteLine(classText.ToString());
            }
        }
    }
}
