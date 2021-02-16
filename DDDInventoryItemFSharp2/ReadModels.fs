module ReadModels

[<CLIMutable>]
type InventoryItemFlatReadModel = 
    { name : string
      count : int
      active : bool }

[<CLIMutable>]
type InventoryItemOverviewReadModel = 
    { total : int }