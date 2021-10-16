namespace super_dungeon_maker

open Godot

type HandFs() =
    inherit Control()

    [<Export>]
    member val Text = "Hello World!" with get, set

    override this._Ready() = GD.Print(this.Text)
