using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Web.SessionState;

namespace AjaxNet.Ext4
{
    public class RestProxyHandler : IHttpHandler, IRequiresSessionState
    {
        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            Type t = null;

            #region prepare the IAjaxProxy/IRestProxy type
            string className = null;
            string idPart = null;
            string[] parts = context.Request.Path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].ToLower() == "ext4")
                {
                    if (i + 1 < parts.Length)
                    {
                        className = parts[i + 1];
                        if (i + 2 < parts.Length)
                        {
                            idPart = parts[i + 2];
                        }
                    }
                    break;
                }
            }
            if (className == null)
            {
                ResponseError(context, new Exception("Type not set."));
                return;
            }

            if (className.EndsWith(".ashx"))
            {
                className = className.Substring(0, className.Length - 5);
            }
            if (idPart != null && idPart.EndsWith(".ashx"))
            {
                idPart = idPart.Substring(0, idPart.Length - 5);
            }

            if (Utility.Settings != null && Utility.Settings.UrlClassMappings.ContainsKey(className))
            {
                className = Utility.Settings.UrlClassMappings[className] as string;
                if (context.Trace.IsEnabled) context.Trace.Write(Constant.AjaxID, "Url match to Type: " + className);
            }

            try
            {
                t = Type.GetType(className, true);
            }
            catch (Exception ex)
            {
                if (context.Trace.IsEnabled) context.Trace.Write(Constant.AjaxID, "Type not found: " + className);
                ResponseError(context, ex);
                return;
            }
            #endregion

            #region GET via IAjaxProxy
            if (context.Request.HttpMethod == "GET")
            {
                if (!typeof(IAjaxProxy).IsAssignableFrom(t))
                {
                    ResponseError(context, new Exception("The type is not a IAjaxProxy: " + className));
                    return;
                }

                try
                {
                    IAjaxProxy ajax = (IAjaxProxy)Activator.CreateInstance(t, new object[] { });

                    QueryParams paras = new QueryParams();
                    if (!string.IsNullOrEmpty(context.Request.Params["page"]))
                    {
                        paras.PageIndex = Convert.ToInt32(context.Request.Params["page"]) - 1;
                    }
                    if (!string.IsNullOrEmpty(context.Request.Params["start"]))
                    {
                        paras.Start = Convert.ToInt32(context.Request.Params["start"]);
                    }
                    if (!string.IsNullOrEmpty(context.Request.Params["limit"]))
                    {
                        paras.PageSize = Convert.ToInt32(context.Request.Params["limit"]);
                    }
                    paras.Id = context.Request.Params["id"];

                    if (!string.IsNullOrEmpty(context.Request.Params["sort"]))
                    {
                        ExtSortParam para = new ExtSortParam();
                        para.Sorts = (ExtSort[])JavaScriptDeserializer.DeserializeFromJson(
                            context.Request.Params["sort"],
                            typeof(ExtSort[]));
                        paras.Sort = para;
                    }
                    if (!string.IsNullOrEmpty(context.Request.Params["group"]))
                    {
                        ExtSortParam para = new ExtSortParam();
                        para.Sorts = (ExtSort[])JavaScriptDeserializer.DeserializeFromJson(
                            context.Request.Params["group"],
                            typeof(ExtSort[]));
                        paras.Group = para;
                    }
                    if (!string.IsNullOrEmpty(context.Request.Params["filter"]))
                    {
                        ExtFilterParam para = new ExtFilterParam();
                        para.Filters = (ExtFilter[])JavaScriptDeserializer.DeserializeFromJson(
                            context.Request.Params["filter"],
                            typeof(ExtFilter[]));
                        paras.Filter = para;
                    }
                    paras.Context = context;

                    ajax.Query(paras);

                    if (paras.RootDirect)
                    {
                        ResponseObject(context, paras.Results);
                    }
                    else
                    {
                        if (paras.Extra == null)
                        {
                            ResponseObject(context, new
                            {
                                success = true,
                                total = paras.TotalRecords,
                                root = paras.Results
                            });
                        }
                        else
                        {
                            ResponseObject(context, new
                            {
                                success = true,
                                total = paras.TotalRecords,
                                root = paras.Results,
                                extra = paras.Extra
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    ResponseError(context, ex);
                    return;
                }
            }
            #endregion

            #region POST/PUT/DELETE via IRestProxy
            if (context.Request.HttpMethod == "POST"
                || context.Request.HttpMethod == "PUT"
                || context.Request.HttpMethod == "DELETE")
            {
                if (!typeof(IRestProxy).IsAssignableFrom(t))
                {
                    ResponseError(context, new Exception("The type is not a IRestProxy: " + className));
                    return;
                }

                try
                {
                    IRestProxy rest = (IRestProxy)Activator.CreateInstance(t, new object[] { });

                    string payload = ReadRequestPayload(context);
                    object entity = JavaScriptDeserializer.DeserializeFromJson(payload, rest.EntityType);

                    switch (context.Request.HttpMethod)
                    {
                        case "POST": //new
                            object obj1 = rest.Create(entity);
                            ResponseObject(context, new
                            {
                                success = true,
                                root = new object[] { obj1 }
                            });
                            break;
                        case "PUT": //update
                            object obj2 = rest.Update(entity);
                            ResponseObject(context, new
                            {
                                success = true,
                                root = new object[] { obj2 }
                            });
                            break;
                        case "DELETE":
                            rest.Delete(entity);
                            ResponseObject(context, new
                            {
                                success = true
                            });
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ResponseError(context, ex);
                    return;
                }
            }
            #endregion
        }

        private void ResponseObject(HttpContext context, object obj)
        {
            context.Response.Write(JavaScriptSerializer.Serialize(obj));
        }

        private void ResponseError(HttpContext context, Exception ex)
        {
            ResponseObject(context, new
            {
                success = false,
                message = ex.Message,
                error = ex
            });
            //context.Response.StatusCode = 500;
            context.Response.End();
        }

        private string ReadRequestPayload(HttpContext context)
        {
            context.Request.ContentEncoding = System.Text.UTF8Encoding.UTF8;
            byte[] b = new byte[context.Request.InputStream.Length];

            if (context.Request.InputStream.Read(b, 0, b.Length) == 0)
                return null;

            StreamReader sr = new StreamReader(new MemoryStream(b), System.Text.UTF8Encoding.UTF8);

            string v = null;
            try
            {
                v = sr.ReadToEnd();
            }
            finally
            {
                sr.Close();
            }
            return v;
        }
    }
}
