namespace SceneFinder

module SceneFinder =
    open System.IO
    open System

    type SceneFileError = NotValidGodotProject of Exception
    let GodotProjectFileExtension = ".godot"

    let private getGodotProjectFile (path: string) =
        let extension = Path.GetExtension(path)

        if extension <> GodotProjectFileExtension then
            "Given path does not seem to point to godot project file"
            |> Exception
            |> NotValidGodotProject
            |> Error
        else
            path |> Path.GetFullPath |> Ok

    type FullPathAndSceneFileLocations =
        { fullPath: string
          sceneFiles: string array }

    let getSceneFiles =
        getGodotProjectFile
        >> Result.map (fun path ->
            let fileName = Path.GetFileName path
            let projectPath = path.Remove(path.Length - fileName.Length)
            (path, projectPath))
        >> Result.map (fun (path, projectPath) ->
            { fullPath = path
              sceneFiles = System.IO.Directory.GetFiles(projectPath, "*.tscn", SearchOption.AllDirectories) })
