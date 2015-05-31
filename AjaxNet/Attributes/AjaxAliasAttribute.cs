/*
 * AjaxNamespaceAttribute.cs
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
 * MS	06-09-26	put regex to private const
 * MS	06-09-29	added enum support
 * 
 * 
 */
using System;
using System.Text.RegularExpressions;

namespace AjaxNet
{
	/// <summary>
	/// This attribute can be used to specified a different namespace for the client-side representation.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Enum, AllowMultiple = false)]
	public class AjaxAliasAttribute : Attribute
	{
		private string _clientAlias = null;
		private static Regex r = new Regex("^[a-zA-Z_]{1}([a-zA-Z_]*([\\d]*)?)*((\\.)?[a-zA-Z_]+([\\d]*)?)*$", 
            RegexOptions.Compiled);

        /// <summary>
        /// This attribute can be used to specified a different namespace for the client-side representation.
        /// </summary>
        /// <param name="clientAlias">The namespace to be used on the client-side JavaScript.</param>
		public AjaxAliasAttribute(string clientAlias)
		{
            if(!r.IsMatch(clientAlias) || clientAlias.StartsWith(".") || clientAlias.EndsWith("."))
                throw new NotSupportedException("The alias '" + clientAlias + "' is not supported.");

			_clientAlias = clientAlias;
		}

		#region Internal Properties

        /// <summary>
        /// Gets the client alias.
        /// </summary>
        /// <value>The client alias.</value>
		internal string ClientAlias
		{
			get
			{
                if (_clientAlias != null && _clientAlias.Trim().Length > 0)
                    return _clientAlias.Replace("-", "_").Replace(" ", "_");

				return _clientAlias;
			}
		}

		#endregion
	}
}
