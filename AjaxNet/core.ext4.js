﻿var AjaxNet={invoke:function(c,f,b,e){var d={__r:{error:null,value:null,duration:0},url:c,method:"POST",headers:{"X-AjaxNet-Method":f,"Content-Type":"text/plain"},failure:function(g,h){h.__r.duration=new Date().getTime()-h.__r.duration;if(g.aborted){return}h.__r.error={Message:"Unknown",Type:"ConnectFailure"};if(g.timedout){h.__r.error.Message="Connection Timed Out"}else{h.__r.error.Message=g.statusText}if(typeof(e)=="function"){e(h.__r)}},success:function(h,i){i.__r.duration=new Date().getTime()-i.__r.duration;var g=h.responseText;if(AjaxNet.cryptProvider&&typeof(AjaxNet.cryptProvider.decrypt)=="function"){g=AjaxNet.cryptProvider.decrypt(g)}g=JSON.parse(g,function(l,m){var j=m;for(var k=0;k<AjaxNet.jsonConverters.length;k++){var n=AjaxNet.jsonConverters[k];j=n.fromJSON(this[l],m);if(j!=m){break}}return j});if(g!=null){if(typeof(g.value)!="undefined"){i.__r.value=g.value}else{if(typeof(g.error)!="undefined"){i.__r.error=g.error}}}if(typeof(e)=="function"){e(i.__r)}}};d.__r.duration=new Date().getTime();d.jsonData=JSON.stringify(b,function(j,k){var g=k;for(var h=0;h<AjaxNet.jsonConverters.length;h++){var l=AjaxNet.jsonConverters[h];g=l.toJSON(this[j],k);if(g!=k){break}}return g})+"";if(AjaxNet.cryptProvider&&typeof(AjaxNet.cryptProvider.encrypt)=="function"){d.data=AjaxNet.cryptProvider.encrypt(d.data)}if(AjaxNet.token&&AjaxNet.token.length>0){d.headers["X-AjaxNet-Token"]=AjaxNet.token}if(typeof(e)=="function"){Ext.Ajax.request(d)}else{d.async=false;var a=Ext.Ajax.request(d).responseText;d.success({responseText:a},d)}return d.__r},getCallback:function(a,b){if(b<a.length){return a[b]}return undefined},ensureNamespace:function(ns){if(typeof(ns)!="string"||ns.length==0){return}var pieces=ns.split(".");var n="";for(var i=0;i<pieces.length;i++){if(i==0){n=pieces[i]}else{n=n+"."+pieces[i]}eval("if (typeof("+n+') == "undefined") '+n+" = {};")}},jsonConverters:[],Hashtable:function(a){if(typeof(a)=="object"){this.data=a}else{this.data={keys:[],values:[],__type:"System.Collections.Hashtable"}}this.toJSON=function(b){return this.data};this.count=function(){return this.data.keys.length};this.containsKey=function(c){for(var b=0;b<this.data.keys.length;b++){if(this.data.keys[b]==c){return true}}return false};this.containsValue=function(c){for(var b=0;b<this.data.values.length;b++){if(this.data.values[b]==c){return true}}return false};this.get=function(c){for(var b=0;b<this.data.keys.length;b++){if(this.data.keys[b]==c){return this.data.values[b]}}return null};this.getAt=function(b){if(b>-1&&b<this.data.keys.length){return this.data.values[b]}return null};this.clear=function(){this.data.keys=[];this.data.values=[]};this.add=function(b,c){this.data.keys.push(b);this.data.values.push(c)};this.remove=function(c){var d=-1;for(var b=0;b<this.data.keys.length;b++){if(this.data.keys[b]==c){d=b}}if(d!=-1){this.data.keys.splice(d,1);this.data.values.splice(d,1)}};this.removeAt=function(b){if(b>-1&&b<this.data.keys.length){this.data.keys.splice(b,1);this.data.values.splice(b,1)}};this.toObject=function(){var c={};for(var b=0;b<this.data.keys.length;b++){c[this.data.keys[b]]=this.data.values[b]}return c}},DataSet:function(a){if(typeof(a)=="object"&&a.__type=="System.Data.DataSet,System.Data"){this.data=a}else{this.data={__type:"System.Data.DataSet,System.Data",tables:[]}}this.toJSON=function(b){return this.data};this.Tables=function(b){for(var c=0;c<this.data.tables.length;c++){if(this.data.tables[c].Name==b){return this.data.tables[c]}}return null}},DataTable:function(a){if(typeof(a)=="object"){this.Name=a.name;this.data=a}else{this.Name="";this.data={__type:"System.Data.DataTable,System.Data",name:"",columns:[],columnTypes:[],rows:[]}}this.toJSON=function(b){this.data.name=this.Name;return this.data};this.rowCount=function(){return this.data.rows.length};this.row=function(b){if(b>-1&&b<this.data.rows.length){return new AjaxNet.DataRow(this,this.data.rows[b])}};this.newRow=function(){var c=[];for(var b=0;b<this.data.columns.length;b++){c.push(undefined)}return new AjaxNet.DataRow(this,c)};this.add=function(b){this.data.rows.push(b.data)}},DataRow:function(a,b){if(Ext.isArray(b)){this.data=b}else{this.data=[]}this.get=function(c){for(var d=0;d<a.data.columns.length;d++){if(a.data.columns[d]==c){return this.data[d]}}return undefined};this.set=function(c,e){for(var d=0;d<a.data.columns.length;d++){if(a.data.columns[d]==c){this.data[d]=e;break}}}}};AjaxNet.jsonConverters.push({fromJSON:function(d,a){var c=/^\/Date\(-?\d+\)\/$/;if(typeof(d)=="string"&&d.indexOf("/Date(")==0&&c.test(d)){var b=d.substr(6,d.indexOf("/",6)-7);return new Date(parseInt(b,10))}if(typeof(d)=="object"&&d!=null&&Ext.isArray(d.keys)&&Ext.isArray(d.values)){return new AjaxNet.Hashtable(d)}if(typeof(d)=="object"&&d!=null&&d.__type=="System.Data.DataSet,System.Data"){return new AjaxNet.DataSet(d)}if(typeof(d)=="object"&&d!=null&&d.__type=="System.Data.DataTable,System.Data"){return new AjaxNet.DataTable(d)}return a},toJSON:function(b,a){if(b instanceof Date){return"/Date("+b.getTime()+")/"}return a}});var JSON;if(!JSON){JSON={}}(function(){function f(n){return n<10?"0"+n:n}if(typeof Date.prototype.toJSON!=="function"){Date.prototype.toJSON=function(key){return isFinite(this.valueOf())?this.getUTCFullYear()+"-"+f(this.getUTCMonth()+1)+"-"+f(this.getUTCDate())+"T"+f(this.getUTCHours())+":"+f(this.getUTCMinutes())+":"+f(this.getUTCSeconds())+"Z":null};String.prototype.toJSON=Number.prototype.toJSON=Boolean.prototype.toJSON=function(key){return this.valueOf()}}var cx=/[\u0000\u00ad\u0600-\u0604\u070f\u17b4\u17b5\u200c-\u200f\u2028-\u202f\u2060-\u206f\ufeff\ufff0-\uffff]/g,escapable=/[\\\"\x00-\x1f\x7f-\x9f\u00ad\u0600-\u0604\u070f\u17b4\u17b5\u200c-\u200f\u2028-\u202f\u2060-\u206f\ufeff\ufff0-\uffff]/g,gap,indent,meta={"\b":"\\b","\t":"\\t","\n":"\\n","\f":"\\f","\r":"\\r",'"':'\\"',"\\":"\\\\"},rep;function quote(string){escapable.lastIndex=0;return escapable.test(string)?'"'+string.replace(escapable,function(a){var c=meta[a];return typeof c==="string"?c:"\\u"+("0000"+a.charCodeAt(0).toString(16)).slice(-4)})+'"':'"'+string+'"'}function str(key,holder){var i,k,v,length,mind=gap,partial,value=holder[key];if(value&&typeof value==="object"&&typeof value.toJSON==="function"){value=value.toJSON(key)}if(typeof rep==="function"){value=rep.call(holder,key,value)}switch(typeof value){case"string":return quote(value);case"number":return isFinite(value)?String(value):"null";case"boolean":case"null":return String(value);case"object":if(!value){return"null"}gap+=indent;partial=[];if(Object.prototype.toString.apply(value)==="[object Array]"){length=value.length;for(i=0;i<length;i+=1){partial[i]=str(i,value)||"null"}v=partial.length===0?"[]":gap?"[\n"+gap+partial.join(",\n"+gap)+"\n"+mind+"]":"["+partial.join(",")+"]";gap=mind;return v}if(rep&&typeof rep==="object"){length=rep.length;for(i=0;i<length;i+=1){if(typeof rep[i]==="string"){k=rep[i];v=str(k,value);if(v){partial.push(quote(k)+(gap?": ":":")+v)}}}}else{for(k in value){if(Object.prototype.hasOwnProperty.call(value,k)){v=str(k,value);if(v){partial.push(quote(k)+(gap?": ":":")+v)}}}}v=partial.length===0?"{}":gap?"{\n"+gap+partial.join(",\n"+gap)+"\n"+mind+"}":"{"+partial.join(",")+"}";gap=mind;return v}}if(typeof JSON.stringify!=="function"){JSON.stringify=function(value,replacer,space){var i;gap="";indent="";if(typeof space==="number"){for(i=0;i<space;i+=1){indent+=" "}}else{if(typeof space==="string"){indent=space}}rep=replacer;if(replacer&&typeof replacer!=="function"&&(typeof replacer!=="object"||typeof replacer.length!=="number")){throw new Error("JSON.stringify")}return str("",{"":value})}}if(typeof JSON.parse!=="function"){JSON.parse=function(text,reviver){var j;function walk(holder,key){var k,v,value=holder[key];if(value&&typeof value==="object"){for(k in value){if(Object.prototype.hasOwnProperty.call(value,k)){v=walk(value,k);if(v!==undefined){value[k]=v}else{delete value[k]}}}}return reviver.call(holder,key,value)}text=String(text);cx.lastIndex=0;if(cx.test(text)){text=text.replace(cx,function(a){return"\\u"+("0000"+a.charCodeAt(0).toString(16)).slice(-4)})}if(/^[\],:{}\s]*$/.test(text.replace(/\\(?:["\\\/bfnrt]|u[0-9a-fA-F]{4})/g,"@").replace(/"[^"\\\n\r]*"|true|false|null|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?/g,"]").replace(/(?:^|:|,)(?:\s*\[)+/g,""))){j=eval("("+text+")");return typeof reviver==="function"?walk({"":j},""):j}throw new SyntaxError("JSON.parse")}}}());