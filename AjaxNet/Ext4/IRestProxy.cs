using System;
using System.Collections.Generic;
using System.Text;

namespace AjaxNet.Ext4
{
    public interface IAjaxProxy
    {
        void Query(QueryParams paras);
    }

    public interface IRestProxy : IAjaxProxy
    {
        Type EntityType { get; }

        object Create(object obj);
        object Update(object obj);
        void Delete(object obj);
    }
}
