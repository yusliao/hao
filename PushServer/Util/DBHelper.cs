using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Util
{
using System;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
//using System.Data.OracleClient;
using System.Data.Odbc;
using System.Data.Common;
    using System.ComponentModel;

    /// <summary>
    /// 数据库的类型
    /// </summary>
    public enum DatabaseType
    {
        None,
        SQLServer,
        Oracle,
        ODBC,
    }
    
    public sealed class DbFactory
    {
        private static string connectionString;
        private static DatabaseType dbType = DatabaseType.SQLServer;
        public static string GetConnectString
        {
            get
            {
                if (string.IsNullOrEmpty(connectionString))
                {
                    connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["db1"].ConnectionString;
                    
                }
                return connectionString;
            }
        }
        /// <summary>
        /// 执行查询语句
        /// </summary>
        /// <param name="strSql"></param>
        /// <returns></returns>
        public static DataSet DoQuery(DbConnection conn, DbTransaction trans, string strSql)
        {
            DbDataAdapter dAdapter = DbFactory.CreateAdapter(strSql, conn);
            try
            {
                dAdapter.SelectCommand.Transaction = trans;
                DataSet ds = new DataSet();
                dAdapter.Fill(ds);
                return ds;
            }
            catch (Exception e)
            {
               throw e;
            }
            finally
            {
                dAdapter.Dispose();
            }

        }
        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <param name="conn"></param>
        public static void CloseConnection(DbConnection conn)
        {
            if (conn.State == ConnectionState.Open)
                conn.Close();
            conn.Dispose();
        }
        /// <summary>
        /// 创建连接
        /// </summary>
        public static DbConnection CreateConnection
        {
            get
            {
                DbConnection conn = null;
                try
                {
                    switch (dbType)
                    {
                        case DatabaseType.SQLServer:
                            conn = new SqlConnection(GetConnectString);
                            break;
                        case DatabaseType.Oracle:
                            //conn = new OracleConnection(GetConnectString);
                            break;
                        case DatabaseType.ODBC:
                            conn = new OdbcConnection(GetConnectString);
                            break;
                        default:  // 
                            conn = new OleDbConnection(GetConnectString);
                            break;
                    }
                    if (conn != null)
                    {
                        try
                        {
                            conn.Open();
                        }
                        catch (Exception e)
                        {
                            throw e;
                        }
                        return conn;
                    }
                    else
                        throw new Exception("创建连接失败!");
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="dbtype"></param>
        /// <returns></returns>
        public static DbDataAdapter CreateAdapter(DbCommand cmd, DatabaseType dbtype)
        {
            DbDataAdapter da = null;
            switch (dbtype)
            {
                case DatabaseType.SQLServer:
                    da = new SqlDataAdapter((SqlCommand)cmd);
                    break;
                case DatabaseType.Oracle:
                    //da = new OracleDataAdapter((OracleCommand)cmd);
                    break;
                case DatabaseType.ODBC:
                    da = new OdbcDataAdapter((OdbcCommand)cmd);
                    break;
                default:    //其它的用OLEDB 
                    da = new OleDbDataAdapter((OleDbCommand)cmd);
                    break;
            }
            return da;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public static DbDataAdapter CreateAdapter(DbCommand cmd)
        {
            DbDataAdapter da = null;
            switch (dbType)
            {
                case DatabaseType.SQLServer:
                    da = new SqlDataAdapter((SqlCommand)cmd);
                    break;
                case DatabaseType.Oracle:
                    //da = new OracleDataAdapter((OracleCommand)cmd);
                    break;
                case DatabaseType.ODBC:
                    da = new OdbcDataAdapter((OdbcCommand)cmd);
                    break;
                default:    //
                    da = new OleDbDataAdapter((OleDbCommand)cmd);
                    break;
            }
            return da;
        }
 
        /// <summary>
        /// 数据适配器
        /// </summary>
        /// <param name="selectComandText"></param>
        /// <param name="cnn"></param>
        /// <returns></returns>
        public static DbDataAdapter CreateAdapter(string selectComandText, DbConnection cnn)
        {
            DbDataAdapter da = null;
            switch (dbType)
            {
                case DatabaseType.SQLServer:
                    da = new SqlDataAdapter(selectComandText, (SqlConnection)cnn);
                    break;
                case DatabaseType.Oracle:
                    //da = new OracleDataAdapter(selectComandText, (OracleConnection)cnn);
                    break;
                case DatabaseType.ODBC:
                    da = new OdbcDataAdapter(selectComandText, (OdbcConnection)cnn);
                    break;
                default:    //
                    da = new OleDbDataAdapter(selectComandText, (OleDbConnection)cnn);
                    break;
            }
            return da;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="CommandText"></param>
        /// <param name="dbtype"></param>
        /// <param name="cnn"></param>
        /// <returns></returns>
        public static DbCommand CreateCommand(string commandText, DatabaseType dbtype, DbConnection cnn)
        {
            DbCommand cmd = null;
            switch (dbtype)
            {
                case DatabaseType.SQLServer:
                    cmd = new SqlCommand(commandText, (SqlConnection)cnn);
                    break;
                case DatabaseType.Oracle:
                    //cmd = new OracleCommand(commandText, (OracleConnection)cnn);
                    break;
                case DatabaseType.ODBC:
                    cmd = new OdbcCommand(commandText, (OdbcConnection)cnn);
                    break;
                default:   //
                    cmd = new OleDbCommand(commandText, (OleDbConnection)cnn);
                    break;
            }
            return cmd;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="CommandText"></param>
        /// <param name="cnn"></param>
        /// <returns></returns>
        public static DbCommand CreateCommand(string commandText, DbConnection cnn)
        {
            DbCommand cmd = null;
            switch (dbType)
            {
                case DatabaseType.SQLServer:
                    cmd = new SqlCommand(commandText, (SqlConnection)cnn);
                    break;
                case DatabaseType.Oracle:
                    //cmd = new OracleCommand(commandText, (OracleConnection)cnn);
                    break;
                case DatabaseType.ODBC:
                    cmd = new OdbcCommand(commandText, (OdbcConnection)cnn);
                    break;
                default:   //
                    cmd = new OleDbCommand(commandText, (OleDbConnection)cnn);
                    break;
            }
            return cmd;
        }
        public static DbCommand CreateCommand(string commandText, DbConnection cnn, DbTransaction trans)
        {
            DbCommand cmd = null;
            switch (dbType)
            {
                case DatabaseType.SQLServer:
                    cmd = new SqlCommand(commandText, (SqlConnection)cnn, (SqlTransaction)trans);
                    break;
                case DatabaseType.Oracle:
                    //cmd = new OracleCommand(commandText, (OracleConnection)cnn, (OracleTransaction)trans);
                    break;
                case DatabaseType.ODBC:
                    cmd = new OdbcCommand(commandText, (OdbcConnection)cnn, (OdbcTransaction)trans);
                    break;
                default:   //
                    cmd = new OleDbCommand(commandText, (OleDbConnection)cnn, (OleDbTransaction)trans);
                    break;
            }
            return cmd;
        }

        public static DbCommandBuilder CreateCommandBuilder()
        {
            DbCommandBuilder cmb = null;
            switch (dbType)
            {
                case DatabaseType.SQLServer:
                    cmb = new SqlCommandBuilder();
                    break;
                case DatabaseType.Oracle:
                    //cmb = new OracleCommandBuilder();
                    break;
                case DatabaseType.ODBC:
                    cmb = new OdbcCommandBuilder();
                    break;
                default:   //
                    cmb = new OleDbCommandBuilder();
                    break;
            }
            return cmb;
        }

        public static DbCommandBuilder CreateCommandBuilder(DbDataAdapter dat)
        {
            DbCommandBuilder cmb = null;
            switch (dbType)
            {
                case DatabaseType.SQLServer:
                    cmb = new SqlCommandBuilder((SqlDataAdapter)dat);
                    break;
                case DatabaseType.Oracle:
                    //cmb = new OracleCommandBuilder((OracleDataAdapter)dat);
                    break;
                case DatabaseType.ODBC:
                    cmb = new OdbcCommandBuilder((OdbcDataAdapter)dat);
                    break;
                default:   //
                    cmb = new OleDbCommandBuilder((OleDbDataAdapter)dat);
                    break;
            }
            return cmb;
        }
        /// <summary>
        /// 执行查询语句
        /// </summary>
        /// <param name="strSql"></param>
        /// <returns></returns>
        public static DataSet DoQuery(string strSql)
        {
            DbConnection conn = DbFactory.CreateConnection;
            DbDataAdapter dAdapter = DbFactory.CreateAdapter(strSql, conn);
            try
            {
                DataSet ds = new DataSet();
                dAdapter.SelectCommand.CommandTimeout = 0;
                dAdapter.Fill(ds);
                return ds;
            }
            catch (Exception e)
            {
               throw e;
            }
            finally
            {
                dAdapter.Dispose();
                DbFactory.CloseConnection(conn);
            }
        }
        public static DataSet GetDataSchema(string strSql)
        {
            DbConnection conn = DbFactory.CreateConnection;
            try
            {
                using (DbDataAdapter dAdapter = DbFactory.CreateAdapter(strSql, conn))
                {
                    DataSet ds = new DataSet();
                    dAdapter.FillSchema(ds, SchemaType.Mapped);
                    return ds;
                }
            }
            catch (Exception e)
            {
               throw e;
            }
            finally
            {
                DbFactory.CloseConnection(conn);
            }
        }
        /// <summary>
        /// 执行查询语句
        /// </summary>
        /// <param name="strSql">查询语句</param>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        public static DataSet DoQuery(string strSql, string tableName)
        {
            DbConnection conn = DbFactory.CreateConnection;
            try
            {
                using (DbDataAdapter dAdapter = DbFactory.CreateAdapter(strSql, conn))
                {
                    DataSet ds = new DataSet();
                    dAdapter.SelectCommand.CommandTimeout = 0;
                    //dAdapter.Fill(ds);
                    dAdapter.FillSchema(ds, SchemaType.Mapped, tableName);
                    dAdapter.Fill(ds, tableName);
                    ds.Tables[0].TableName = tableName;
                    //Utils.Tools.RemoveDataTablePrimaryAndAllowDBNull(ds);
                    return ds;
                }

            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                DbFactory.CloseConnection(conn);
            }
        }
        /// <summary>
        /// 执行查询语句
        /// </summary>
        /// <param name="strSql"></param>
        /// <returns></returns>
        public static DataSet DoQuery(DbConnection conn, string strSql)
        {
            DbDataAdapter dAdapter = DbFactory.CreateAdapter(strSql, conn);
            try
            {
                dAdapter.SelectCommand.CommandTimeout = 0;
                DataSet ds = new DataSet();
                dAdapter.Fill(ds);
                return ds;
            }
            catch (Exception e)
            {
               throw e;
            }
            finally
            {
                dAdapter.Dispose();
            }

        }

        /// <summary>
        /// 执行返回单一变量的sql
        /// </summary>
        /// <param name="strSql"></param>
        /// <returns></returns>
        public static object DoQueryResultSingleValue(string strSql)
        {
            DbConnection conn = DbFactory.CreateConnection;
            using (DbCommand dc = DbFactory.CreateCommand(strSql, conn))
            {
                dc.CommandTimeout = 0;
                DbDataReader dr = dc.ExecuteReader();
                object obj = null;
                while (dr.Read())
                {
                    obj = dr.GetValue(0);
                    break;
                }
                dr.Close();
                DbFactory.CloseConnection(conn);
                return obj;
            }
        }
        /// <summary>
        /// 执行返回单一变量的sql
        /// </summary>
        /// <param name="strSql"></param>
        /// <returns></returns>
        public static object DoQueryResultSingleValue(DbConnection conn, string strSql)
        {
            using (DbCommand dc = DbFactory.CreateCommand(strSql, conn))
            {
                dc.CommandTimeout = 0;
                object obj = null;
                using (DbDataReader dr = dc.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        obj = dr.GetValue(0);
                        break;
                    }
                    dr.Close();
                }
                return obj;
            }
        }
        /// <summary>
        /// 执行返回单一变量的sql
        /// </summary>
        /// <param name="strSql"></param>
        /// <returns></returns>
        public static object DoQueryResultSingleValue(DbConnection conn, DbTransaction tran, string strSql)
        {
            using (DbCommand dc = DbFactory.CreateCommand(strSql, conn, tran))
            {
                object obj = null;
                dc.CommandTimeout = 0;
                using (DbDataReader dr = dc.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        obj = dr.GetValue(0);
                        break;
                    }
                    dr.Close();
                }
                return obj;
            }
        }

        /// <summary>
        /// 执行没有返回结果集的Sql
        /// </summary>
        /// <param name="strSql"></param>
        /// <returns></returns>
        public static int ExecuteNonQuery(string strSql)
        {
            DbConnection conn = DbFactory.CreateConnection;
            using (DbCommand dc = DbFactory.CreateCommand(strSql, conn))
            {
                dc.CommandTimeout = 0;
                try
                {
                    return dc.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    throw e;
                }
                finally
                {
                    DbFactory.CloseConnection(conn);
                }
            }
        }
        /// <summary>
        /// 执行返回DbDataReader结果集的sql
        /// </summary>
        /// <param name="conn">数据连接对象</param>
        /// <param name="strSql"></param>
        /// <returns></returns>
        public static DbDataReader ExecuteReader(DbConnection conn, string strSql)
        {
            using (DbCommand dc = DbFactory.CreateCommand(strSql, conn))
            {
                dc.CommandTimeout = 0;
                DbDataReader dr = dc.ExecuteReader();
                return dr;
            }
        }
        /// <summary>
        /// 执行返回DbDataReader结果集的sql
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="tran"></param>
        /// <param name="strSql"></param>
        /// <returns></returns>
        public static DbDataReader ExecuteReader(DbConnection conn, DbTransaction tran, string strSql)
        {
            using (DbCommand dc = DbFactory.CreateCommand(strSql, conn))
            {
                dc.CommandTimeout = 0;
                dc.Transaction = tran;
                DbDataReader dr = dc.ExecuteReader();
                return dr;
            }
        }
 
        /// <summary>
        /// 执行没有返回结果集的Sql,需要事务支持
        /// </summary>
        /// <param name="conn">连接对象</param>
        /// <param name="tran">事务对象</param>
        /// <param name="strSql">sql语句</param>
        /// <returns></returns>
        public static int ExecuteNonQuery(DbConnection conn, IDbTransaction tran, string strSql)
        {
            using (IDbCommand dc = DbFactory.CreateCommand(strSql, conn))
            {
                dc.CommandTimeout = 0;
                dc.Transaction = tran;
                try
                {
                    return dc.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                   throw e;
                }
            }
        }

        /// <summary>
        /// 设置DBDataAdapter的sql
        /// </summary>
        /// <param name="adapter"></param>
        public static void SetDbDataAdapterCommand(DbDataAdapter adapter)
        {
            DbCommandBuilder cb = CreateCommandBuilder(adapter);
            adapter.UpdateCommand = cb.GetUpdateCommand();
            adapter.InsertCommand = cb.GetInsertCommand();
            adapter.DeleteCommand = cb.GetDeleteCommand();

        }

        /// <summary>
        /// 设置DbDataAdapter的事务
        /// </summary>
        /// <param name="adapter"></param>
        /// <param name="tran"></param>
        public static void SetDbDataAdapterTransaction(DbDataAdapter adapter, DbTransaction tran)
        {
            adapter.DeleteCommand.Transaction = tran;
            adapter.SelectCommand.Transaction = tran;
            adapter.UpdateCommand.Transaction = tran;
            adapter.InsertCommand.Transaction = tran;
        }
        /// <summary>
        /// 保存数据集
        /// </summary>
        /// <param name="dsSource">数据源  (数据源中的表名必须是实际的数据表名)</param>
        /// <param name="conn">数据连接对象</param>
        /// <param name="tran">事物对象</param>
        public static void UpdateDateSet(DataSet dsSource, DbConnection conn, DbTransaction tran)
        {
            if (dsSource == null || conn == null || tran == null) return;
            if (dsSource.Tables.Count == 0) return;
            try
            {
                foreach (DataTable dtSource in dsSource.Tables)
                {
                    UpdateDataTable(dtSource, conn, tran);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        /// <summary>
        /// 保持数据表
        /// </summary>
        /// <param name="dtSource">数据源  (数据源中的表名必须是实际的数据表名)</param>
        /// <param name="conn"></param>
        /// <param name="tran"></param>
        public static void UpdateDataTable(DataTable dtSource, DbConnection conn, DbTransaction tran)
        {
            if (dtSource == null || conn == null || tran == null) return;
            try
            {
                string sqlCmd = string.Format("SELECT * FROM [{0}]", dtSource.TableName);
                DbCommand dbCommand = CreateCommand(sqlCmd, conn, tran);
                DbDataAdapter adapter = CreateAdapter(dbCommand);
                DbCommandBuilder cmdBuilder = CreateCommandBuilder(adapter);
                adapter.DeleteCommand = cmdBuilder.GetDeleteCommand();
                adapter.InsertCommand = cmdBuilder.GetInsertCommand();
                adapter.UpdateCommand = cmdBuilder.GetUpdateCommand();
                adapter.Update(dtSource);
            }
            catch (Exception e)
            {
                throw e;
            }

        }
        public static void BulkInsert<T>(string connection, string tableName, IList<T> list)
        {
            using (var bulkCopy = new SqlBulkCopy(connection))
            {
                bulkCopy.BatchSize = list.Count;
                bulkCopy.DestinationTableName = tableName;

                var table = new DataTable();
                var props = TypeDescriptor.GetProperties(typeof(T))
                   .Cast<PropertyDescriptor>()
                   .Where(propertyInfo => propertyInfo.PropertyType.Namespace.Equals("System"))
                   .ToArray();

                foreach (var propertyInfo in props)
                {
                    bulkCopy.ColumnMappings.Add(propertyInfo.Name, propertyInfo.Name);
                    table.Columns.Add(propertyInfo.Name, Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType);
                }

                var values = new object[props.Length];
                foreach (var item in list)
                {
                    for (var i = 0; i < values.Length; i++)
                    {
                        values[i] = props[i].GetValue(item);
                    }

                    table.Rows.Add(values);
                }

                bulkCopy.WriteToServer(table);
            }
        }
    }

}
