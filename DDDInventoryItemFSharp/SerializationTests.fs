module SerializationTests

let serialize',deserialize = Serialization.serializer

let serialize o = 
    let et,bytes = serialize' o   
    let json = System.Text.Encoding.UTF8.GetString(bytes)
    printfn "EventType=%A;Json=%A" et json
    let o' = deserialize(o.GetType(), et, System.Text.Encoding.UTF8.GetBytes(json))
    printfn "Deserialized=%A" o'
    et,json

let test (eet:string) (ejson:string) o = 
    let et,json = serialize(o)   
    assert(eet = et)
    assert(ejson = json)

type ComplexType = {
    links : int list;
    limit : bool option;
    name : string;
    birth : System.DateTime;
} 
and ComplexEvent =
| Created of ComplexType
| Destroyed of System.Guid * string
| Renamed of string
| Limited

[<Xunit.Fact>]
let complexTypeShouldSerialize() =
    
    let v = {
        ComplexType.name = "e to the i";
        links = [1;2;3];
        limit = Some(false);        
        birth = System.DateTime(1985, 10, 9);
    }
    
    serialize (Created v) |> ignore
    serialize (Destroyed (System.Guid.NewGuid(),"see ya")) |> ignore
    ()


[<Xunit.Fact>]
let eventsShouldSerialize() =

    test "Created" """{"case":"Created","value":"hello item"}""" (InventoryItem.Created("hello item"))
    test "Deactivated" """{"case":"Deactivated","value":null}""" (InventoryItem.Deactivated)
    test "Renamed" """{"case":"Renamed","value":"new name"}""" (InventoryItem.Renamed("new name"))
    test "ItemsCheckedIn" """{"case":"ItemsCheckedIn","value":100}""" (InventoryItem.ItemsCheckedIn(100))
    test "ItemsRemoved" """{"case":"ItemsRemoved","value":200}""" (InventoryItem.ItemsRemoved(200))

    ()