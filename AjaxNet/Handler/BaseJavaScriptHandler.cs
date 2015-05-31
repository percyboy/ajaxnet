/*
 * BaseJavaScriptHandler.cs
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
#endif

namespace AjaxNet
{
    /// <summary>
    /// Represents an IHttpHandler for the client-side JavaScript wrapper.
    /// </summary>
    public abstract class BaseJavaScriptHandler : IHttpHandler, IReadOnlySessionState	// need IReadOnlySessionState to check if using cookieless session ID
    {
        // TODO: The session ID has to be used in the cache of core and types.js
        protected string filename;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeJavaScriptHandler"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        internal BaseJavaScriptHandler(string filename)
        {
            this.filename = filename;
        }

        /// <summary>
        /// Enables processing of HTTP Web requests by a custom HttpHandler that implements the <see cref="T:System.Web.IHttpHandler"></see> interface.
        /// </summary>
        /// <param name="context">An <see cref="T:System.Web.HttpContext"></see> object that provides references to the intrinsic server objects (for example, Request, Response, Session, and Server) used to service HTTP requests.</param>
        public void ProcessRequest(HttpContext context)
        {
            // The request was not a request to invoke a server-side method.
            // Now, we will render the Javascript that will be used on the
            // client to run as a proxy or wrapper for the methods marked
            // with the AjaxMethodAttribute.

            if (context.Trace.IsEnabled) context.Trace.Write(Constant.AjaxID, "Render JavaScript: " + filename);

            // Check wether the javascript is already rendered and cached in the
            // current context.

            string etag = context.Request.Headers["If-None-Match"];
            string lastMod = context.Request.Headers["If-Modified-Since"];
            string cacheKey = Constant.AjaxID + "." + filename;
            CacheInfo ci = context.Cache[cacheKey] as CacheInfo;

            if (context.Trace.IsEnabled)
            {
                context.Trace.Write(Constant.AjaxID, "Client LastModified: " + lastMod);
                context.Trace.Write(Constant.AjaxID, "Server LastModified: " + (ci == null ? "null" : ci.LastModified));
            }

            if (etag != null && lastMod != null && ci != null)
            {
                if (etag == ci.ETag && lastMod == ci.LastModified)
                {
                    if (context.Trace.IsEnabled) context.Trace.Write(Constant.AjaxID, "No change. Response 304.");
                    context.Response.StatusCode = 304;
                    return;
                }
            }

            // Ok, client-side doesn't have cached script file yet
            // we need to response the script file.

            DateTime lastModDate = DateTime.Now;
            etag = MD5Helper.GetHash(filename);

            if (ci == null)
            {
                DateTime now = DateTime.Now;
                lastModDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second); //.ToUniversalTime();
                lastMod = now.ToString(@"ddd, dd MMM yyyy HH:mm:ss G\MT", System.Globalization.DateTimeFormatInfo.InvariantInfo);

                context.Cache.Add(cacheKey, new CacheInfo(etag, lastMod), null,
                    System.Web.Caching.Cache.NoAbsoluteExpiration, System.Web.Caching.Cache.NoSlidingExpiration,
                    System.Web.Caching.CacheItemPriority.Normal, null);

                if (context.Trace.IsEnabled) context.Trace.Write(Constant.AjaxID, "A new CacheInfo was Cached.");
            }
            else
            {
                lastMod = ci.LastModified;
                lastModDate = DateTime.ParseExact(lastMod, @"ddd, dd MMM yyyy HH:mm:ss G\MT",
                    System.Globalization.DateTimeFormatInfo.InvariantInfo);
            }

            // Use querystring 'e' to indicate a special output charset
            string charset = context.Request.QueryString["e"];
            Encoding encoding = Encoding.UTF8;
            if (String.IsNullOrEmpty(charset))
            {
                charset = "utf8";
                if (context.Trace.IsEnabled) context.Trace.Write(Constant.AjaxID, "Use default charset UTF8.");
            }
            else
            {
                try
                {
                    encoding = Encoding.GetEncoding(charset);
                    if (context.Trace.IsEnabled) context.Trace.Write(Constant.AjaxID, "Use charset: " + charset);
                }
                catch
                {
                    // ignore it
                    if (context.Trace.IsEnabled) context.Trace.Write(Constant.AjaxID, "Invalid charset: " + charset + ", use default charset UTF8.");
                    charset = "utf8";
                }
            }

            context.Response.ContentType = "application/x-javascript;charset=" + charset;
            context.Response.ContentEncoding = encoding;
            context.Response.CacheControl = "public";
            context.Response.Cache.SetCacheability(System.Web.HttpCacheability.Public);
            context.Response.Cache.SetETag(etag);
            context.Response.Cache.SetLastModified(lastModDate);

            // Now, check server-side-cache
            // If exists, write script file from cache

            cacheKey = Constant.AjaxID + ".script." + filename;
            string scripts = context.Cache[cacheKey] as string;

            if (scripts == null)
            {
                if (context.Trace.IsEnabled) context.Trace.Write(Constant.AjaxID, "No cache found. Generate it now.");
                scripts = GenerateScripts(context);

                context.Cache.Add(cacheKey, scripts, null,
                    System.Web.Caching.Cache.NoAbsoluteExpiration, System.Web.Caching.Cache.NoSlidingExpiration,
                    System.Web.Caching.CacheItemPriority.Normal, null);
            }
            else
            {
                if (context.Trace.IsEnabled) context.Trace.Write(Constant.AjaxID, "Use cached script.");
            }

            context.Response.Write(scripts);

            if (context.Trace.IsEnabled) context.Trace.Write(Constant.AjaxID, "End ProcessRequest");
        }

        /// <summary>
        /// Gets a value indicating whether another request can use the <see cref="T:System.Web.IHttpHandler"></see> instance.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Web.IHttpHandler"></see> instance is reusable; otherwise, false.</returns>
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        protected abstract string GenerateScripts(HttpContext context);
    }
}
