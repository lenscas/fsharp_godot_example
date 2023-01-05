namespace super_dungeon_maker

open Godot
open GDUtils

type SceneManagerFs() as this =
    inherit Node2D()

    let mainMenu = fun () -> this.LoadScene<MainMenuFs> "MainMenu" //() this.getNode<MainMenuFs> "./MainMenu"

    let editor =
        fun () -> this.LoadScene<EditorFs> "Editor"

    let dungeon =
        fun () -> this.LoadScene<DungeonFs> "Dungeon"

    let mutable currentScene: Option<Node> = None

    let mutable floorsBeaten = 0

    let replaceScene scene =
        match currentScene with
        | None -> ()
        | Some x -> x.QueueFree()

        currentScene <- Some scene

    let openEditor after =
        let editor = editor ()
        editor.FuncAfterDone <- Some after
        replaceScene editor


    let openDungeon dun after =
        let dungeon = dungeon ()

        dungeon.StartDungeon
            dun
            (after
             >> fun () -> floorsBeaten <- floorsBeaten + 1)

        replaceScene dungeon

    let rec setupMainLoop =
        function
        | None -> Some >> setupMainLoop |> openEditor
        | Some x -> openDungeon x (fun () -> setupMainLoop None)

    override _._Ready() =
        let menu = mainMenu ()
        menu.OnPlay <- (fun () -> setupMainLoop None)
        replaceScene menu
