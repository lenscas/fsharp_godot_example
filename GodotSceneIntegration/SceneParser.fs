namespace SceneParser

module SceneParser =
    open System.IO
    open System
    open System.Text
    open System.Collections

    let x = 1

    type Chunk<'a> = { Head: 'a; Rest: seq<'a> }

    let makeChunks a b =
        seq {
            let mutable chunk = ResizeArray()
            let mutable head = None

            for c in b do
                if a c then
                    match head with
                    | Some x -> chunk.Add(c)
                    | None -> head <- Some c
                else
                    let oldHead = head
                    let oldChunk = chunk
                    chunk <- ResizeArray()
                    head <- Some c

                    match oldHead with
                    | Some x -> yield { Head = x; Rest = oldChunk }
                    | None -> ()

            match head with
            | Some x -> yield { Head = x; Rest = chunk }
            | None -> ()
        }

    let filterRest b a =
        { Head = a.Head
          Rest = a.Rest |> Seq.filter b }


    type ResPathAndFullPath = { resPath: string; fullPath: string }

    type LineKeys =
        | Id of int
        | Path of string
        | Other of string

    type NodeDef =
        { externalScript: Option<string>
          parent: Option<string>
          name: string
          baseType: string
          instance: Option<string> }

    type SceneFile =
        { dependencies: System.Collections.Generic.IDictionary<int, string>
          nodes: List<NodeDef>
          fileName: string }

    let makeLogger () =
        let path = @"./log.txt"

        if File.Exists path |> not then
            // Create a file to write to.
            File.CreateText path
        else
            File.AppendText(path)

    let encode (a: string) (b: string) =
        a.Replace("!", "!!") + "!" + b.Replace("!", "!!")


    let decode (a: string) =
        let logger = makeLogger ()
        logger.WriteLine("decoding " + a)
        let mutable first = ""
        let mutable nextPart = None
        let mutable foundSplit = false

        for (index, char) in a |> Seq.indexed do
            logger.WriteLine(
                "character = "
                + char.ToString()
                + " index = "
                + index.ToString()
                + " firstPart = "
                + first
                + " size: "
                + a.Length.ToString()
            )

            logger.Flush()

            match (char, foundSplit) with
            | '!', false -> foundSplit <- true
            | '!', true ->
                first <- first + "!"
                foundSplit <- false
            | x, true when nextPart.IsNone ->
                logger.WriteLine("found split")
                logger.Flush()
                nextPart <- a.Substring(index).Replace("!!", "!") |> Some
            | x, false -> first <- first + x.ToString()
            | x, true -> ()

        logger.WriteLine("Done parsing. First = " + first + " second = " + nextPart.ToString())
        logger.Flush()
        first, nextPart

    type Started =
        | No
        | Quote
        | Bracket

    let getValueFromExternalSource (line: String) (logger: StreamWriter) =
        let parts =
            line.Replace("[ext_resource ", "").Split(" ", StringSplitOptions.None)
            |> Seq.map (fun x -> x.Trim())
            |> Seq.map (fun x ->
                logger.WriteLine("time to split: " + x)
                let z = x.Split("=")
                let splitted = (z[0], z[1])
                logger.WriteLine("done splitting")
                splitted)

            |> Seq.filter (fun (param, _) -> param = "id" || param = "path")
            |> Seq.map (fun (param, value) ->
                if value.EndsWith("]") then
                    (param, value.Remove(value.Length - 1))
                else
                    (param, value))
            |> Seq.map (fun (param, value) ->
                logger.WriteLine("parsing param = " + param + " with value = `" + value + "`")

                if param = "id" then
                    value |> int |> Id
                else
                    value.Replace("\"", "")
                    |> Path.GetFileName
                    |> (fun x ->
                        let extension: string = Path.GetExtension x
                        let withoutEx = Path.GetFileNameWithoutExtension x

                        if extension = ".cs" then
                            (withoutEx + "Fs") |> Path
                        else if extension = ".tscn" then
                            value.Replace("\"", "") |> Path
                        else
                            Other value))
            |> Seq.filter (function
                | Other _ -> false
                | _ -> true)
            |> Seq.toList

        let id =
            parts
            |> Seq.pick (function
                | Id x -> Some x
                | _ -> None)

        let typeName =
            parts
            |> Seq.pick (function
                | Path x -> Some x
                | _ -> None)

        (typeName, id)

    let unwrapExtResource (a: string) =
        Console.WriteLine("Unwrapping extResource from: " + a)

        let res =
            a
                .Replace("script = ", "")
                .Replace("ExtResource", "")
                .Replace("( ", "")
                .Replace(" )", "")
                .Trim()

        Console.WriteLine("stripped it down to `" + res + "`. Time to turn it into an integer")
        res |> int

    let parseScene
        (logger: StreamWriter)
        (projectFilePath: String)
        ((fileName, contents): string * seq<string>)
        : SceneFile =
        logger.WriteLine "start parsing scene"

        let externalNodes = Generic.Dictionary<int, string>()

        let dependencies = Generic.Dictionary<int, string>()

        let contents =
            contents
            |> Seq.filter (fun line ->
                logger.WriteLine line
                logger.WriteLine("working on line =  " + line)

                if line.StartsWith("[node") || line.StartsWith("script") then
                    true
                elif line.StartsWith("[ext_resource") && line.Contains("type=\"PackedScene\"") then
                    let (typeName: string, id) = getValueFromExternalSource line logger
                    logger.WriteLine("adding to dependencies " + id.ToString() + " " + typeName)
                    dependencies.Add(id, typeName)

                    false
                elif line.StartsWith("[ext_resource") && line.Contains("type=\"Script\"") then
                    let (typeName: string, id) = getValueFromExternalSource line logger
                    logger.WriteLine("adding to externalNodes " + id.ToString() + " " + typeName)
                    externalNodes.Add(id, typeName)
                    false

                else
                    false)
            |> Seq.toList


        let splitHeadIntoValues (head: string) =
            seq {
                let mutable partSoFar = StringBuilder()
                let mutable started = Started.No

                for char in head do
                    match started, char with
                    | (No, x) when x = ' ' ->
                        let oldPart = partSoFar
                        partSoFar <- StringBuilder()
                        yield oldPart.ToString()
                    | No, x when x = '"' -> started <- Started.Quote
                    | (No, x) when x = '(' -> started <- Started.Bracket
                    | (Quote, x) when x = '"' -> started <- Started.No
                    | (Bracket, x) when x = ')' -> started <- Started.No
                    | (_, x) -> x |> partSoFar.Append |> ignore

                yield partSoFar.ToString()
            }

        let collectNodesAndScripts (content: seq<string>) =
            content
            |> makeChunks (fun x -> x.StartsWith("[node") |> not)
            |> Seq.map (fun chunk ->
                logger.WriteLine("chunk Head " + chunk.Head)

                logger.WriteLine("rest is " + (chunk.Rest |> Seq.length |> (fun x -> x.ToString())))

                let splits =
                    chunk.Head
                    |> splitHeadIntoValues
                    |> Seq.filter (fun x -> x <> "[node")
                    |> Seq.map (fun x ->
                        logger.WriteLine("parsing header part : " + x)

                        let z =
                            "="
                            |> x.Split
                            |> Seq.map (fun x -> x.Replace("=", "").Replace("\"", "").Replace("]", ""))
                            |> Seq.toList

                        logger.WriteLine("parsed into: " + z.ToString())
                        (z[0], z[1]))
                    |> Seq.toList

                let name =
                    splits |> Seq.pick (fun (name, v) -> if name = "name" then Some v else None)

                let externalType =
                    chunk.Rest
                    |> Seq.tryFind (fun x -> x.StartsWith("script") && x.Contains("ExtResource"))
                    |> Option.map (fun x ->

                        let id = unwrapExtResource x

                        logger.WriteLine(
                            "Found external id for "
                            + name
                            + " external id = "
                            + id.ToString()
                            + " entire line = "
                            + x
                        )

                        let externalType: option<string> =
                            match externalNodes.TryGetValue id with
                            | (true, x) ->
                                logger.WriteLine(
                                    "External type found in external scripts, found "
                                    + x
                                    + " with id = "
                                    + id.ToString()
                                )

                                Some(x)
                            | (_, _) ->
                                logger.WriteLine(
                                    "Did not find external type for type "
                                    + name
                                    + " while node was marked as having an external type. External type id = "
                                    + id.ToString()
                                    + " entire line = "
                                    + x
                                )

                                None

                        externalType)
                    |> Option.flatten

                let parent =
                    splits
                    |> Seq.tryPick (fun (name, value) -> if name = "parent" then Some value else None)


                let baseType =
                    splits
                    |> Seq.tryPick (fun (name, v) -> if name = "type" then Some v else None)
                    |> Option.defaultValue "Node"

                let instance =
                    splits
                    |> Seq.tryPick (fun (name, v) ->
                        if name = "instance" then
                            v |> unwrapExtResource |> (fun x -> dependencies[x]) |> Some
                        else
                            None)

                let nodeType: NodeDef =
                    { externalScript = externalType
                      parent = parent
                      baseType = baseType
                      name = name
                      instance = instance }

                nodeType)

        let nodes = contents |> collectNodesAndScripts |> Seq.toList
        let fullPath = projectFilePath |> Path.GetFullPath
        let basePath = fullPath.Replace(fullPath |> Path.GetFileName, "")
        let resPath = fileName.Replace(basePath, "res://")

        { nodes = nodes
          dependencies = dependencies
          fileName = resPath }
