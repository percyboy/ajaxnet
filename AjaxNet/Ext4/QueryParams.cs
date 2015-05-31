using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace AjaxNet.Ext4
{
    public class QueryParams
    {
        public string Id { get; set; }
        public int Start { get; set; }
        public int PageSize { get; set; }
        public int PageIndex { get; set; }
        public ExtSortParam Sort { get; set; }
        public ExtSortParam Group { get; set; }
        public ExtFilterParam Filter { get; set; }
        public HttpContext Context { get; set; }

        public ICollection Results { get; set; }
        public long TotalRecords { get; set; }
        public bool RootDirect { get; set; }
        public object Extra { get; set; }
    }

    public class ExtFilter
    {
        public string property { get; set; }
        public string value { get; set; }
    }

    public class ExtFilterParam
    {
        public ExtFilter[] Filters { get; set; }
    }

    public class ExtSort
    {
        public string property { get; set; }
        public string direction { get; set; }
        public string DbField { get; set; }
    }

    public class ExtSortParam
    {
        public ExtSort[] Sorts { get; set; }

        public void Alias(string webname, string dbname)
        {
            foreach (ExtSort sort in Sorts)
            {
                if (sort.property == webname)
                {
                    sort.DbField = dbname;
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (ExtSort sort in Sorts)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }
                if (string.IsNullOrEmpty(sort.DbField))
                {
                    sb.Append(sort.property);
                }
                else
                {
                    sb.Append(sort.DbField);
                }
                if (sort.direction.ToLower() == "desc")
                {
                    sb.Append(" DESC");
                }
            }
            return sb.ToString();
        }
    }

}
