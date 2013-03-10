(function ($) {
    appFsMvc.App = function(inventoryItemsViewModel) {
        return $.sammy("#content", function () {
            var self = this;

            this.use(Sammy.Cache);
            this.inventoryItemsViewModel = inventoryItemsViewModel;

            this.renderTemplate = function (html) {
                self.$element().html(html);
                ko.applyBindings(self.inventoryItemsViewModel);
            };            

            this.get("#/", function () {
                this.render("/Templates/inventoryItemOverview.htm", {}, self.renderTemplate);
            });

            this.get("#/create", function () {
                this.render("/Templates/inventoryItemCreate.htm", {}, function (html) {
                    self.renderTemplate(html);
                });
            });
        });
    };

    $(function () {
        $.getJSON("/api/inventoryItems", function (item) {
            var viewModel = new appFsMvc.ViewModels.InventoryItemsViewModel([item]);
            appFsMvc.App(viewModel).run("#/");
        });
    });
})(jQuery);

