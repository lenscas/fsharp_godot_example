namespace SceneFileWriter

module Writer =
    open SceneParser
    open System.IO
    open SceneParser.SceneParser
    open ErrorHandling
    open System

    let magicHeader =
        "//This file is generated using GodotSceneIntergration. Any changes to this file will be overwritten automatically"

    let magicStart sceneFile nodeType props nameSpace =
        $"namespace {nameSpace}\nopen Godot\n\n[<AbstractClass>]\ntype {sceneFile}{props}() as this =\n    inherit {nodeType}()\n"

    let addScenePath path =
        $"    static member GetScenePath () = \"{path}\"\n"

    let implementISceneLoader props =
        $"    abstract member Setup: {props} -> unit\n    interface ISceneLoader<{props}> with\n        member this.Setup (props:{props}) = this.Setup props\n"

    let regularNodeStart name nodeType nameSpace =
        $"namespace {nameSpace}\nopen Godot\n\ntype {name}(this:{nodeType})=\n    member val public UnwrappedNode = this\n"

    let createRegularField name ofType ofWrapperType path =
        $"    member val _{name} = lazy(this.GetNode<{ofType}> \"{path}\" |> {ofWrapperType})\n    member public this.{name} with get() = this._{name}.Value"

    let createField name ofType path =
        $"    member val public {name} = lazy(this.GetNode<{ofType}> \"{path}\")"

    type SceneWriteErrors =
        | FileExistsWithoutMagicHeader of string
        | MissingDependency of string

    let isSafe a =
        if File.Exists a then
            use file = File.OpenText a
            let res = magicHeader |> file.ReadLine().StartsWith
            file.Close()
            res
        else
            true

    let getUniqueName (node: NodeDef) =
        let name =
            if node.name.ToLower() = node.baseType.ToLower() then
                match node.parent with
                | Some x when x = "." -> "FromRoot" + node.name
                | Some x -> x + node.name
                | None -> "Root" + node.name
            else
                node.name

        name + "Scene"

    let WriteSceneFile
        (nameSpace: string)
        nodeWritePath
        (parsedSceneFile: SceneFile)
        (deps: Collections.Generic.IDictionary<string, string>)
        =
        parsedSceneFile.nodes
        |> Seq.map (fun node ->
            let name = getUniqueName node
            let fullPath = Path.Combine([| nodeWritePath; name + ".fs" |])

            if fullPath |> isSafe |> not then
                fullPath |> FileExistsWithoutMagicHeader |> Error
            else
                use file = File.CreateText fullPath
                file.WriteLine magicHeader

                if node.externalScript.IsSome then
                    let inherits = node.baseType

                    let (props, scenePath) =
                        match node.parent with
                        | Some x -> "", ""
                        | None -> "<'t>", (addScenePath parsedSceneFile.fileName) + implementISceneLoader "'t"

                    file.Write(magicStart name inherits props nameSpace)
                    file.WriteLine(scenePath)
                else
                    file.Write(regularNodeStart name node.baseType nameSpace)

                let needsToBePath =
                    match node.parent with
                    | Some _ -> node.name
                    | None -> "."

                let fields =
                    parsedSceneFile.nodes
                    |> Seq.filter (fun x ->
                        x.parent
                        |> Option.map (fun x -> x = needsToBePath)
                        |> Option.defaultValue (false))
                    |> Seq.map (fun child ->
                        let ofType =
                            match child.instance, child.externalScript with
                            | _, Some x -> Ok(x, true)
                            | None, None -> Ok(child.baseType, false)
                            | Some x, _ ->
                                match deps.TryGetValue x with
                                | true, x -> Ok(x, true)
                                | false, _ -> x |> MissingDependency |> Error

                        match ofType with
                        | Ok(x, true) -> (createField child.name x ("./" + child.name)) |> Ok
                        | Ok(x, false) ->
                            let wrapperType = getUniqueName child
                            (createRegularField child.name x wrapperType ("./" + child.name)) |> Ok
                        | Error x -> Error x)
                    |> Seq.sequenceResultM

                let res =
                    match fields with
                    | Error x -> Error x
                    | Ok fields ->
                        for field in fields do
                            file.WriteLine field

                        Ok()

                let res =
                    match res with
                    | Error x -> Error x
                    | Ok() ->
                        match node.parent with
                        | Some x ->
                            System.Console.WriteLine(
                                "Found node ("
                                + node.name
                                + ") with parent: `"
                                + x
                                + "` Not adding to dependencies"
                            )

                            Ok(None)
                        | None ->
                            System.Console.WriteLine(
                                "Node (" + node.name + ") does not have a parent, adding to dependencies"
                            )

                            node.externalScript |> Option.defaultValue node.baseType |> Some |> Ok

                file.Flush()
                file.Close()
                res)
        |> Seq.sequenceResultM
        |> Result.map (fun x ->
            let z = x |> Seq.find (fun y -> y.IsSome)
            z.Value)
