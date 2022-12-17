module GodotTypeProviderImplementation

open System
open System.IO
open System.Reflection
open FSharp.Core.CompilerServices
open MyNamespace
open ProviderImplementation.ProvidedTypes

// Put any utility helpers here
[<AutoOpen>]
module internal Helpers =
    let x = 1

    let cache =
        System.Collections.Concurrent.ConcurrentDictionary<_, Lazy<ProvidedTypeDefinition>>()

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
        let asm = ProvidedAssembly()

        let myType =
            ProvidedTypeDefinition(asm, ns, typeName, Some typeof<obj>, isErased = false, nonNullable = true)

        let projectFileName = Path.GetFileName(path)

        if ".godot" |> projectFileName.EndsWith |> not then
            //is this the best way to report an error?
            "Given path does not seem to point to godot project file" |> Exception |> raise

        let projectPath = path.Remove(path.Length - projectFileName.Length)
        // let test = ProvidedField.Literal("testName", typeof<string>, projectPath)
        // myType.AddMember test
        // //is there a better way of doing this?
        let sceneFilePaths = System.IO.Directory.GetFiles(projectPath, "*.tscn")

        for sceneFilePath in sceneFilePaths do
            let name = Path.GetFileNameWithoutExtension(sceneFilePath)

            let fullPath =
                ProvidedField.Literal(name + "FullPath", typeof<string>, sceneFilePath)

            fullPath.AddXmlDoc(
                "Full path to the scene file: "
                + name
                + ".tscn.\n. Not for loading at runtime. Use : "
                + typeName
                + "."
                + name
                + " instead for loading at runtime"
            )

            myType.AddMember fullPath

            let loadPath =
                ProvidedField.Literal(name, typeof<string>, sceneFilePath.Replace(projectPath, "res://"))

            loadPath.AddXmlDoc(
                "res:// path to the scene file: "
                + name
                + ".tscn.\nOnly for internal use. For loading use: "
                + typeName
                + "."
                + name
            )

            myType.AddMember loadPath

        myType

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
