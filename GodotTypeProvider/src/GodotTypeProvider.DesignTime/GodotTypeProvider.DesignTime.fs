module GodotTypeProviderImplementation

open System
open System.IO
open System.Reflection
open FSharp.Core.CompilerServices
open MyNamespace
open ProviderImplementation.ProvidedTypes

open System
open System
open System.IO
open System
open Microsoft.FSharp.Quotations
open Godot
// Put any utility helpers here
[<AutoOpen>]
module internal Helpers =
    let x = 1

    let cache =
        System.Collections.Concurrent.ConcurrentDictionary<_, Lazy<ProvidedTypeDefinition>>()

    type ResPathAndFullPath = { resPath: string; fullPath: string }

    type LineKeys =
        | Id of int
        | Path of string
        | Other of string

    type NodeDef =
        { externalScript: Option<string>
          parent: Option<string>
          name: string
          baseType: string }

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

    let parseScene (logger: StreamWriter) (contents: seq<string>) =
        logger.WriteLine "start parsing scene"

        let externalNodes: Collections.Generic.Dictionary<int, string> =
            Collections.Generic.Dictionary<int, string>()

        let extractedInfo = []

        let contents =
            contents
            |> Seq.filter (fun line ->
                logger.WriteLine line

                if line.StartsWith("[node") || line.StartsWith("script") then
                    true
                elif line.StartsWith("[ext_resource") && line.Contains("type=\"Script\"") then
                    logger.WriteLine("working on line =  " + line)

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
                                    if Path.GetExtension(x) = ".cs" then
                                        x |> Path.GetFileNameWithoutExtension |> (fun x -> x + "FS") |> Path
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

                    logger.WriteLine("adding to extenal nodes " + id.ToString() + " " + typeName)
                    externalNodes.Add(id, typeName)
                    false

                else
                    false)
            |> Seq.toList

        let collectNodesAndScripts (content: List<string>) =
            let findExternalType: string list -> string option * string list =
                function
                | head :: rest when head.StartsWith("script") && head.Contains("ExtResource") ->
                    logger.WriteLine("head = " + head + " rest = " + rest.ToString())
                    let id = head.Replace("script = ExtResource( ", "").Replace(" )", "") |> int

                    let externalType: option<string> =
                        match externalNodes.TryGetValue id with
                        | (true, x) -> Some(x)
                        | (_, _) -> None

                    (externalType, rest)
                | rest ->
                    logger.WriteLine("did not find external Type for " + rest.ToString())
                    None, rest

            let rec collectNodesHelper (content: List<string>) res =
                let (content, newVal) =
                    if content.Head.StartsWith "[node" then
                        logger.WriteLine("trying to find type for: " + content.Head)

                        let (externalType, content) = findExternalType content

                        let splits =
                            content.Head.Split(" ")
                            |> Seq.filter (fun x -> x <> "[node")
                            |> Seq.map (fun x ->
                                let z =
                                    "="
                                    |> x.Split
                                    |> Seq.map (fun x -> x.Replace("=", "").Replace("\"", "").Replace("]", ""))
                                    |> Seq.toList

                                (z[0], z[1]))
                            |> Seq.toList

                        let nodeType: NodeDef =
                            { externalScript = externalType
                              parent =
                                splits
                                |> Seq.tryPick (fun (name, value) -> if name = "parent" then Some value else None)
                              name = splits |> Seq.pick (fun (name, v) -> if name = "name" then Some v else None)
                              baseType = splits |> Seq.pick (fun (name, v) -> if name = "type" then Some v else None) }

                        (content, Some nodeType)
                    else
                        (content, None)

                let newRes =
                    match newVal with
                    | Some x -> x :: res
                    | None -> res

                match content.Tail with
                | [] -> newRes
                | x -> collectNodesHelper x newRes

            collectNodesHelper content []

        collectNodesAndScripts contents

