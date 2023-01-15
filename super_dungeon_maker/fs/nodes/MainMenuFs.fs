namespace super_dungeon_maker

open Godot

type MainMenuFs() =
    inherit MainMenuScene<unit -> unit>()

    override this.Setup a = this.OnPlay <- a
    member val OnPlay = ignore with get,set

    member this.OnPlayButtonPressed () =
        this.OnPlay ()