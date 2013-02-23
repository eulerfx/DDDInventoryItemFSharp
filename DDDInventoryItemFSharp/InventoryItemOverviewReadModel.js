var emitReadModel = function (s, e) {
    emit("InventoryItemOverviewReadModel", e.eventType + "_InventoryItemOverviewReadModel", s);
};
fromCategory('InventoryItem').when(
    {
        $init: function() {
            return {total:0};
        },
        "ItemsCheckedIn": function(s, e) {
            s.total += e.body.Count;
            emitReadModel(s, e);
        },
        "ItemsRemoved": function(s, e) {
            s.total -= e.body.Count;
            emitReadModel(s, e);
        }
    });
