module Serialization

open System
open System.IO
open System.Text
open Newtonsoft.Json

module private JsonNet =

    open System.Collections.Generic
    open Microsoft.FSharp.Reflection

    type GuidConverter() =
        inherit JsonConverter()

        override x.CanConvert(t:Type) = t = typeof<Guid>

        override x.WriteJson(writer, value, serializer) =
            let value = value :?> Guid
            if value <> Guid.Empty then writer.WriteValue(value.ToString("N"))
            else writer.WriteValue("")
        
        override x.ReadJson(reader, t, _, serializer) = 
            match reader.TokenType with
            | JsonToken.Null -> Guid.Empty :> obj
            | JsonToken.String ->
                let str = reader.Value :?> string
                if (String.IsNullOrEmpty(str)) then Guid.Empty :> obj
                else Guid(str) :> obj
            | _ -> failwith "Invalid token when attempting to read Guid."


    type ListConverter() =
        inherit JsonConverter()
    
        override x.CanConvert(t:Type) = 
            t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<list<_>>

        override x.WriteJson(writer, value, serializer) =
            let list = value :?> Collections.IEnumerable |> Seq.cast
            serializer.Serialize(writer, list)

        override x.ReadJson(reader, t, _, serializer) = 
            let itemType = t.GetGenericArguments().[0]
            let collectionType = typedefof<IEnumerable<_>>.MakeGenericType(itemType)
            let collection = serializer.Deserialize(reader, collectionType) :?> Collections.IEnumerable |> Seq.cast        
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
                if isNull value then null
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
            if isNull value then FSharpValue.MakeUnion(cases.[0], [||])
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


    type UnionCaseNameConverter() =
        inherit JsonConverter()

        override x.CanConvert(t) = 
            FSharpType.IsUnion(t) || (not (isNull t.DeclaringType) && FSharpType.IsUnion(t.DeclaringType))

        override x.WriteJson(writer, value, serializer) =
            let t = value.GetType()
            let caseInfo,fieldValues = FSharpValue.GetUnionFields(value, t)
            writer.WriteStartObject()
            writer.WritePropertyName("case")
            writer.WriteValue(caseInfo.Name)
            writer.WritePropertyName("value")
            let value = 
                match fieldValues.Length with
                | 0 -> null
                | 1 -> fieldValues.[0]
                | _ -> fieldValues :> obj
            serializer.Serialize(writer, value)
            writer.WriteEndObject()

        override x.ReadJson(reader, t, _, serializer) =

            let t = if FSharpType.IsUnion(t) then t else t.DeclaringType                

            let fail() = failwith "Invalid token!"

            let read (t:JsonToken) =
                if reader.TokenType = t then
                    let value = reader.Value
                    reader.Read() |> ignore
                    Some value
                else None

            let require v =
                match v with
                | Some o -> o
                | None -> fail()

            let readProp (n:string) =
                read JsonToken.PropertyName |> Option.map (fun v -> if (v :?> string) <> n then fail())

            read JsonToken.StartObject |> require |> ignore
            readProp "case" |> require |> ignore
            let case = read JsonToken.String |> require :?> string
            readProp "value" |> ignore

            let caseInfo = FSharpType.GetUnionCases(t) |> Seq.find (fun c -> c.Name = case)
            let fields = caseInfo.GetFields()                                                        

            let args = 
                match fields.Length with
                | 0 -> [||]
                | 1 -> 
                    [| serializer.Deserialize(reader, fields.[0].PropertyType) |]
                | _ ->
                    let tupleType = FSharpType.MakeTupleType(fields |> Seq.map (fun f -> f.PropertyType) |> Seq.toArray)
                    let tuple = serializer.Deserialize(reader, tupleType)
                    FSharpValue.GetTupleFields(tuple)
            
            FSharpValue.MakeUnion(caseInfo, args)          
            


//    type UnionExtractingConverter() =
//        inherit JsonConverter()
//
//        override x.CanConvert(t) = 
//            FSharpType.IsUnion(t) || (t.DeclaringType <> null && FSharpType.IsUnion(t.DeclaringType))
//
//        override x.WriteJson(writer, value, serializer) =
//            let t = value.GetType()
//            let _,fieldValues = FSharpValue.GetUnionFields(value, t)
//            let value = 
//                match fieldValues.Length with
//                | 0 -> null
//                | 1 -> fieldValues.[0]
//                | _ -> fieldValues :> obj
//            serializer.Serialize(writer, value)
//
//        override x.ReadJson(reader, t, _, serializer) =   
//            serializer.Deserialize(reader, t)
    
    let s = JsonSerializer()    
    s.Converters.Add(GuidConverter())
    s.Converters.Add(TupleArrayConverter())
    s.Converters.Add(OptionConverter())
    s.Converters.Add(ListConverter())
    //s.Converters.Add(new UnionExtractingConverter())
    s.Converters.Add(UnionCaseNameConverter())
    
    let eventType o =
        let t = o.GetType()
        if FSharpType.IsUnion(t) || (not (isNull t.DeclaringType) && FSharpType.IsUnion(t.DeclaringType)) then
            let cases = FSharpType.GetUnionCases(t)
            let unionCase,_ = FSharpValue.GetUnionFields(o, t)
            unionCase.Name
        else t.Name
        
    let serialize o =
        use ms = new MemoryStream()
        (use jsonWriter = new JsonTextWriter(new StreamWriter(ms))
        s.Serialize(jsonWriter, o))
        let data = ms.ToArray()
        (eventType o),data

    let deserialize (t, et:string, data:byte array) =
        use ms = new MemoryStream(data)
        use jsonReader = new JsonTextReader(new StreamReader(ms))
        s.Deserialize(jsonReader, t)

//    let deserialize (t, et:string, data:byte array) =
//        let case = FSharpType.GetUnionCases(t) |> Seq.find (fun c -> c.Name = et)
//        let fields = case.GetFields()
//        let t' =         
//            match fields.Length with
//            | 0 -> null
//            | 1 -> fields.[0].PropertyType
//            | _ -> FSharpType.MakeTupleType(fields |> Seq.map (fun f -> f.PropertyType) |> Seq.toArray)
//        if t' <> null then
//            use reader = new StreamReader(new MemoryStream(data), Encoding.UTF8)
//            let body = s.Deserialize(reader, t')
//            let args = 
//                if FSharpType.IsTuple(t') then FSharpValue.GetTupleFields(body)
//                elif body <> null then [|body|]
//                else [||]
//            FSharpValue.MakeUnion(case, args)
//        else null

let serializer = JsonNet.serialize,JsonNet.deserialize

let deserializet<'T> (data:byte array) =
    let json = Encoding.UTF8.GetString(data)
    JsonConvert.DeserializeObject<'T>(json)
    

