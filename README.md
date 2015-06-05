# ajaxnet
ajaxnet is a fork of Ajax.NET Professional (AjaxPro) library.

(Homepage: http://www.ajaxpro.info , Original author: Michael Schwarz, The MIT Licence).

## Modifications I made

1) Util methods for adding configurations via codes. (I don't like to put too many lines into the Web.config file.)

2) Rewritten browser-side script file based on the jQuery or ExtJS library. (The jQuery/ExtJS library is always included
at the HTML head today, Why not based on it? )

3) A new HttpHandler for ExtJS's RESTful store. Works more smoothly with ExtJS's grid, combobox, form load & save etc.

4) A few bug(?) fixes. For example, I don't think the behavior of original JSON serializer/deserializer
for Nullable<DateTime> is as same as my expectation.
