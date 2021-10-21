namespace super_dungeon_maker

open Godot


type BulletFs() =
    inherit Area2D()

    member val Velocity = 0f with get, set
    member val OnCollisionFunc = ignore with get, set

    override this._PhysicsProcess _ =
        this.Position <-
            this.Position
            + Vector2(this.Velocity, 0f).Rotated(this.Rotation)


        ()

    member this.OnCollision(body: Node) =
        this.OnCollisionFunc body
        this.QueueFree()
