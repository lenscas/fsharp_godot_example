namespace super_dungeon_maker

open Godot

type EndFs() =
    inherit Sprite()

    member val RunOnReached = ignore with get, set

    member this.OnObjectGotOverEnd(a: Node) =
        match a with
        | :? PlayerFs as player -> this.RunOnReached()
        | _ -> ()
