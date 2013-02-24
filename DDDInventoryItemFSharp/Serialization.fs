module Serialization

open System
open System.IO
open System.Text
open Newtonsoft.Json

module private JsonNet =

    open System.Collections.Generic
    open Newtonsoft.Json.Serialization
    open Newtonsoft.Json.Converters
    open Microsoft.FSharp.Reflection

    type UnionCaseNameConverter() =
        inherit JsonConverter()

        override x.CanConvert(t) = 
            FSharpType.IsUnion(t) || (t.DeclaringType <> null && FSharpType.IsUnion(t.DeclaringType))

        override x.WriteJson(writer, value, serializer) =
            let t = value.GetType()
            let caseInfo,fieldValues = FSharpValue.GetUnionFields(value, t)
            let fieldValue = if fieldValues.Length = 0 then null else fieldValues.[0]
            let map = [caseInfo.Name,fieldValue] |> Map.ofList
            serializer.Serialize(writer, map)

        override x.ReadJson(reader, t, _, serializer) =        
            let map = serializer.Deserialize(reader, typeof<Dictionary<string, obj>>) :?> Dictionary<string, obj>
            let pair = map |> Seq.nth 0
            let args = if pair.Value = null then [||] else [| pair.Value |]
            let case = FSharpType.GetUnionCases(t) |> Seq.find (fun c -> c.Name = pair.Key)
            FSharpValue.MakeUnion(case, args)

    type ListConverter() =
        inherit JsonConverter()
    
        override x.CanConvert(t:Type) = 
            t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<list<_>>

        override x.WriteJson(writer, value, serializer) =
            let list = value :?> System.Collections.IEnumerable |> Seq.cast
            serializer.Serialize(writer, list)

        override x.ReadJson(reader, t, _, serializer) = 
            let itemType = t.GetGenericArguments().[0]
            let collectionType = typedefof<IEnumerable<_>>.MakeGenericType(itemType)
            let collection = serializer.Deserialize(reader, collectionType) :?> System.Collections.IEnumerable |> Seq.cast        
            let listType = typedefof<list<_>>.MakeGenericType(itemType)
            let cases = FSharpType.GetUnionCases(listType)
            let rec make = function
                | [] -> FSharpValue.MakeUnion(cases.[0], [||])
                | head::tail -> FSharpValue.MakeUnion(cases.[1], [| head; (make tail); |])                    
            make (collection |> Seq.toList)

    type OptionConverter() =
        inherit JsonConverter()
    
        override x.CanConvert(t) = 
            t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<option<_>>

        override x.WriteJson(writer, value, serializer) =
            let value = 
                if value = null then null
                else 
                    let _,fields = FSharpValue.GetUnionFields(value, value.GetType())
                    fields.[0]  
            serializer.Serialize(writer, value)

        override x.ReadJson(reader, t, existingValue, serializer) =        
            let innerType = t.GetGenericArguments().[0]
            let innerType = 
                if innerType.IsValueType then typedefof<Nullable<_>>.MakeGenericType([|innerType|])
                else innerType        
            let value = serializer.Deserialize(reader, innerType)
            let cases = FSharpType.GetUnionCases(t)
            if value = null then FSharpValue.MakeUnion(cases.[0], [||])
            else FSharpValue.MakeUnion(cases.[1], [|value|])

    type TupleArrayConverter() =
        inherit JsonConverter()
    
        override x.CanConvert(t:Type) = 
            FSharpType.IsTuple(t)

        override x.WriteJson(writer, value, serializer) =
            let values = FSharpValue.GetTupleFields(value)
            serializer.Serialize(writer, values)

        override x.ReadJson(reader, t, _, serializer) =
            let advance = reader.Read >> ignore
            let deserialize t = serializer.Deserialize(reader, t)
            let itemTypes = FSharpType.GetTupleElements(t)

            let readElements() =
                let rec read index acc =
                    match reader.TokenType with
                    | JsonToken.EndArray -> acc
                    | _ ->
                        let value = deserialize(itemTypes.[index])
                        advance()
                        read (index + 1) (acc @ [value])
                advance()
                read 0 List.empty

            match reader.TokenType with
            | JsonToken.StartArray ->
                let values = readElements()
                FSharpValue.MakeTuple(values |> List.toArray, t)
            | _ -> failwith "invalid token"

    let s = new JsonSerializer()    
    s.Converters.Add(new TupleArrayConverter())
    s.Converters.Add(new OptionConverter())
    s.Converters.Add(new ListConverter())
    s.Converters.Add(new UnionCaseNameConverter())

    let eventType o =
        let t = o.GetType()
        if FSharpType.IsUnion(t) || (t.DeclaringType <> null && FSharpType.IsUnion(t.DeclaringType)) then
            let cases = FSharpType.GetUnionCases(t)
            let unionCase,_ = FSharpValue.GetUnionFields(o, t)
            unionCase.Name
        else t.Name
        
    let serialize o =
        use ms = new MemoryStream()
        use writer = new StreamWriter(ms)
        s.Serialize(writer, o)
        let data = ms.ToArray()
        (eventType o),data

    let deserialize (t, data:byte array) =
        use reader = new StreamReader(new MemoryStream(data), Encoding.UTF8)
        s.Deserialize(reader, t)

let serializer = JsonNet.serialize,JsonNet.deserialize

let deserializet<'T> (data:byte array) =
    let json = Encoding.UTF8.GetString(data)
    JsonConvert.DeserializeObject<'T>(json)
    

