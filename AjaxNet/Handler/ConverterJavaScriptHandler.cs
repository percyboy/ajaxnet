/*
 * ConverterJavaScriptHandler.cs
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
 * MS	06-05-30	changed using new converter collections
 * MS	06-06-06	fixed If-Modified-Since http header if using zip
 * MS	06-06-07	changed to internal
 * MS	07-04-24	using new SecurityProvider
 * 
 */
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Web;
using System.Web.Caching;
using System.IO;
using System.Text;

namespace AjaxNet
{
	/// <summary>
	/// Represents an IHttpHandler for the client-side JavaScript converter methods.
	/// </summary>
    internal class ConverterJavaScriptHandler : BaseJavaScriptHandler
	{
        internal ConverterJavaScriptHandler()
            : base("converter")
		{
		}

        /// <summary>
        /// Enables processing of HTTP Web requests by a custom HttpHandler that implements the <see cref="T:System.Web.IHttpHandler"></see> interface.
        /// </summary>
        /// <param name="context">An <see cref="T:System.Web.HttpContext"></see> object that provides references to the intrinsic server objects (for example, Request, Response, Session, and Server) used to service HTTP requests.</param>
        protected override string GenerateScripts(HttpContext context)
		{
            StringBuilder sb = new StringBuilder();

			StringCollection convTypes = new StringCollection();
			string convTypeName;
			JavaScriptConverter c;
			string script;

			IEnumerator s = Utility.Settings.SerializableConverters.Values.GetEnumerator();
			while(s.MoveNext())
			{
				c = (JavaScriptConverter)s.Current;
				convTypeName = c.GetType().FullName;
				if (!convTypes.Contains(convTypeName))
				{
					script = c.GetClientScript();

					if (script.Length > 0)
					{
#if(NET20)
						if(!String.IsNullOrEmpty(c.ConverterName))
#else
						if(c.ConverterName != null && c.ConverterName.Length > 0)
#endif
						sb.Append("// " + c.ConverterName + "\r\n");
						sb.Append(script);
						sb.Append("\r\n");
					}

					convTypes.Add(convTypeName);
				}
			}

			IEnumerator d = Utility.Settings.DeserializableConverters.Values.GetEnumerator();
			while(d.MoveNext())
			{
				c = (JavaScriptConverter)d.Current;
				convTypeName = c.GetType().FullName;
				if (!convTypes.Contains(convTypeName))
				{
					script = c.GetClientScript();

					if (script.Length > 0)
					{
#if(NET20)
						if (!String.IsNullOrEmpty(c.ConverterName))
#else
						if(c.ConverterName != null && c.ConverterName.Length > 0)
#endif

						sb.Append("// " + c.ConverterName + "\r\n");
						sb.Append(script);
						sb.Append("\r\n");
					}

					convTypes.Add(convTypeName);
				}
			}

            return sb.ToString();
		}
	}
}