[<TypeProvider>]
type BasicGenerativeProvider(config: TypeProviderConfig) as this =
    inherit
        TypeProviderForNamespaces(
            config,
            assemblyReplacementMap = [ ("GodotTypeProvider.DesignTime", "GodotTypeProvider.Runtime") ]
        )

    let ns = "GodotTypeProvider"
    let asm = Assembly.GetExecutingAssembly()

    // check we contain a copy of runtime files, and are not referencing the runtime DLL
    do assert (typeof<DataSource>.Assembly.GetName().Name = asm.GetName().Name)

    let createType typeName (path: string) =
        let logger: StreamWriter = makeLogger ()
        logger.AutoFlush <- true

        try

            let asm = ProvidedAssembly()

            let myType =
                ProvidedTypeDefinition(asm, ns, typeName, Some typeof<obj>, isErased = false, nonNullable = true)

            let projectFileName = Path.GetFileName(path)
            logger.WriteLine("Filename = " + projectFileName)

            if ".godot" |> projectFileName.EndsWith |> not then
                //is this the best way to report an error?
                "Given path does not seem to point to godot project file" |> Exception |> raise

            let projectPath = path.Remove(path.Length - projectFileName.Length)
            // let test = ProvidedField.Literal("testName", typeof<string>, projectPath)
            // myType.AddMember test
            // //is there a better way of doing this?
            let sceneFilePaths =
                System.IO.Directory.GetFiles(projectPath, "*.tscn", SearchOption.AllDirectories)

            for sceneFilePath in sceneFilePaths do
                logger.WriteLine("Found scene file:" + sceneFilePath)
                let name = Path.GetFileNameWithoutExtension(sceneFilePath)
                System.Console.WriteLine(name)

                let resPath = sceneFilePath.Replace(projectPath, "res://")
                let loadPath = ProvidedField.Literal("Res" + name, typeof<string>, resPath)

                loadPath.AddXmlDoc(
                    "res:// path to the scene file: "
                    + name
                    + ".tscn.\nOnly for internal use. For loading use: "
                    + typeName
                    + "."
                    + name
                )

                logger.WriteLine("Adding member:" + loadPath.Name)
                myType.AddMember loadPath


                let asStr = encode resPath sceneFilePath

                let fullPath = ProvidedField.Literal(name, typeof<string>, asStr)

                fullPath.AddXmlDoc(
                    "Used as a parameter for the other type providers.\nIt is a json string containing the full path and the res path to "
                    + name
                    + ".tscn.\n.This parameter should not be used to manually load the file at runtime. Use: "
                    + typeName
                    + "."
                    + loadPath.Name
                    + " instead"
                )

                logger.WriteLine("Adding member:" + fullPath.Name)
                myType.AddMember fullPath

            logger.Flush()
            logger.Close()
            myType
        with e ->
            logger.WriteLine("Error happened!")
            logger.WriteLine("===============================")
            logger.WriteLine "Error:"
            logger.WriteLine(e.ToString())
            logger.WriteLine("=====================")
            logger.WriteLine(e.StackTrace)
            logger.WriteLine("==========================")
            logger.WriteLine(e.Source)
            logger.Flush()
            raise e

    let myParamType =
        let t =
            ProvidedTypeDefinition(asm, ns, "GenerativeProvider", Some typeof<obj>, isErased = false)

        t.DefineStaticParameters(
            [ ProvidedStaticParameter("godotProjectPath", typeof<string>) ],
            fun typeName args ->
                cache
                    .GetOrAdd(
                        unbox<string> args[0],
                        (fun x -> lazy (createType typeName (x)))
                    )
                    .Value
        )


        t

    do this.AddNamespace(ns, [ myParamType ])

