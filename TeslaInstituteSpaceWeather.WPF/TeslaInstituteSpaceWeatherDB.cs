using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;

namespace TeslaInstituteSpaceWeather.WPF
{
    public class TeslaInstituteSpaceWeatherDB
    {
        public static string connectionString =
            "Host=<YOUR DATABASE IP ADDRESS HERE>;Username=<YOUR DB USERNAME HERE>;Password=<YOUR DB PASSWORD HERE>;Database=TeslaInstituteSpaceWeather";

        public void WriteToServer<T>(NpgsqlConnection conn, IEnumerable<T> data, string DestinationTableName)
        {
            try
            {
                if (DestinationTableName == null || DestinationTableName == "")
                {
                    throw new ArgumentOutOfRangeException("DestinationTableName", "Destination table must be set");
                }
                PropertyInfo[] properties = typeof(T).GetProperties();
                int colCount = properties.Length;

                NpgsqlDbType[] types = new NpgsqlDbType[colCount];
                int[] lengths = new int[colCount];
                string[] fieldNames = new string[colCount];

                using (var cmd = new NpgsqlCommand("SELECT * FROM " + DestinationTableName + " LIMIT 1", conn))
                {
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.FieldCount != colCount)
                        {
                            throw new ArgumentOutOfRangeException("dataTable", "Column count in Destination Table does not match column count in source table.");
                        }
                        var columns = rdr.GetColumnSchema();
                        for (int i = 0; i < colCount; i++)
                        {
                            types[i] = (NpgsqlDbType)columns[i].NpgsqlDbType;
                            lengths[i] = columns[i].ColumnSize == null ? 0 : (int)columns[i].ColumnSize;
                            fieldNames[i] = columns[i].ColumnName;
                        }
                    }

                }
                var sB = new StringBuilder(fieldNames[0]);
                for (int p = 1; p < colCount; p++)
                {
                    sB.Append(", " + fieldNames[p]);
                }
                using (var writer = conn.BeginBinaryImport("COPY " + DestinationTableName + " (" + sB.ToString() + ") FROM STDIN (FORMAT BINARY)"))
                {
                    foreach (var t in data)
                    {
                        writer.StartRow();

                        for (int i = 0; i < colCount; i++)
                        {
                            if (properties[i].GetValue(t) == null)
                            {
                                writer.WriteNull();
                            }
                            else
                            {
                                switch (types[i])
                                {
                                    case NpgsqlDbType.Bigint:
                                        writer.Write((long)properties[i].GetValue(t), types[i]);
                                        break;
                                    case NpgsqlDbType.Bit:
                                        if (lengths[i] > 1)
                                        {
                                            writer.Write((byte[])properties[i].GetValue(t), types[i]);
                                        }
                                        else
                                        {
                                            writer.Write((byte)properties[i].GetValue(t), types[i]);
                                        }
                                        break;
                                    case NpgsqlDbType.Boolean:
                                        writer.Write((bool)properties[i].GetValue(t), types[i]);
                                        break;
                                    case NpgsqlDbType.Bytea:
                                        writer.Write((byte[])properties[i].GetValue(t), types[i]);
                                        break;
                                    case NpgsqlDbType.Char:
                                        if (properties[i].GetType() == typeof(string))
                                        {
                                            writer.Write((string)properties[i].GetValue(t), types[i]);
                                        }
                                        else if (properties[i].GetType() == typeof(Guid))
                                        {
                                            var value = properties[i].GetValue(t).ToString();
                                            writer.Write(value, types[i]);
                                        }


                                        else if (lengths[i] > 1)
                                        {
                                            writer.Write((char[])properties[i].GetValue(t), types[i]);
                                        }
                                        else
                                        {

                                            var s = ((string)properties[i].GetValue(t).ToString()).ToCharArray();
                                            writer.Write(s[0], types[i]);
                                        }
                                        break;
                                    case NpgsqlDbType.Time:
                                    case NpgsqlDbType.Timestamp:
                                    case NpgsqlDbType.TimestampTz:
                                    case NpgsqlDbType.Date:
                                        writer.Write((DateTime)properties[i].GetValue(t), types[i]);
                                        break;
                                    case NpgsqlDbType.Double:
                                        writer.Write((double)properties[i].GetValue(t), types[i]);
                                        break;
                                    case NpgsqlDbType.Integer:
                                        try
                                        {
                                            if (properties[i].GetType() == typeof(int))
                                            {
                                                writer.Write((int)properties[i].GetValue(t), types[i]);
                                                break;
                                            }
                                            else if (properties[i].GetType() == typeof(string))
                                            {
                                                var swap = Convert.ToInt32(properties[i].GetValue(t));
                                                writer.Write((int)swap, types[i]);
                                                break;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            string sh = ex.Message;
                                        }

                                        writer.Write((object)properties[i].GetValue(t), types[i]);
                                        break;
                                    case NpgsqlDbType.Interval:
                                        writer.Write((TimeSpan)properties[i].GetValue(t), types[i]);
                                        break;
                                    case NpgsqlDbType.Numeric:
                                    case NpgsqlDbType.Money:
                                        writer.Write((decimal)properties[i].GetValue(t), types[i]);
                                        break;
                                    case NpgsqlDbType.Real:
                                        writer.Write((Single)properties[i].GetValue(t), types[i]);
                                        break;
                                    case NpgsqlDbType.Smallint:

                                        try
                                        {
                                            if (properties[i].GetType() == typeof(byte))
                                            {
                                                var swap = Convert.ToInt16(properties[i].GetValue(t));
                                                writer.Write((short)swap, types[i]);
                                                break;
                                            }
                                            writer.Write((short)properties[i].GetValue(t), types[i]);
                                        }
                                        catch (Exception ex)
                                        {
                                            string ms = ex.Message;
                                        }

                                        break;
                                    case NpgsqlDbType.Varchar:
                                    case NpgsqlDbType.Text:
                                        writer.Write((string)properties[i].GetValue(t), types[i]);
                                        break;
                                    case NpgsqlDbType.Uuid:
                                        writer.Write((Guid)properties[i].GetValue(t), types[i]);
                                        break;
                                    case NpgsqlDbType.Xml:
                                        writer.Write((string)properties[i].GetValue(t), types[i]);
                                        break;
                                }
                            }
                        }
                    }
                    writer.Complete();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing NpgSqlBulkCopy.WriteToServer().  See inner exception for details", ex);
            }
        } // end 

