/*
 * DataSetConverter.cs
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
 * MS	06-05-23	using local variables instead of "new Type()" for get De-/SerializableTypes
 * MS	06-06-09	removed addNamespace use
 * MS	06-06-22	added AllowInheritance=true
 * MS	06-09-26	improved performance using StringBuilder
 * MS	07-04-24	added renderJsonCompliant serialization
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
	/// Provides methods to serialize and deserialize a DataSet object.
	/// </summary>
	public class DataSetConverter : JavaScriptConverter
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="DataSetConverter"/> class.
        /// </summary>
		public DataSetConverter() : base()
		{
			m_AllowInheritance = true;

			m_serializableTypes = new Type[] { typeof(DataSet) };
			m_deserializableTypes = new Type[] { typeof(DataSet) };
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

			if(ht == null)
				throw new NotSupportedException();

			if(!ht.ContainsKey("tables") || !(ht["tables"] is JavaScriptArray))
				throw new NotSupportedException();

			JavaScriptArray tables = (JavaScriptArray)ht["tables"];
			
			DataSet ds = new DataSet();
			DataTable dt = null;

			foreach(IJavaScriptObject table in tables)
			{
				dt = (DataTable)JavaScriptDeserializer.Deserialize(table, typeof(DataTable));

				if(dt != null)
					ds.Tables.Add(dt);
			}

			return ds;			
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
			DataSet ds = o as DataSet;

			if(ds == null)
				throw new NotSupportedException();

            sb.Append("{\"__type\":\"System.Data.DataSet,System.Data\",\"tables\":[");

            bool b = true;

			foreach(DataTable dt in ds.Tables)
			{
				if(b){ b = false; }
				else{ sb.Append(","); }

				JavaScriptSerializer.Serialize(dt, sb);
			}

			sb.Append("]}");
		}
	}
}
