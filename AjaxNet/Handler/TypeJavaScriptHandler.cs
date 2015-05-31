/*
 * TypeJavaScriptHandler.cs
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
 * MS	06-04-05	fixed sessionID on ASP.NET 2.0
 *					fixed Object.prototype.extend problem when running with third-party libs
 * MS	06-04-12	added useAssemblyQualifiedName
 * MS	06-04-25	fixed forms authentication cookieless configuration
 * MS	06-05-15	removed Class.create for JavaScript proxy
 * MS	06-05-23	using AjaxNamespace name for method
 * MS	06-06-06	fixed If-Modified-Since http header if using zip
 * MS	06-06-09	removed addNamespace use
 * MS	07-04-24	using new TypeJavaScriptProvider
 * MS	08-03-24	added patch 47, mdissel, NullReference exception when compiling .NET 1.1
 * 
 * 
 * 
 */
using System;
using System.Reflection;
using System.Web;
using System.Web.SessionState;
using System.Web.Caching;
using System.IO;
using System.Security.Permissions;
using System.Web.Security;
#if(NET20)
using System.Collections.Generic;
using System.Text;
using System.Web.Management;
#endif

namespace AjaxNet
{
	/// <summary>
	/// Represents an IHttpHandler for the client-side JavaScript wrapper.
	/// </summary>
    internal class TypeJavaScriptHandler : BaseJavaScriptHandler
	{
		// TODO: The session ID has to be used in the cache of core and types.js
		//private Type type;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeJavaScriptHandler"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
		internal TypeJavaScriptHandler(string filename) : base(filename)
		{
		}

        /// <summary>
        /// Enables processing of HTTP Web requests by a custom HttpHandler that implements the <see cref="T:System.Web.IHttpHandler"></see> interface.
        /// </summary>
        /// <param name="context">An <see cref="T:System.Web.HttpContext"></see> object that provides references to the intrinsic server objects (for example, Request, Response, Session, and Server) used to service HTTP requests.</param>
		protected override string  GenerateScripts(HttpContext context)
		{
            StringBuilder sb = new StringBuilder();

            string className = filename;

			if(Utility.Settings != null && Utility.Settings.UrlClassMappings.ContainsKey(filename))
			{
                className = Utility.Settings.UrlClassMappings[filename] as string;
                if (context.Trace.IsEnabled) context.Trace.Write(Constant.AjaxID, "Url match to Type: " + className);
            }

            Type type = null;

            try
            {
                type = Type.GetType(className, true);
            }
            catch (Exception ex)
            {
                if (context.Trace.IsEnabled) { context.Trace.Write(Constant.AjaxID, "Type not found: " + className); ex.ToString(); }
#if(WEBEVENT)
				string errorText = string.Format(Constant.AjaxID + " Error", context.User.Identity.Name);

				Management.WebAjaxErrorEvent ev = new Management.WebAjaxErrorEvent(errorText, WebEventCodes.WebExtendedBase + 201, ex);
				ev.Raise();
#endif
                return null;
            }

			// Ok, we do not have the javascript rendered, yet.
			// Build the javascript source and save it to the current
			// Application context.

            // type handler url
			string url = context.Request.ApplicationPath 
                + (context.Request.ApplicationPath.EndsWith("/") ? "" : "/") 
                + Utility.HandlerPath + "/" + AjaxNet.Utility.GetSessionUri() + filename + Utility.HandlerExtension;

			// find all methods that are able to be used with AjaxNet

			MethodInfo[] mi = type.GetMethods();
			MethodInfo method;
#if(NET20)
			List<MethodInfo> methods = new List<MethodInfo>();
#else
			MethodInfo[] methods;
			System.Collections.ArrayList methodList = new System.Collections.ArrayList();

			int mc = 0;
#endif

			for (int y = 0; y < mi.Length; y++)
			{
				method = mi[y];

				if (!method.IsPublic)
					continue;

				AjaxMethodAttribute[] ma = (AjaxMethodAttribute[])method.GetCustomAttributes(typeof(AjaxMethodAttribute), true);

				if (ma.Length == 0)
					continue;

				PrincipalPermissionAttribute[] ppa = (PrincipalPermissionAttribute[])method.GetCustomAttributes(typeof(PrincipalPermissionAttribute), true);
				if (ppa.Length > 0)
				{
					bool permissionDenied = true;
					for (int p = 0; p < ppa.Length && permissionDenied; p++)
					{
#if(_____NET20)
						if (Roles.Enabled)
						{
							try
							{
								if (!String.IsNullOrEmpty(ppa[p].Role) && !Roles.IsUserInRole(ppa[p].Role))
									continue;
							}
							catch (Exception)
							{
								// Should we disable this AjaxMethod of there is an exception?
								continue;
							}

						}
						else
#endif
							if (ppa[p].Role != null && ppa[p].Role.Length > 0 && context.User != null && context.User.Identity.IsAuthenticated && !context.User.IsInRole(ppa[p].Role))
								continue;

						permissionDenied = false;
					}

					if (permissionDenied)
						continue;
				}

#if(NET20)
				methods.Add(method);
#else
				//methods[mc++] = method;
				methodList.Add(method);
#endif
			}

#if(!NET20)
			methods = (MethodInfo[])methodList.ToArray(typeof(MethodInfo));
#endif

			// render client-side proxy file
			TypeJavaScriptGenerator jsp = null;

			if (Utility.Settings.TypeJavaScriptGenerator != null)
			{
				try
				{
					Type jspt = Type.GetType(Utility.Settings.TypeJavaScriptGenerator);
					if (jspt != null && typeof(TypeJavaScriptGenerator).IsAssignableFrom(jspt))
					{
						jsp = (TypeJavaScriptGenerator)Activator.CreateInstance(jspt, new object[3] { type, url, sb });
					}
				}
				catch (Exception)
				{
				}
			}

			if (jsp == null)
			{
				jsp = new TypeJavaScriptGenerator(type, url, sb);
			}

			jsp.RenderNamespace();
			jsp.RenderClassBegin();
#if(NET20)
			jsp.RenderMethods(methods.ToArray());
#else
			jsp.RenderMethods(methods);
#endif
			jsp.RenderClassEnd();

            return sb.ToString();
		}
	}
}
