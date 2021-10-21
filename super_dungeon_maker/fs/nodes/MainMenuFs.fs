namespace super_dungeon_maker

open Godot

type MainMenuFs() =
    inherit Control()

    member val OnPlay = ignore with get,set

    member this.OnPlayButtonPressed () =
        this.OnPlay ()