namespace super_dungeon_maker

open Godot

type MainSceneFs() =
    inherit Node2D()

    [<Export>]
    member val Text = "Hello World!" with get, set

    override this._Ready() = GD.Print(this.Text)
