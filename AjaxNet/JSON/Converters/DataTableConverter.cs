/*
 * DataTableConverter.cs
 * 
 * Copyright (c)2007 Michael Schwarz (http://www.ajaxpro.info).
 * All Rights Reserved.
 * 
 * Permission is hereby granted, free of charge, to any person 
 * obtaining a copy of this software and associated documentation 
 * files (the "Software"), to deal in the Software without 
 * restriction, including without limitation the rights to use, 
 * copy, modify, merge, publish, distribute, sublicense, and/or 
 * sell copies of the Software, and to permit persons to whom the 
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be 
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR 
 * ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
 * CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
/*
 * MS	06-04-25	removed unnecessarily used cast
 * MS	06-04-26	fixed null values to DBNull.Value (thanks to allex@as.ro)
 * MS	06-05-23	using local variables instead of "new Type()" for get De-/SerializableTypes
 * MS	06-06-09	removed addNamespace use
 * MS	06-06-22	added AllowInheritance=true
 * MS	06-09-24	use QuoteString instead of Serialize
 * MS	06-09-26	improved performance using StringBuilder
 * MS	07-04-24	added renderJsonCompliant serialization
 * MS	08-03-21	fixed DataTable client-side script
 * 
 * 
 * 
 */
using System;
using System.Text;
using System.Data;

namespace AjaxNet
{
	/// <summary>
	/// Provides methods to serialize and deserialize a DataTable object.
	/// </summary>
	public class DataTableConverter : JavaScriptConverter
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DataTableConverter"/> class.
		/// </summary>
		public DataTableConverter()
			: base()
		{
			m_AllowInheritance = true;

			m_serializableTypes = new Type[] { typeof(DataTable) };
			m_deserializableTypes = new Type[] { typeof(DataTable) };
		}

		/// <summary>
		/// Converts an IJavaScriptObject into an NET object.
		/// </summary>
		/// <param name="o">The IJavaScriptObject object to convert.</param>
		/// <param name="t"></param>
		/// <returns>Returns a .NET object.</returns>
		public override object Deserialize(IJavaScriptObject o, Type t)
		{
			JavaScriptObject ht = o as JavaScriptObject;

			if (ht == null)
				throw new NotSupportedException();

			if (!ht.ContainsKey("columns") || !(ht["columns"] is JavaScriptArray) ||
                !ht.ContainsKey("columnTypes") || !(ht["columnTypes"] is JavaScriptArray) ||
				!ht.ContainsKey("rows") || !(ht["rows"] is JavaScriptArray))
			{
				throw new NotSupportedException();
			}

			JavaScriptArray columns = (JavaScriptArray)ht["columns"];
            JavaScriptArray columnTypes = (JavaScriptArray)ht["columnTypes"];
			JavaScriptArray rows = (JavaScriptArray)ht["rows"];

            if (columns.Count != columnTypes.Count)
            {
                throw new NotSupportedException();
            }

			DataTable dt = new DataTable();
			DataRow row = null;
			Type colType;
            JavaScriptString columnName;
            JavaScriptString columnType;

			if (ht.ContainsKey("name") && ht["name"] is JavaScriptString)
				dt.TableName = ht["name"].ToString();

			for (int i = 0; i < columns.Count; i++)
			{
				columnName = (JavaScriptString)columns[i];
                columnType = (JavaScriptString)columnTypes[i];

				colType = Type.GetType(columnType, true);
				dt.Columns.Add(columnName, colType);
			}

			JavaScriptArray cols = null;
			object obj;

			for (int y = 0; y < rows.Count; y++)
			{
				//				if(!(r is JavaScriptArray))
				//					continue;

				cols = (JavaScriptArray)rows[y];
				row = dt.NewRow();

				for (int i = 0; i < cols.Count; i++)
				{
					//row[i] = JavaScriptDeserializer.Deserialize((IJavaScriptObject)cols[i], dt.Columns[i].DataType);

					obj = JavaScriptDeserializer.Deserialize((IJavaScriptObject)cols[i], dt.Columns[i].DataType);
					row[i] = (obj == null) ? DBNull.Value : obj;
				}

				dt.Rows.Add(row);
			}

			return dt;
		}

		/// <summary>
		/// Converts a .NET object into a JSON string.
		/// </summary>
		/// <param name="o">The object to convert.</param>
		/// <returns>Returns a JSON string.</returns>
		public override string Serialize(object o)
		{
			StringBuilder sb = new StringBuilder();
			Serialize(o, sb);
			return sb.ToString();
		}

		/// <summary>
		/// Serializes the specified o.
		/// </summary>
		/// <param name="o">The o.</param>
		/// <param name="sb">The sb.</param>
		public override void Serialize(object o, StringBuilder sb)
		{
			DataTable dt = o as DataTable;

			if (dt == null)
				throw new NotSupportedException();

			DataColumnCollection cols = dt.Columns;
			DataRowCollection rows = dt.Rows;

			bool b = true;

            sb.Append("{\"__type\":\"System.Data.DataTable,System.Data\",name:");
            JavaScriptUtil.QuoteString(dt.TableName, sb);
            sb.Append(",\"columns\":[");

            foreach (DataColumn col in dt.Columns)
            {
                if (b) { b = false; }
                else { sb.Append(","); }
                JavaScriptUtil.QuoteString(col.ColumnName, sb);
            }

            sb.Append("],\"columnTypes\":[");
            b = true;

            foreach (DataColumn col in dt.Columns)
            {
                if (b) { b = false; }
                else { sb.Append(","); }
                JavaScriptUtil.QuoteString(col.DataType.AssemblyQualifiedName, sb);
            }

            sb.Append("],\"rows\":[");
            b = true;

            foreach (DataRow row in rows)
            {
                if (b) { b = false; }
                else { sb.Append(","); }

                JavaScriptSerializer.Serialize(row, sb);
            }
            sb.Append("]}");
		}
	}
}