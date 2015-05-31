using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace AjaxNet
{
    internal class ExtJSProcessor : AjaxProcessor
    {
        private int hashCode;

        internal ExtJSProcessor(HttpContext context, Type type)
            : base(context, type)
        {
        }

        /// <summary>
        /// Gets a value indicating whether this instance can handle request.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance can handle request; otherwise, <c>false</c>.
        /// </value>
        internal override bool CanHandleRequest
        {
            get
            {
                if (context.Request["method"] != null)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// Gets the ajax method.
        /// </summary>
        /// <value>The ajax method.</value>
        public override MethodInfo AjaxMethod
        {
            get
            {
                string m = context.Request["method"];

                if (m != null && m.Length > 0)
                    return this.GetMethodInfo(m);

                return null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is encryption able.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is encryption able; otherwise, <c>false</c>.
        /// </value>
        public override bool IsEncryptionAble
        {
            get
            {
                //因为浏览器侧没有额外脚本以支持加解密，所以服务器端也没必要实现
                return false;
            }
        }

        /// <summary>
        /// Serializes the object.
        /// </summary>
        /// <param name="o">The o.</param>
        /// <returns></returns>
        public override string SerializeObject(object o)
        {
            // callback是ExtJS在跨站情景时用ScriptTagProxy方式请求的情况
            string callback = context.Request["callback"];
            string res;
            if (o is Exception)
            {
                if (!string.IsNullOrEmpty(callback))
                {
                    Exception ex = o as Exception;
                    res = string.Format("Ext.Msg.alert('Error', {0});",
                        JavaScriptUtil.QuoteString(ex.Message));
                }
                else
                {
                    res = "{\"error\":" + JavaScriptSerializer.Serialize(o) + "}";
                    context.Response.StatusCode = 500;
                }
            }
            else
            {
                res = JavaScriptSerializer.Serialize(o);
                if (!string.IsNullOrEmpty(callback))
                {
                    res = callback + "(" + res + ");";
                }
            }

            context.Response.Write(res);

            return res;
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return hashCode;
        }

        /// <summary>
        /// Retreives the parameters.
        /// </summary>
        /// <returns></returns>
        public override object[] RetreiveParameters()
        {
            ParameterInfo[] pis = method.GetParameters();

            object[] args = new object[pis.Length];

            // initialize default values
            for (int i = 0; i < pis.Length; i++)
            {
                args[i] = pis[i].DefaultValue;
            }

            List<string> keys = new List<string>();
            foreach (string key in context.Request.QueryString.Keys)
            {
                keys.Add(key);
            }
            foreach (string key in context.Request.Form.Keys)
            {
                if (!keys.Contains(key)) keys.Add(key);
            }
            //keys.Sort();

            Dictionary<string, string> values = new Dictionary<string, string>();
            foreach (string key in keys)
            {
                values.Add(key, context.Request.Params[key]);
            }

            for (int i = 0; i < pis.Length; i++)
            {
                IJavaScriptObject jso = Process(pis[i].Name, values);
                if (jso != null)
                {
                    args[i] = JavaScriptDeserializer.Deserialize(jso, pis[i].ParameterType);
                    if (hashCode == 0)
                    {
                        hashCode = jso.Value.GetHashCode();
                    }
                    else
                    {
                        hashCode ^= jso.Value.GetHashCode();
                    }
                }
            }

            return args;
        }

        private static IJavaScriptObject Process(string paraName, Dictionary<string, string> values)
        {
            IJavaScriptObject root = null;

            foreach (var kv in values)
            {
                if (kv.Key == paraName
                    || kv.Key.StartsWith(paraName + ".")
                    || kv.Key.StartsWith(paraName + "["))
                {
                    root = Process(root, kv.Key, kv.Value);
                }
            }
            return root;
        }

        private static IJavaScriptObject Process(IJavaScriptObject root, string key, string value)
        {
            string[] parts = key.Split(new char[] { '.', '[', ']' },
                StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                root = new JavaScriptString(value);
            }
            else
            {
                if (IsNumber(parts[1]) && !(root is JavaScriptArray))
                {
                    root = new JavaScriptArray();
                }
                if (!IsNumber(parts[1]) && !(root is JavaScriptObject))
                {
                    root = new JavaScriptObject();
                }

                IJavaScriptObject prev = root;

                for (int i = 1; i < parts.Length; i++)
                {
                    string part = parts[i];
                    if (IsNumber(part))
                    {
                        int index = Convert.ToInt32(part);
                        if (i < parts.Length - 1)
                        {
                            if (((JavaScriptArray)prev).Count > index)
                            {
                                bool nextArray = IsNumber(parts[i + 1]);
                                if ((nextArray && ((JavaScriptArray)prev)[index] is JavaScriptArray)
                                    || (!nextArray && ((JavaScriptArray)prev)[index] is JavaScriptObject))
                                {
                                    prev = ((JavaScriptArray)prev)[index];
                                    continue;
                                }
                                ((JavaScriptArray)prev)[index] = Process(parts, i, value);
                                break;
                            }
                        }
                        else
                        {
                            //最后一段应该是string
                            if (((JavaScriptArray)prev).Count > index)
                            {
                                ((JavaScriptArray)prev)[index] = Process(parts, i, value);
                                break;
                            }
                        }
                        for (int j = ((JavaScriptArray)prev).Count; j < index; j++)
                        {
                            ((JavaScriptArray)prev).Add(null);
                        }
                        ((JavaScriptArray)prev).Add(Process(parts, i, value));
                        prev = ((JavaScriptArray)prev)[index];
                        break;
                    }
                    else
                    {
                        if (((JavaScriptObject)prev).ContainsKey(part))
                        {
                            if (i < parts.Length - 1)
                            {
                                bool nextArray = IsNumber(parts[i + 1]);
                                if ((nextArray && ((JavaScriptObject)prev)[part] is JavaScriptArray)
                                    || (!nextArray && ((JavaScriptObject)prev)[part] is JavaScriptObject))
                                {
                                    prev = ((JavaScriptObject)prev)[part];
                                    continue;
                                }
                            }
                            ((JavaScriptObject)prev)[part] = Process(parts, i, value);
                            break;
                        }
                        else
                        {
                            ((JavaScriptObject)prev).AddInternal(part, Process(parts, i, value));
                            break;
                        }
                    }
                }
            }

            return root;
        }

        private static IJavaScriptObject Process(string[] parts, int i, string value)
        {
            IJavaScriptObject obj = new JavaScriptString(value);
            IJavaScriptObject tmp = null;

            for (int j = parts.Length - 1; j > i; j--)
            {
                string part = parts[j];

                if (IsNumber(part))
                {
                    int index = Convert.ToInt32(part);
                    tmp = new JavaScriptArray();
                    for (int m = 0; m < index; m++)
                    {
                        ((JavaScriptArray)tmp).Add(null);
                    }
                    ((JavaScriptArray)tmp).Add(obj);
                    obj = tmp;
                }
                else
                {
                    tmp = new JavaScriptObject();
                    ((JavaScriptObject)tmp).AddInternal(part, obj);
                    obj = tmp;
                }
            }
            return obj;
        }

        private static bool IsNumber(string part)
        {
            if (string.IsNullOrEmpty(part)) return false;
            for (int i = 0; i < part.Length; i++)
            {
                if (!Char.IsDigit(part[i])) return false;
            }
            return true;
        }

        public static object BuildObjectFrom(HttpRequest request, string parameterName, Type parameterType)
        {
            if (request == null || parameterName == null || parameterType == null)
            {
                throw new ArgumentNullException();
            }
            List<string> keys = new List<string>();
            foreach (string key in request.QueryString.Keys)
            {
                if (key == parameterName
                    || key.StartsWith(parameterName + ".")
                    || key.StartsWith(parameterName + "["))
                {
                    keys.Add(key);
                }
            }
            foreach (string key in request.Form.Keys)
            {
                if (key == parameterName
                    || key.StartsWith(parameterName + ".")
                    || key.StartsWith(parameterName + "["))
                {
                    if (!keys.Contains(key)) keys.Add(key);
                }
            }
            //keys.Sort();

            Dictionary<string, string> values = new Dictionary<string, string>();
            foreach (string key in keys)
            {
                values.Add(key, request.Params[key]);
            }

            IJavaScriptObject jso = Process(parameterName, values);
            if (jso == null)
            {
                return null;
            }
            else
            {
                return JavaScriptDeserializer.Deserialize(jso, parameterType);
            }
        }

        static void Main()
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data["user[2][1].name.first"] = "lele";
            data["user[2][1].name.last"] = "ye";
            data["user[1].name"] = "pb";
            data["user[1].age"] = "12";
            data["user[0]"] = "yyy";

            IJavaScriptObject jso = Process("user", data);
            Console.WriteLine(jso.Value);
            //Console.ReadKey();
        }
    }
}
