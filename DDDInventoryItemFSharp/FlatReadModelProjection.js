var emitReadModel = function (s, e) {
    var streamId = "InventoryItemFlatReadModel-" + e.streamId.replace("InventoryItem-", "");
    var eventType = e.eventType + "_InventoryItemFlatReadModel";
    emit(streamId, eventType, s);
};
fromCategory('InventoryItem').foreachStream().when({
    $init: function() {
        return {
            name: null,
            count: 0,
            active: false,
            countChanges: 0
        };
    },
    "Created": function (s, e) {
        s.name = e.body.Name;
        s.active = true;
        emitReadModel(s, e);
    },
    "Deactivated": function (s, e) {
        s.active = false;
        emitReadModel(s, e);
    },
    "Renamed": function (s, e) {
        s.name = e.body.Name;
        emitReadModel(s, e);
    },
    "ItemsCheckedIn": function (s, e) {
        s.count += e.body.Count;
        s.countChanges += 1;
        emitReadModel(s, e);
    },
    "ItemsRemoved": function (s, e) {
        s.count -= e.body.Count;
        s.countChanges += 1;
        emitReadModel(s, e);
    }
});