(function (viewModel, $) {
    viewModel.InventoryItemsViewModel = function (items) {
        var self = this;
        self.items = ko.observableArray(items);
        self.addInventoryItem = function () {            
            var item = appFsMvc.utility.serializeObject($("#inventoryItemForm"));
            $.ajax({
                url: "/api/inventoryItems",
                data: JSON.stringify(item),
                type: "POST",
                dataType: "json",
                contentType: "application/json"
            })
            .done(function (r) {
                toastr.success("You have successfully created a new inventory item!", "Success!");
                //self.items.push(item);
                window.location.href = "#/";
            })
            .fail(function () {
                toastr.error("There was an error creating your new inventory item", "<sad face>");
            });
        };
    };
})(appFsMvc.ViewModels = appFsMvc.ViewModels || {}, jQuery);