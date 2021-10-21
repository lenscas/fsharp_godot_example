namespace super_dungeon_maker

open Godot
open GDUtils

type SceneManagerFs() as this =
    inherit Node2D()

    let mainMenu =
        this.getNode<MainMenuFs> "./MainMenu"

    let editor = this.getNode<EditorFs> "./EditorNode"

    let dungeon = this.getNode<DungeonFs> "./Dungeon"

    override _._Ready() =
        GD.Print "ran ready!"

        mainMenu.Value.OnPlay <-
            (fun () ->
                mainMenu.Value.Visible <- false
                mainMenu.Value.SetProcess false
                editor.Value.Visible <- true

                editor.Value.FuncAfterDone <-
                    Some
                        (fun dun ->
                            editor.Value.Visible <- false
                            editor.Value.SetProcess false
                            editor.Value.PauseMode <- Node.PauseModeEnum.Stop
                            dungeon.Value.PauseMode <- Node.PauseModeEnum.Inherit
                            dungeon.Value.StartDungeon dun
                            dungeon.Value.Visible <- true
                            ()))
