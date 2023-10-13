// Copyright (c) Faizan Yousaf, Tobias Bonnesen. All Rights Reserved.

namespace Shared

open Newtonsoft.Json.Serialization

module JsonUtils =
    open System
    open Newtonsoft.Json
    open Newtonsoft.Json.Linq
    open Newtonsoft.Json.Schema
    open Newtonsoft.Json.Schema.Generation
    open Newtonsoft.Json.Serialization
    open System.IO
    
    let parseJsonSchema (filePath: string) : JSchema =
        let schemaContent: string = System.IO.File.ReadAllText(filePath)
        JSchema.Parse(schemaContent)

    let parseJson (filePath: string) : JObject =
        let json: string = System.IO.File.ReadAllText(filePath)
        JObject.Parse(json)

    let checkJsonAdheresToSchema (schema: JSchema) (jObject: JObject) : Result<JObject, string> =
        try
            jObject.Validate(schema)
            Ok jObject
        with :? JSchemaValidationException as (ex: JSchemaValidationException) ->
            let e: ValidationError = ex.ValidationError
            raise ex

    let generateJsonSchemaFromType (t: Type) : string =
        let jSchemaGenerator: JSchemaGenerator = new JSchemaGenerator()
        let jSchema: JSchema = jSchemaGenerator.Generate(t)
        jSchema.ToString()

    let exportAsJson (path: string) (obj: obj) : unit =
        let serializerSettings = new JsonSerializerSettings();
        serializerSettings.ContractResolver <- new CamelCasePropertyNamesContractResolver();
        let json: string = JsonConvert.SerializeObject(obj, serializerSettings)
        Directory.CreateDirectory(Path.GetDirectoryName(path)) |> ignore
        System.IO.File.WriteAllText(path, json)
    