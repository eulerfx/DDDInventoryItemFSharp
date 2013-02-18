fromAll().
    when({
        "Created": function (s, e) {
            var inventoryItemName = e.body["Name"];
            //linkTo(
            //emit("ByName-" + e.eventStreamId, "Created", { "name" : inventoryItemName })
        },
        "Deactivated": function (s, e) {

        },
        "Renamed": function (s, e) {
            var newName = parseInt(e.body["Name"]);
        },
        "ItemsCheckedIn": function (s, e) {
            var checkedInCount = parseInt(e.body["Count"]);
        },
        "ItemsRemoved": function (s, e) {
            var removedCount = parseInt(e.body["Count"]);
        }
    });