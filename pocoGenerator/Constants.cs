using System;
using System.Collections.Generic;
using System.Text;

namespace pocoGenerator
{
    public static class Constants
    {
        public const string QUERY_FOR_TABLES = @"SELECT name 
                                                    FROM (
                                                        SELECT name FROM sys.tables
                                                        UNION ALL
                                                        SELECT name FROM sys.views
                                                    ) t";

        public const string QUERY_FOR_TABLE_FIELDS = @"SELECT TAB.name, 
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

        public const string QUERY_FOR_PROCEDURES = @"SELECT name 
                                                    FROM (
                                                        SELECT name FROM sys.procedures
                                                        UNION ALL
                                                        SELECT name FROM sys.objects WHERE type IN ('FN', 'IF', 'TF')
                                                    ) t";

        public const string QUERY_FOR_PROCEDURE_FIELDS = @"SELECT OBJ.name, 
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

        // **********************************************************************************************************************************************

        public const string EF_KEY_DA = "[Key]";
        public const string EF_IDENTITY_DA = "[DatabaseGenerated(DatabaseGeneratedOption.Identity)]";
        public const string EF_COMPUTED_DA = "[DatabaseGenerated(DatabaseGeneratedOption.Computed)]";
        public static string EF_COLUMN_ORDER(int _order)
        {
            return "[Column(Order=" + _order.ToString() + ")]";
        }
    }
}
