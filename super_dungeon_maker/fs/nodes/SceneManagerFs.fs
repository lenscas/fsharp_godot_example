namespace super_dungeon_maker

open Godot
open GDUtils
open SceneLoader

type SceneManagerFs() as this =
    inherit SceneManagerScene<unit>()
    
    let mutable currentScene: Option<Node> = None

    let mutable floorsBeaten = 0

    let replaceScene scene =
        this.AddChild scene
        match currentScene with
        | None -> ()
        | Some x -> x.QueueFree()

        currentScene <- Some scene

    let openEditor after =
        after 
        |> LoadScene<_,EditorFs>
        |> replaceScene


    let openDungeon dun after =
        let scene = LoadScene<_,DungeonFs> {dungeon = dun;after= (after >> fun () -> floorsBeaten <- floorsBeaten + 1)}
        replaceScene scene

    let rec setupMainLoop =
        function
        | None -> Some >> setupMainLoop |> openEditor
        | Some x -> openDungeon x (fun () -> setupMainLoop None)

    override this.Setup(()) = ()
    override _._Ready() =
        (fun () -> setupMainLoop None)
        |> LoadScene<_,MainMenuFs> 
        |> replaceScene
