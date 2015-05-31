/*
 * EmbeddedJavaScriptHandler.cs
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
 * MS	06-04-05	added oldstyled Object.prototype.extend code, enabled by web.config
 *					setting oldStyle\objectExtendPrototype
 * MS	06-05-22	added possibility to have one file for prototype,core instead of two
 * MS	06-06-06	fixed If-Modified-Since http header if using zip
 * MS	06-06-07	changed to internal
 * 
 * 
 */
using System;
using System.Reflection;
using System.Web;
using System.Web.Caching;
using System.IO;
using System.Text;

namespace AjaxNet
{
	/// <summary>
	/// Represents an IHttpHandler for the client-side JavaScript prototype and core methods.
	/// </summary>
    internal class EmbeddedJavaScriptHandler : BaseJavaScriptHandler
	{


        /// <summary>
        /// Initializes a new instance of the <see cref="EmbeddedJavaScriptHandler"/> class.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
		internal EmbeddedJavaScriptHandler(string fileName) : base(fileName)
		{
		}

        /// <summary>
        /// Enables processing of HTTP Web requests by a custom HttpHandler that implements the <see cref="T:System.Web.IHttpHandler"></see> interface.
        /// </summary>
        /// <param name="context">An <see cref="T:System.Web.HttpContext"></see> object that provides references to the intrinsic server objects (for example, Request, Response, Session, and Server) used to service HTTP requests.</param>
        protected override string GenerateScripts(HttpContext context)
		{
            StringBuilder sb = new StringBuilder();

			// Now, we want to read the JavaScript embedded source
			// from the assembly. If the filename includes any comma
			// we have to return more than one embedded JavaScript file.

			if (filename != null && filename.Length > 0)
			{
				string[] files = filename.Split(',');
				Assembly assembly = Assembly.GetExecutingAssembly();
				Stream s;

				for (int i = 0; i < files.Length; i++)
				{
					s = assembly.GetManifestResourceStream(Constant.AssemblyName + "." + files[i] + ".js");

					if (s != null)
					{
						System.IO.StreamReader sr = new System.IO.StreamReader(s);

						sb.Append("// " + files[i] + ".js\r\n");
						sb.Append(sr.ReadToEnd());
						sb.Append("\r\n");

						sr.Close();
					}
				}

                if (filename == "core" || filename.StartsWith("core."))
                {
                    if (Utility.Settings != null && Utility.Settings.Security != null)
                    {
                        try
                        {
                            string secrityScript = Utility.Settings.Security.SecurityProvider.ClientScript;

                            if (secrityScript != null && secrityScript.Length > 0)
                            {
                                sb.Append("//security provider\r\n");
                                sb.Append(secrityScript);
                                context.Response.Write("\r\n");
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
			}

            return sb.ToString();
		}

	}
}