[<TypeProvider>]
type CreateRootNode(config: TypeProviderConfig) as this =
    inherit
        TypeProviderForNamespaces(
            config,
            assemblyReplacementMap = [ ("GodotTypeProvider.DesignTime", "GodotTypeProvider.Runtime") ]
        )

    let ns = "GodotTypeProvider"
    let asm = Assembly.GetExecutingAssembly()

    // check we contain a copy of runtime files, and are not referencing the runtime DLL
    do assert (typeof<DataSource>.Assembly.GetName().Name = asm.GetName().Name)

    let createType typeName (jsonPaths: string) =
        let logger = makeLogger ()
        logger.AutoFlush <- true

        try
            let asm = ProvidedAssembly()


            logger.WriteLine(jsonPaths)
            let (resPath, fullPath) = decode jsonPaths
            logger.WriteLine(resPath)
            logger.Flush()

            let fullPath =
                match fullPath with
                | Some x -> x
                | None ->
                    raise (
                        Exception(
                            "Failed to extract paths from parameter. This should only happen if you write the path yourself.\nUse the Project type provider instead to get the correct values\n"
                        )
                    )

            logger.WriteLine "start parsing scene file"
            logger.Flush()
            let sceneFile = fullPath |> File.ReadLines |> parseScene logger
            logger.WriteLine "parsed scene file"
            logger.Flush()
            let root = sceneFile |> Seq.find (fun x -> x.parent.IsNone)
            logger.WriteLine("Found root = " + root.name)
            let baseType = System.Type.GetType("Godot." + root.baseType + ", GodotSharp")
            logger.WriteLine("Found basetype for scene = " + baseType.FullName)

            let myType =
                ProvidedTypeDefinition(
                    asm,
                    ns,
                    typeName,
                    Some baseType,
                    isErased = false,
                    isAbstract = false,
                    isSealed = false
                )

            //let baseCtor = baseType.GetConstructors().[0]

            // let providedConstructor =
            // ProvidedConstructor(
            // [],
            // invokeCode = (fun _ -> <@@ () @@>),
            // BaseConstructorCall = fun args -> (baseCtor, [])
            // )

            let providedConstructor = ProvidedConstructor([], (fun args -> <@@ () @@>))

            let ctorInfo =
                baseType.GetConstructor(BindingFlags.Public ||| BindingFlags.Instance, null, [||], null)

            logger.WriteLine("consturcotr = " + ctorInfo.Name)
            providedConstructor.BaseConstructorCall <- fun args -> ctorInfo, args

            myType.AddMember providedConstructor

            for ctor in myType.GetConstructors() do
                logger.WriteLine(
                    "added ctor = "
                    + ctor.Name
                    + " is public = "
                    + ctor.IsPublic.ToString()
                    + " is ctor "
                    + ctor.IsConstructor.ToString()
                )

                let ctorParams = ctor.GetParameters()

                logger.WriteLine(
                    "Amount of params = "
                    + ctorParams.Length.ToString()
                    + " is fixed size =  "
                    + ctorParams.IsFixedSize.ToString()
                )

                for param in ctorParams do
                    logger.WriteLine("with param = " + param.Name + "Of type:" + param.GetType().FullName)

            let directChildren =
                sceneFile
                |> Seq.filter (fun x ->
                    match x.parent with
                    | Some "." -> true
                    | _ -> false)
                |> Seq.map (fun x ->
                    let returnType =
                        match x.externalScript with
                        | Some y ->
                            logger.WriteLine("found external type for " + y = " external type = " + y)
                            let foundType = () |> asm.GetTypes |> Seq.find (fun z -> z.Name = y)

                            if foundType = null then
                                logger.WriteLine("Did not find external type in asm.GetTypes")
                                System.Type.GetType y
                            else
                                foundType
                        | None ->
                            logger.WriteLine(
                                "did not find external type for "
                                + x.name
                                + " going for base type = "
                                + x.baseType
                                + ", Godot"
                            )

                            let foundType = System.Type.GetType("Godot." + x.baseType + ",GodotSharp")

                            if foundType = null then
                                logger.WriteLine("failed to find type for " + x.baseType)
                                foundType
                            else
                                logger.WriteLine("Found base type: " + foundType.FullName)
                                foundType

                    (returnType, x.name))

            for (fieldType, fieldName) in directChildren do
                logger.WriteLine("Time to make field:" + fieldName + "with type: " + fieldType.FullName)
                let path = "./" + fieldName
                logger.WriteLine("path = " + path)

                let field =
                    ProvidedProperty(
                        fieldName,
                        typeof<Lazy<Node>>,
                        //typedefof<Lazy<_>>.MakeGenericType fieldType,
                        fun args -> <@@ lazy ((%%args.[0]: Godot.Node).GetNode(new Godot.NodePath(path))) @@>
                    )

                logger.WriteLine("made field = " + field.Name)
                field.AddXmlDoc("Lazily gets the child node: " + fieldName + " Of type : " + fieldType.Name)
                myType.AddMember field
                logger.WriteLine("added field")
                ()

            let staticProp =
                ProvidedProperty(
                    propertyName = "debugProperty",
                    propertyType = typeof<string>,
                    isStatic = true,
                    getterCode = (fun args -> <@@ "Hello!" @@>)
                )

            myType.AddMember staticProp
            logger.WriteLine("Done")
            logger.Flush()
            myType
        with e ->
            logger.WriteLine("Error happened!")
            logger.WriteLine("===============================")
            logger.WriteLine "Error:"
            logger.WriteLine(e.ToString())
            logger.WriteLine("=====================")
            logger.WriteLine(e.StackTrace)
            logger.WriteLine("==========================")
            logger.WriteLine(e.Source)
            logger.Flush()
            raise e

    let myParamType =
        let t =
            ProvidedTypeDefinition(asm, ns, "CreateRootNode", Some typeof<obj>, isErased = false)

        t.DefineStaticParameters(
            [ ProvidedStaticParameter("godotProjectPath", typeof<string>) ],
            fun typeName args -> (createType typeName (unbox<string> args[0]))
        )


        t

    do this.AddNamespace(ns, [ myParamType ])
