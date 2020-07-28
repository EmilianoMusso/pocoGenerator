using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

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
                _sqlCmd = Constants.QUERY_FOR_TABLES;
            }
            else
            {
                _sqlCmd = Constants.QUERY_FOR_PROCEDURES;
            }
            if (arguments.Any() && !string.IsNullOrEmpty(arguments[0])) _sqlCmd += " WHERE name = QUOTENAME('" + arguments[0] + "')";
            _sqlCmd += " ORDER BY t.name";

            using (var _cmd = new SqlCommand(_sqlCmd, connection))
            {
                _cmd.CommandTimeout = 120000;

                using (var _da = new SqlDataAdapter(_cmd))
                {
                    var _dt = new DataTable();
                    _da.Fill(_dt);

                    var classText = new Classer();
                    if (_areTables)
                    {
                        classText = new Classer(_nameSpace, _dataModelName, "DbContext");
                    }

                    foreach (DataRow table in _dt.Rows)
                    {
                        var _t = table.Field<string>(0);
                        OutpMessage(_t);

                        CreatePocoEntity(connection, _t, _path, _nameSpace, _areTables);

                        if (_areTables)
                        {
                            classText.AddDbSet(_t);
                        }
                    }

                    if (_areTables)
                    {
                        classText.Close();
                        File.WriteAllText(Path.Combine(_path, _dataModelName + ".cs"), classText.ToString());
                    }

                    classText.Dispose();
                }
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
            using (var classText = new Classer(_nameSpace, _t))
            {

                string _sqlCmd;

                if (_isTable)
                {
                    _sqlCmd = Constants.QUERY_FOR_TABLE_FIELDS;
                }
                else
                {
                    classText.Append("\t\tpublic void " + _t + "(");

                    _sqlCmd = Constants.QUERY_FOR_PROCEDURE_FIELDS;
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
                            classText.AddDataAnnotation(Constants.EF_KEY_DA);
                            classText.AddDataAnnotation(Constants.EF_COLUMN_ORDER(r.Field<int>(5) - 1));
                        }

                        if (r.Field<bool>(6))
                        {
                            classText.AddDataAnnotation(Constants.EF_IDENTITY_DA);
                        }

                        if (r.Field<bool>(7))
                        {
                            classText.AddDataAnnotation(Constants.EF_COMPUTED_DA);
                        }

                        classText.AddPublicProperty(_type + (r.Field<bool>(3) ? "?" : ""), _name);
                    }
                    else
                    {
                        classText.AddArgument(_type, _name, _numRows++ < _dt.Rows.Count);
                    }
                }

                if (!_isTable)
                {
                    classText.AppendLine(")")
                                .AppendLine("\t\t{")
                                .AppendLine("\t\t\t// TO DO: Implement function calling")
                                .AppendLine("\t\t}");
                }

                classText.Close();

                if (_isTable) File.WriteAllText(Path.Combine(_path, _t + ".cs"), classText.ToString());
            }
        }
    }
}