        public void WriteToServer(NpgsqlConnection conn, DataTable dataTable, string DestinationTableName)
        {
            try
            {
                if (DestinationTableName == null || DestinationTableName == "")
                {
                    throw new ArgumentOutOfRangeException("DestinationTableName", "Destination table must be set");
                }
                int colCount = dataTable.Columns.Count;

                NpgsqlDbType[] types = new NpgsqlDbType[colCount];
                int[] lengths = new int[colCount];
                string[] fieldNames = new string[colCount];

                using (var cmd = new NpgsqlCommand("SELECT * FROM " + DestinationTableName + " LIMIT 1", conn))
                {
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.FieldCount != colCount)
                        {
                            throw new ArgumentOutOfRangeException("dataTable", "Column count in Destination Table does not match column count in source table.");
                        }
                        var columns = rdr.GetColumnSchema();
                        for (int i = 0; i < colCount; i++)
                        {
                            types[i] = (NpgsqlDbType)columns[i].NpgsqlDbType;
                            lengths[i] = columns[i].ColumnSize == null ? 0 : (int)columns[i].ColumnSize;
                            fieldNames[i] = columns[i].ColumnName;
                        }
                    }

                }
                var sB = new StringBuilder(fieldNames[0]);
                for (int p = 1; p < colCount; p++)
                {
                    sB.Append(", " + fieldNames[p]);
                }
                using (var writer = conn.BeginBinaryImport("COPY " + DestinationTableName + " (" + sB.ToString() + ") FROM STDIN (FORMAT BINARY)"))
                {
                    for (int j = 0; j < dataTable.Rows.Count; j++)
                    {
                        DataRow dR = dataTable.Rows[j];
                        writer.StartRow();

                        for (int i = 0; i < colCount; i++)
                        {
                            if (dR[i] == DBNull.Value)
                            {
                                writer.WriteNull();
                            }
                            else
                            {
                                switch (types[i])
                                {
                                    case NpgsqlDbType.Bigint:
                                        writer.Write((long)dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Bit:
                                        if (lengths[i] > 1)
                                        {
                                            writer.Write((byte[])dR[i], types[i]);
                                        }
                                        else
                                        {
                                            writer.Write((byte)dR[i], types[i]);
                                        }
                                        break;
                                    case NpgsqlDbType.Boolean:
                                        writer.Write((bool)dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Bytea:
                                        writer.Write((byte[])dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Char:
                                        if (dR[i] is string)
                                        {
                                            writer.Write((string)dR[i], types[i]);
                                        }
                                        else if (dR[i] is Guid)
                                        {
                                            var value = dR[i].ToString();
                                            writer.Write(value, types[i]);
                                        }


                                        else if (lengths[i] > 1)
                                        {
                                            writer.Write((char[])dR[i], types[i]);
                                        }
                                        else
                                        {

                                            var s = ((string)dR[i].ToString()).ToCharArray();
                                            writer.Write(s[0], types[i]);
                                        }
                                        break;
                                    case NpgsqlDbType.Time:
                                    case NpgsqlDbType.Timestamp:
                                    case NpgsqlDbType.TimestampTz:
                                    case NpgsqlDbType.Date:
                                        writer.Write((DateTime)dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Double:
                                        writer.Write((double)dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Integer:
                                        try
                                        {
                                            if (dR[i] is int)
                                            {
                                                writer.Write((int)dR[i], types[i]);
                                                break;
                                            }
                                            else if (dR[i] is string)
                                            {
                                                var swap = Convert.ToInt32(dR[i]);
                                                writer.Write((int)swap, types[i]);
                                                break;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            string sh = ex.Message;
                                        }

                                        writer.Write((object)dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Interval:
                                        writer.Write((TimeSpan)dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Numeric:
                                    case NpgsqlDbType.Money:
                                        writer.Write((decimal)dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Real:
                                        writer.Write((Single)dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Smallint:

                                        try
                                        {
                                            if (dR[i] is byte)
                                            {
                                                var swap = Convert.ToInt16(dR[i]);
                                                writer.Write((short)swap, types[i]);
                                                break;
                                            }
                                            writer.Write((short)dR[i], types[i]);
                                        }
                                        catch (Exception ex)
                                        {
                                            string ms = ex.Message;
                                        }

                                        break;
                                    case NpgsqlDbType.Varchar:
                                    case NpgsqlDbType.Text:
                                        writer.Write((string)dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Uuid:
                                        writer.Write((Guid)dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Xml:
                                        writer.Write((string)dR[i], types[i]);
                                        break;
                                }
                            }
                        }
                    }
                    writer.Complete();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing NpgSqlBulkCopy.WriteToServer().  See inner exception for details", ex);
            }

        } // end 
    }
}
