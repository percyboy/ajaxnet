/*
 * IDictionaryConverter.cs
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
 * MS	05-12-21	added Deserialize for Hashtables
 *					JavaScript object will now include the type for key and value
 * MS	06-04-25	removed unnecessarily used cast
 * MS	06-05-16	added Generic.IDictionary support
 *					(initial version with new client-side script)
 * MS	06-05-23	using local variables instead of "new Type()" for get De-/SerializableTypes
 * MS	06-06-09	removed addNamespace use
 * MS	06-06-14	changed access to keys and values
 * MS	06-09-26	improved performance using StringBuilder
 * MS	07-04-24	added renderJsonCompliant serialization
 * 
 * 
*/
using System;
using System.Reflection;
using System.Text;
using System.Collections;
using System.Collections.Specialized;

namespace AjaxNet
{
	/// <summary>
	/// Provides methods to serialize and deserialize an object that implements IDictionary.
	/// </summary>
	public class IDictionaryConverter : JavaScriptConverter
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="IDictionaryConverter"/> class.
        /// </summary>
		public IDictionaryConverter() : base()
		{
			m_AllowInheritance = true;
#if(NET20)
			m_serializableTypes = new Type[] { typeof(Hashtable), typeof(IDictionary), typeof(System.Collections.Generic.IDictionary<,>) };
			m_deserializableTypes = m_serializableTypes;
#else
			m_serializableTypes = new Type[] { typeof(IDictionary), typeof(NameValueCollection) };
			m_deserializableTypes = new Type[] { typeof(IDictionary), typeof(NameValueCollection) };
#endif
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

            IDictionary d = (IDictionary)Activator.CreateInstance(t);

            ParameterInfo[] p = t.GetMethod("Add").GetParameters();
            Type kT = p[0].ParameterType;
            Type vT = p[1].ParameterType;

            if (ht.ContainsKey("keys") && ht.ContainsKey("values"))
            {
                object key;
                object value;

                JavaScriptArray keys = ht["keys"] as JavaScriptArray;
                JavaScriptArray values = ht["values"] as JavaScriptArray;

                for (int i = 0; i < keys.Count && i < values.Count; i++)
                {
                    key = JavaScriptDeserializer.Deserialize(keys[i], kT);
                    value = JavaScriptDeserializer.Deserialize(values[i], vT);

                    d.Add(key, value);
                }
            }
            else if (kT == typeof(string))
            {
                foreach (string key in ht.Keys)
                {
                    object value = JavaScriptDeserializer.Deserialize(ht[key], vT);
                    d.Add(key, value);
                }
            }
            else
            {
                throw new NotSupportedException();
            }

            return d;
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
			IDictionary dic = o as IDictionary;

			if(dic == null)
				throw new NotSupportedException();

            Type t = o.GetType();
            IDictionaryEnumerator enumerable = dic.GetEnumerator();

			enumerable.Reset();
			bool b = true;

			sb.Append("{");
            sb.Append("\"keys\":[");

			while (enumerable.MoveNext())
			{
				if (b) { b = false; }
				else { sb.Append(","); }

				sb.Append(JavaScriptSerializer.Serialize(enumerable.Key));
			}
            sb.Append("],\"values\":[");

            enumerable.Reset();
            b = true;
            while (enumerable.MoveNext())
            {
                if (b) { b = false; }
                else { sb.Append(","); }

                sb.Append(JavaScriptSerializer.Serialize(enumerable.Value));
            }
            sb.Append("],\"__type\":");
            JavaScriptUtil.QuoteString(t.AssemblyQualifiedName, sb);
			sb.Append("}");
		}

        /// <summary>
        /// Tries the serialize value.
        /// </summary>
        /// <param name="o">The o.</param>
        /// <param name="t">The t.</param>
        /// <param name="sb">The sb.</param>
        /// <returns></returns>
		public override bool TrySerializeValue(object o, Type t, StringBuilder sb)
		{
#if(NET20)
			if (IsInterfaceImplemented(o, typeof(IDictionary)))
			{
				this.Serialize(o, sb);
				return true;
			}
#endif

			return base.TrySerializeValue(o, t, sb);
		}
#if(NET20)
        /// <summary>
        /// Determines whether [is interface implemented] [the specified obj].
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <returns>
        /// 	<c>true</c> if [is interface implemented] [the specified obj]; otherwise, <c>false</c>.
        /// </returns>
		internal static bool IsInterfaceImplemented(object obj, Type interfaceType)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");

			return
				obj.GetType().FindInterfaces(
					new TypeFilter(
						delegate(Type type, object filter)
						{
							return (type.Name == ((Type)filter).Name) &&
									(type.Namespace == ((Type)filter).Namespace);
						}),
						interfaceType
						).Length == 1;
		}
#endif
	}
}
