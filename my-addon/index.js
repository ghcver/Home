var contextMenu = require("sdk/context-menu");
var tabs = require("sdk/tabs");
let { Bookmark, Group, save, search, UNSORTED } = require("sdk/places/bookmarks");

contextMenu.Item({
    label: "Save This Tab",
    context: contextMenu.PageContext(),
    contentScript: 'self.on("click", function () {' +
                   '  self.postMessage();' +
                   '});',
    onMessage: function () {
        search({ group: UNSORTED }).on("end", function (results) {
			var bookedUrlList = [];
			for (let result of results)
			{
				bookedUrlList.push(result.url);
			}
			
			var dateTime = new Date().toISOString().replace(/T/, ' ').replace(/\..+/, '');
            var tab = tabs.activeTab;
			if(bookedUrlList.indexOf(tab.url) == -1)
			{
				save(Bookmark({ title: tab.title, url: tab.url, tags:[dateTime], group: UNSORTED }));
			}
        });
    }
});

contextMenu.Item({
    label: "Save All Tabs",
    context: contextMenu.PageContext(),
    contentScript: 'self.on("click", function () {' +
                   '  self.postMessage();' +
                   '});',
    onMessage: function () {
		search({ group: UNSORTED }).on("end", function (results) {
			var bookedUrlList = [];
			for (let result of results)
			{
				bookedUrlList.push(result.url);
			}
			
			var dateTime = new Date().toISOString().replace(/T/, ' ').replace(/\..+/, '');
			var urlList = [];
			for (let tab of tabs)
			{
				if(urlList.indexOf(tab.url) == -1)
				{
					urlList.push(tab.url);
					if(bookedUrlList.indexOf(tab.url) == -1)
			        {
				        save(Bookmark({ title: tab.title, url: tab.url, tags:[dateTime], group: UNSORTED }));
			        }
				}
			}
        });
    }
});