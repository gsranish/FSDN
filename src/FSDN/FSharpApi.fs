﻿namespace FSDN

open System.Runtime.Serialization
open FSharpApiSearch

[<DataContract>]
type FSharpApi = {
  [<field: DataMember(Name = "name")>]
  Name: string
  [<field: DataMember(Name = "kind")>]
  Kind: string
  [<field: DataMember(Name = "signature")>]
  Signature: string
}

[<DataContract>]
type SearchResult = {
  [<field: DataMember(Name = "distance")>]
  Distance: int
  [<field: DataMember(Name = "api")>]
  Api: FSharpApi
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FSharpApi =

  let toSerializable (results: FSharpApiSearch.Result seq) =
    {
      Values =
        results
        |> Seq.map (fun result ->
          {
            Distance = result.Distance
            Api =
              {
                Name = ReverseName.toString result.Api.Name
                Kind = result.Api.Kind.Print()
                Signature = result.Api.Signature.Print()
              }
          })
        |> Seq.toArray
    }

  let trySearch (client: FSharpApiSearchClient) opts (query: string) =
    try
      client.Search(query, opts)
      |> Seq.filter (fun x -> x.Distance < 3)
      |> Choice1Of2
    with e -> Choice2Of2 e

  module OptionStatus =

    let tryParse = function
    | "enabled" -> Some Enabled
    | "disabled" -> Some Disabled
    | _ -> None

    let parseOrDefault defaultValue value =
      match tryParse value with
      | Some value -> value
      | None -> defaultValue

  module SearchOptions =

    open Suave

    [<Literal>]
    let Strict = "strict"

    [<Literal>]
    let Similarity = "similarity"

    [<Literal>]
    let IgnoreArgStyle = "ignore_arg_style"

    let parse (req: HttpRequest) =
      let update name value opt =
        match name with
        | Strict -> { opt with StrictQueryVariable = OptionStatus.parseOrDefault Enabled value }
        | Similarity -> { opt with SimilaritySearching = OptionStatus.parseOrDefault Disabled value }
        | IgnoreArgStyle -> { opt with IgnoreArgumentStyle = OptionStatus.parseOrDefault Enabled value }
        | _ -> opt
      [Strict; Similarity; IgnoreArgStyle]
      |> List.fold (fun opt name ->
        match req.queryParam name with
        | Choice1Of2 value -> update name value opt
        | Choice2Of2 _ -> opt) SearchOptions.defaultOptions